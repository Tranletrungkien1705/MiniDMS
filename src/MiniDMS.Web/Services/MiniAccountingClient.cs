using System.Net.Http.Json;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public record AccountingResult(int Id, string EntryNo);

public interface IMiniAccountingClient
{
    string BaseUrl { get; }
    /// <summary>Đẩy 1 đơn hàng sang MiniAccounting → tự sinh bút toán Nợ131/Có511/Có3331.</summary>
    Task<AccountingResult> PostSaleAsync(Order order, CancellationToken ct = default);
}

public sealed class MiniAccountingClient(HttpClient http, IConfiguration config) : IMiniAccountingClient
{
    public string BaseUrl =>
        (config["MiniAccounting:BaseUrl"] ?? "https://miniaccounting.onrender.com").TrimEnd('/');

    public async Task<AccountingResult> PostSaleAsync(Order order, CancellationToken ct = default)
    {
        // Coi TotalAmount là doanh thu chưa thuế (khớp cách tính HĐĐT), VAT 10%.
        var net = order.TotalAmount;
        var vat = Math.Round(net * 0.10m, 0);
        var body = new
        {
            customer = order.Customer?.Name ?? "Khách lẻ",
            netAmount = net,
            vatAmount = vat,
            refNo = order.OrderNo
        };
        var resp = await http.PostAsJsonAsync($"{BaseUrl}/api/post-sale", body, ct);
        resp.EnsureSuccessStatusCode();
        var r = await resp.Content.ReadFromJsonAsync<AccountingResult>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Không nhận được kết quả bút toán.");
        return r;
    }
}
