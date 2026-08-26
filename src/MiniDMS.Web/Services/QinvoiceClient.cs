using System.Net.Http.Json;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

/// <summary>Kết quả phát hành HĐĐT trả về từ QinvoiceLite.</summary>
public record EInvoiceResult(Guid Id, string Series, long? Number, string Status, string? AuthorityCode);

public interface IQinvoiceClient
{
    string BaseUrl { get; }
    /// <summary>Tạo hóa đơn nháp từ đơn hàng rồi phát hành (ký số + gửi CQT) trên QinvoiceLite.</summary>
    Task<EInvoiceResult> IssueForOrderAsync(Order order, CancellationToken ct = default);
}

public sealed class QinvoiceClient(HttpClient http, IConfiguration config) : IQinvoiceClient
{
    public string BaseUrl =>
        (config["QinvoiceLite:BaseUrl"] ?? "https://qinvoicelite.onrender.com").TrimEnd('/');

    private Guid? _tenantId;

    private async Task<Guid> TenantAsync(CancellationToken ct)
    {
        if (_tenantId is { } cached) return cached;
        var info = await http.GetFromJsonAsync<InfoDto>($"{BaseUrl}/api/info", ct)
                   ?? throw new InvalidOperationException("Không lấy được thông tin tổ chức từ QinvoiceLite.");
        _tenantId = info.SeedTenantId;
        return info.SeedTenantId;
    }

    public async Task<EInvoiceResult> IssueForOrderAsync(Order order, CancellationToken ct = default)
    {
        var tenantId = await TenantAsync(ct);
        var req = new
        {
            tenantId,
            series = $"1C{DateTime.Now:yy}MDM",
            type = 1,   // HĐ GTGT
            buyerName = order.Customer?.Name ?? "Khách lẻ",
            lines = order.Lines.Select(l => new
            {
                name = l.Product?.Name ?? "Hàng hóa",
                quantity = l.Quantity,
                unitPrice = l.UnitPrice,
                vatRate = 10m
            })
        };

        var createResp = await http.PostAsJsonAsync($"{BaseUrl}/api/invoices", req, ct);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<InvoiceDto>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Tạo hóa đơn thất bại.");

        var issueResp = await http.PostAsync($"{BaseUrl}/api/invoices/{created.Id}/issue", null, ct);
        issueResp.EnsureSuccessStatusCode();
        var issued = await issueResp.Content.ReadFromJsonAsync<InvoiceDto>(cancellationToken: ct)
                     ?? throw new InvalidOperationException("Phát hành hóa đơn thất bại.");

        return new EInvoiceResult(issued.Id, issued.Series, issued.Number, issued.Status, issued.AuthorityCode);
    }

    private sealed record InfoDto(Guid SeedTenantId);
    private sealed record InvoiceDto(Guid Id, string Series, long? Number, string Status, string? AuthorityCode);
}
