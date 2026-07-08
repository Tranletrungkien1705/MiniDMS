namespace MiniDMS.Models.ViewModels;

public class SalesReportRow
{
    public string Period { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}
