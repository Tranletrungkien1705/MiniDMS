const base = '/api/v1'
async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
    ...opts, body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  if (res.status === 401) { window.location.href = '/Account/Login?returnUrl=/index.html'; throw new Error('Cần đăng nhập') }
  const text = await res.text(); const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}
export const api = {
  dashboard: () => req('/dashboard'),
  orders: (status) => req(`/orders${status != null ? `?status=${status}` : ''}`),
  order: (id) => req(`/orders/${id}`),
  createOrder: (b) => req('/orders', { method: 'POST', body: b }),
  confirm: (id) => req(`/orders/${id}/confirm`, { method: 'POST' }),
  deliver: (id) => req(`/orders/${id}/deliver`, { method: 'POST' }),
  payment: (id, amount) => req(`/orders/${id}/payment`, { method: 'POST', body: { amount } }),
  cancelOrder: (id) => req(`/orders/${id}/cancel`, { method: 'POST' }),
  products: () => req('/products'),
  stock: (sku) => req(`/stock${sku ? `?sku=${encodeURIComponent(sku)}` : ''}`),
  stockHistory: (pid) => req(`/stock/${pid}/history`),
  stockIn: (b) => req('/stock/in', { method: 'POST', body: b }),
  stockOut: (b) => req('/stock/out', { method: 'POST', body: b }),
  customers: (q) => req(`/customers${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  createCustomer: (b) => req('/customers', { method: 'POST', body: b })
}
export const fmtMoney = (n) => (n ?? 0).toLocaleString('vi-VN') + 'đ'
export const fmtNum = (n) => (n ?? 0).toLocaleString('vi-VN')
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
export const OSTATUS = ['Nháp', 'Đã xác nhận', 'Đã giao', 'Đã hủy']
export const OSTATUS_CSS = ['secondary', 'info', 'success', 'dark']
export const PSTATUS = ['Chưa TT', 'TT một phần', 'Đã TT']
export const PSTATUS_CSS = ['danger', 'warning', 'success']
