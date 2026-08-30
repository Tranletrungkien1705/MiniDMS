using System.Net.Http.Json;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public record WmsIssueResult(bool Ok, string? DocCode, bool Posted, string? Msg, string? TraceCode, string? Warehouse);

public interface IWmsClient
{
    string BaseUrl { get; }
    /// <summary>Đẩy đơn giao sang MiniWMS → xuất kho theo mã SP (chung mã danh mục PIM).</summary>
    Task<WmsIssueResult?> IssueOrderAsync(Order order, CancellationToken ct = default);
}

public sealed class WmsClient(HttpClient http, IConfiguration config) : IWmsClient
{
    public string BaseUrl =>
        (config["MiniWMS:BaseUrl"] ?? "https://miniwms.onrender.com").TrimEnd('/');

    public async Task<WmsIssueResult?> IssueOrderAsync(Order order, CancellationToken ct = default)
    {
        var lines = order.Lines
            .Where(l => l.Product != null && !string.IsNullOrWhiteSpace(l.Product.SKU) && l.Quantity > 0)
            .Select(l => new { code = l.Product!.SKU, qty = l.Quantity })
            .ToList();
        if (lines.Count == 0) return null;
        var body = new { refNo = order.OrderNo, partnerName = order.Customer?.Name ?? "Đại lý", lines };
        var resp = await http.PostAsJsonAsync($"{BaseUrl}/api/ext/issue", body, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<WmsIssueResult>(cancellationToken: ct);
    }
}
