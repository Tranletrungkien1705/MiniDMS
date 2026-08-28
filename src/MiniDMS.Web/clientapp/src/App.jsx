import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtMoney, fmtNum, fmtDate, fmtDateTime, OSTATUS, OSTATUS_CSS, PSTATUS, PSTATUS_CSS } from './api'

function Badge({ text, css }) { return <span className={`badge ${css || 'secondary'}`}>{text}</span> }
function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 740 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>{children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }

function Layout() {
  return (
    <>
      <nav className="nav"><span className="brand">🏢 MiniDMS</span>
        <NavLink to="/" end>Tổng quan</NavLink><NavLink to="/orders">Đơn hàng</NavLink>
        <NavLink to="/stock">Tồn kho</NavLink><NavLink to="/products">Sản phẩm</NavLink><NavLink to="/customers">Khách hàng</NavLink>
        <a href="/Account/Logout" style={{ marginLeft: 'auto' }}>Đăng xuất</a></nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  const max = Math.max(1, ...d.monthlySales.map(x => x.value))
  return (
    <>
      <h1>Tổng quan DMS {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis" style={{ marginBottom: 18 }}>
        <div className="kpi"><div className="v">{d.totalProducts}</div><div className="l">Sản phẩm</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--danger)' }}>{d.lowStockCount}</div><div className="l">Sắp hết hàng</div></div>
        <div className="kpi"><div className="v" style={{ fontSize: 18, color: 'var(--success)' }}>{fmtMoney(d.todayRevenue)}</div><div className="l">Doanh thu hôm nay</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--warning)' }}>{d.pendingOrders}</div><div className="l">Đơn chờ xử lý</div></div>
      </div>
      <div className="card funnel"><h2>Doanh số theo tháng</h2>
        {d.monthlySales.map((x, i) => (<div className="bar" key={i}><div className="lbl">{x.label}</div>
          <div className="track"><div className="fill" style={{ width: `${(x.value / max) * 100}%` }} /></div><div className="n" style={{ width: 110, fontSize: 12 }}>{fmtMoney(x.value)}</div></div>))}
      </div>
    </>
  )
}

