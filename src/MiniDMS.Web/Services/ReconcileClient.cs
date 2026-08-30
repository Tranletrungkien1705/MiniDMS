using System.Net.Http.Json;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public interface IReconcileClient
{
    string BaseUrl { get; }
    /// <summary>Đẩy công nợ đại lý (ghi nợ) sang MiniReconcile khi xác nhận đơn.</summary>
    Task PostDebtAsync(Order order, Customer? customer, CancellationToken ct = default);
}

public sealed class ReconcileClient(HttpClient http, IConfiguration config) : IReconcileClient
{
    public string BaseUrl =>
        (config["MiniReconcile:BaseUrl"] ?? "https://minireconcile.onrender.com").TrimEnd('/');

    public async Task PostDebtAsync(Order order, Customer? customer, CancellationToken ct = default)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Code) || order.TotalAmount <= 0) return;
        var body = new
        {
            partnerCode = customer.Code, partnerName = customer.Name,
            type = 0,                       // 0 = ghi nợ
            amount = order.TotalAmount, refNo = order.OrderNo,
            note = "Công nợ đơn hàng " + order.OrderNo
        };
        try { await http.PostAsJsonAsync($"{BaseUrl}/api/ext/ledger", body, ct); } catch { /* best-effort */ }
    }
}
