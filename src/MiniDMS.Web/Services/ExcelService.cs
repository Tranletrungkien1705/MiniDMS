using OfficeOpenXml;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public interface IExcelService
{
    List<StockImportRow> ReadStockImport(Stream stream);
    byte[] ExportStockBalance(List<StockBalance> balances);
    byte[] ExportTransactions(List<StockTransaction> txs, string productName);
    byte[] ExportDebtReport(List<DebtRow> rows);
}

public record StockImportRow(string SKU, int Quantity, string? Note);
public record DebtRow(string CustomerCode, string CustomerName, decimal TotalAmount, decimal PaidAmount, decimal Debt);

public class ExcelService : IExcelService
{
    static ExcelService() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    public List<StockImportRow> ReadStockImport(Stream stream)
    {
        using var pkg = new ExcelPackage(stream);
        var ws = pkg.Workbook.Worksheets.First();
        var rows = new List<StockImportRow>();

        for (int r = 2; r <= ws.Dimension?.End.Row; r++)
        {
            var sku = ws.Cells[r, 1].Text.Trim();
            if (string.IsNullOrEmpty(sku)) continue;
            _ = int.TryParse(ws.Cells[r, 2].Text, out int qty);
            var note = ws.Cells[r, 3].Text.Trim();
            rows.Add(new StockImportRow(sku, qty, note));
        }
        return rows;
    }

    public byte[] ExportStockBalance(List<StockBalance> balances)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Tồn kho");

        // Headers
        string[] headers = ["SKU", "Tên sản phẩm", "Danh mục", "ĐVT", "Nhập", "Xuất", "Tồn", "Cảnh báo"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
            ws.Cells[1, c + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 50, 80));
            ws.Cells[1, c + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        int row = 2;
        foreach (var b in balances)
        {
            ws.Cells[row, 1].Value = b.SKU;
            ws.Cells[row, 2].Value = b.ProductName;
            ws.Cells[row, 3].Value = b.Category;
            ws.Cells[row, 4].Value = b.Unit;
            ws.Cells[row, 5].Value = b.TotalIn;
            ws.Cells[row, 6].Value = b.TotalOut;
            ws.Cells[row, 7].Value = b.Balance;
            ws.Cells[row, 8].Value = b.IsLowStock ? "⚠ Thấp" : "";
            if (b.IsLowStock)
                ws.Cells[row, 7].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        ws.Cells[1, 1, 1, headers.Length].AutoFilter = true;
        return pkg.GetAsByteArray();
    }

    public byte[] ExportTransactions(List<StockTransaction> txs, string productName)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add($"Lịch sử - {productName}");

        string[] headers = ["Ngày", "Loại", "Số lượng", "Mã tham chiếu", "Ghi chú", "Người tạo"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var t in txs)
        {
            ws.Cells[row, 1].Value = t.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            ws.Cells[row, 2].Value = t.Type == TransactionType.In ? "Nhập" : "Xuất";
            ws.Cells[row, 3].Value = t.Quantity;
            ws.Cells[row, 4].Value = t.RefNo;
            ws.Cells[row, 5].Value = t.Note;
            ws.Cells[row, 6].Value = t.CreatedBy;
            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return pkg.GetAsByteArray();
    }

    public byte[] ExportDebtReport(List<DebtRow> rows)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Công nợ");

        string[] headers = ["Mã KH", "Khách hàng", "Tổng tiền", "Đã thanh toán", "Còn nợ"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
        }

        int row = 2;
        decimal totalDebt = 0;
        foreach (var r in rows)
        {
            ws.Cells[row, 1].Value = r.CustomerCode;
            ws.Cells[row, 2].Value = r.CustomerName;
            ws.Cells[row, 3].Value = r.TotalAmount;
            ws.Cells[row, 4].Value = r.PaidAmount;
            ws.Cells[row, 5].Value = r.Debt;
            ws.Cells[row, 3, row, 5].Style.Numberformat.Format = "#,##0";
            if (r.Debt > 0) ws.Cells[row, 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            totalDebt += r.Debt;
            row++;
        }

        ws.Cells[row, 4].Value = "TỔNG NỢ:";
        ws.Cells[row, 4].Style.Font.Bold = true;
        ws.Cells[row, 5].Value = totalDebt;
        ws.Cells[row, 5].Style.Font.Bold = true;
        ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return pkg.GetAsByteArray();
    }
}