function Orders() {
  const [rows, setRows] = useState([]); const [status, setStatus] = useState(''); const [open, setOpen] = useState(null); const [show, setShow] = useState(false)
  const load = () => api.orders(status === '' ? null : Number(status)).then(r => setRows(r.data))
  useEffect(() => { load() }, [status])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Đơn hàng</h1><div className="sp" />
        <select style={{ maxWidth: 150 }} value={status} onChange={e => setStatus(e.target.value)}><option value="">— Trạng thái —</option>{OSTATUS.map((s, i) => <option key={i} value={i}>{s}</option>)}</select>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Tạo đơn</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Số đơn</th><th>Khách</th><th>Ngày</th><th className="right">Giá trị</th><th>Thanh toán</th><th>HĐĐT</th><th>Trạng thái</th></tr></thead>
          <tbody>{rows.map(o => (
            <tr key={o.id} style={{ cursor: 'pointer' }} onClick={() => setOpen(o.id)}>
              <td>{o.orderNo}</td><td>{o.customer}</td><td>{fmtDate(o.orderDate)}</td><td className="right">{fmtMoney(o.totalAmount)}</td>
              <td><Badge text={PSTATUS[o.paymentStatus]} css={PSTATUS_CSS[o.paymentStatus]} /></td><td>{o.eInvoice ? <span className="pill">{o.eInvoice}</span> : '—'}</td>
              <td><Badge text={OSTATUS[o.status]} css={OSTATUS_CSS[o.status]} /></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Không có đơn.</td></tr>}</tbody></table>
      </div>
      {open && <OrderDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
      {show && <OrderForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function OrderDetail({ id, onClose, onChanged }) {
  const [o, setO] = useState(null); const [msg, setMsg] = useState(null); const [pay, setPay] = useState('')
  const load = () => api.order(id).then(r => setO(r.data))
  useEffect(() => { load() }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const act = async (fn, ok) => { try { await fn(); flash(true, ok || 'OK'); load(); onChanged() } catch (e) { flash(false, e.message) } }
  if (!o) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`Đơn ${o.orderNo}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <div className="row" style={{ marginBottom: 8 }}><Badge text={OSTATUS[o.status]} css={OSTATUS_CSS[o.status]} /><Badge text={PSTATUS[o.paymentStatus]} css={PSTATUS_CSS[o.paymentStatus]} />
        {o.eInvoiceCode && <span className="pill" style={{ flex: 'none' }}>HĐĐT: {o.eInvoiceCode}</span>}{o.accountingEntry && <span className="pill" style={{ flex: 'none' }}>Bút toán: {o.accountingEntry}</span>}</div>
      <dl className="dl"><dt>Khách</dt><dd>{o.customer}</dd><dt>Ngày</dt><dd>{fmtDate(o.orderDate)}</dd>
        <dt>Tổng tiền</dt><dd>{fmtMoney(o.totalAmount)}</dd><dt>Đã thanh toán</dt><dd>{fmtMoney(o.paidAmount)}</dd></dl>
      <div className="section-t">Dòng hàng</div>
      <table><thead><tr><th>Sản phẩm</th><th className="right">SL</th><th className="right">Đơn giá</th><th className="right">Thành tiền</th></tr></thead>
        <tbody>{o.lines.map((l, i) => <tr key={i}><td>{l.product}</td><td className="right">{l.quantity}</td><td className="right">{fmtMoney(l.unitPrice)}</td><td className="right">{fmtMoney(l.lineTotal)}</td></tr>)}</tbody></table>
      <div className="row" style={{ gap: 6, marginTop: 12, flexWrap: 'wrap' }}>
        {o.status === 0 && <button className="btn sm" style={{ flex: 'none' }} onClick={() => act(() => api.confirm(id), 'Đã xác nhận (đồng bộ kế toán).')}>Xác nhận</button>}
        {o.status === 1 && <button className="btn sm" style={{ flex: 'none' }} onClick={() => act(() => api.deliver(id), 'Đã giao (trừ kho).')}>Giao hàng</button>}
        {(o.status === 0 || o.status === 1) && <button className="btn gray sm" style={{ flex: 'none' }} onClick={() => act(() => api.cancelOrder(id), 'Đã hủy.')}>Hủy</button>}
        {o.paymentStatus !== 2 && o.status !== 3 && <><input type="number" placeholder="Số tiền TT" value={pay} onChange={e => setPay(e.target.value)} style={{ maxWidth: 140 }} />
          <button className="btn ghost sm" style={{ flex: 'none' }} onClick={() => act(() => api.payment(id, Number(pay)), 'Đã ghi nhận thanh toán.')} disabled={!pay}>Ghi TT</button></>}
      </div>
    </Modal>
  )
}

function OrderForm({ onClose, onSaved }) {
  const [customers, setCustomers] = useState([]); const [products, setProducts] = useState([])
  const [customerId, setCustomerId] = useState(''); const [lines, setLines] = useState([{ productId: '', quantity: 1, unitPrice: 0 }]); const [err, setErr] = useState('')
  useEffect(() => { api.customers().then(r => { setCustomers(r.data); if (r.data[0]) setCustomerId(r.data[0].id) }); api.products().then(r => setProducts(r.data)) }, [])
  const setLine = (i, k, v) => setLines(lines.map((l, j) => { if (j !== i) return l; const nl = { ...l, [k]: v }; if (k === 'productId') { const p = products.find(x => x.id === Number(v)); if (p) nl.unitPrice = p.salePrice } return nl }))
  const total = lines.reduce((s, l) => s + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0)
  const save = async () => {
    try {
      const payload = { customerId: Number(customerId), lines: lines.filter(l => l.productId && Number(l.quantity) > 0).map(l => ({ productId: Number(l.productId), quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) })) }
      if (payload.lines.length === 0) { setErr('Cần ≥1 dòng hàng'); return }
      await api.createOrder(payload); onSaved()
    } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Tạo đơn hàng" onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <Field label="Khách hàng"><select value={customerId} onChange={e => setCustomerId(e.target.value)}>{customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></Field>
      <div className="section-t">Dòng hàng</div>
      <table><thead><tr><th>Sản phẩm</th><th style={{ width: 90 }}>SL</th><th style={{ width: 140 }}>Đơn giá</th><th></th></tr></thead>
        <tbody>{lines.map((l, i) => (<tr key={i}>
          <td><select value={l.productId} onChange={e => setLine(i, 'productId', e.target.value)}><option value="">—</option>{products.map(p => <option key={p.id} value={p.id}>{p.sku} · {p.name}</option>)}</select></td>
          <td><input type="number" value={l.quantity} onChange={e => setLine(i, 'quantity', e.target.value)} /></td>
          <td><input type="number" value={l.unitPrice} onChange={e => setLine(i, 'unitPrice', e.target.value)} /></td>
          <td>{lines.length > 1 && <button className="btn gray sm" onClick={() => setLines(lines.filter((_, j) => j !== i))}>×</button>}</td></tr>))}</tbody></table>
      <div className="row" style={{ marginTop: 8 }}><button className="btn ghost sm" style={{ flex: 'none' }} onClick={() => setLines([...lines, { productId: '', quantity: 1, unitPrice: 0 }])}>+ Dòng</button>
        <div className="sp" /><div style={{ flex: 'none', fontWeight: 700 }}>Tổng: {fmtMoney(total)}</div></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Tạo đơn (Nháp)</button></div>
    </Modal>
  )
}

function Stock() {
  const [rows, setRows] = useState([]); const [sku, setSku] = useState(''); const [io, setIo] = useState(null)
  const load = () => api.stock(sku).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Tồn kho</h1><div className="sp" />
        <input style={{ maxWidth: 200 }} placeholder="Tìm SKU…" value={sku} onChange={e => setSku(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>SKU</th><th>Sản phẩm</th><th className="right">Tồn</th><th className="right">Định mức</th><th></th></tr></thead>
          <tbody>{rows.map(b => (<tr key={b.productId} style={b.low ? { background: '#fff5f5' } : undefined}>
            <td>{b.sku}</td><td>{b.productName}{b.low && <span className="badge danger" style={{ marginLeft: 4 }}>Thấp</span>}</td>
            <td className="right"><b>{fmtNum(b.quantity)}</b></td><td className="right muted">{b.minStock}</td>
            <td className="right"><button className="btn ghost sm" style={{ flex: 'none' }} onClick={() => setIo({ productId: b.productId, name: b.productName, mode: 'in' })}>Nhập</button> <button className="btn gray sm" style={{ flex: 'none' }} onClick={() => setIo({ productId: b.productId, name: b.productName, mode: 'out' })}>Xuất</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={5} className="muted" style={{ padding: 20 }}>Chưa có tồn.</td></tr>}</tbody></table>
      </div>
      {io && <StockForm io={io} onClose={() => setIo(null)} onSaved={() => { setIo(null); load() }} />}
    </>
  )
}

function StockForm({ io, onClose, onSaved }) {
  const [qty, setQty] = useState(1); const [note, setNote] = useState(''); const [err, setErr] = useState('')
  const save = async () => { try { const b = { productId: io.productId, quantity: Number(qty), note }; io.mode === 'in' ? await api.stockIn(b) : await api.stockOut(b); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title={`${io.mode === 'in' ? 'Nhập' : 'Xuất'} kho: ${io.name}`} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Số lượng"><input type="number" value={qty} onChange={e => setQty(e.target.value)} /></Field>
        <Field label="Ghi chú"><input value={note} onChange={e => setNote(e.target.value)} /></Field></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>{io.mode === 'in' ? 'Nhập kho' : 'Xuất kho'}</button></div>
    </Modal>
  )
}

function Products() {
  const [rows, setRows] = useState([])
  useEffect(() => { api.products().then(r => setRows(r.data)) }, [])
  return (
    <>
      <h1>Sản phẩm</h1>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>SKU</th><th>Tên</th><th>Nhóm</th><th className="right">Giá bán</th><th className="right">Giá vốn</th><th>Trạng thái</th></tr></thead>
          <tbody>{rows.map(p => <tr key={p.id}><td>{p.sku}</td><td>{p.name}</td><td>{p.category || '—'}</td><td className="right">{fmtMoney(p.salePrice)}</td><td className="right">{fmtMoney(p.costPrice)}</td>
            <td><span className={`badge ${p.isActive ? 'success' : 'dark'}`}>{p.isActive ? 'Đang bán' : 'Ngừng'}</span></td></tr>)}</tbody></table>
      </div>
    </>
  )
}

function Customers() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [show, setShow] = useState(false)
  const load = () => api.customers(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Khách hàng</h1><div className="sp" />
        <input style={{ maxWidth: 200 }} placeholder="Tìm…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Thêm KH</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Tên</th><th>SĐT</th><th>Địa chỉ</th></tr></thead>
          <tbody>{rows.map(c => <tr key={c.id}><td>{c.code}</td><td>{c.name}</td><td>{c.phone || '—'}</td><td>{c.address || '—'}</td></tr>)}</tbody></table>
      </div>
      {show && <CustomerForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function CustomerForm({ onClose, onSaved }) {
  const [f, setF] = useState({ name: '', phone: '', address: '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => { try { if (!f.name) { setErr('Cần tên'); return } await api.createCustomer(f); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title="Thêm khách hàng" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tên *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
        <Field label="SĐT"><input value={f.phone} onChange={e => up('phone', e.target.value)} /></Field></div>
      <Field label="Địa chỉ"><input value={f.address} onChange={e => up('address', e.target.value)} /></Field>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="orders" element={<Orders />} />
        <Route path="stock" element={<Stock />} />
        <Route path="products" element={<Products />} />
        <Route path="customers" element={<Customers />} />
      </Route>
    </Routes>
  )
}
