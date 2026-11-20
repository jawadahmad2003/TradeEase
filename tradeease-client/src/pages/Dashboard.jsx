import { useEffect, useState } from "react";
import { useToast } from "../components/Toast";
import { api } from "../api/client";

const STATUS_CLASS = {
  Booked: "status-booked",
  Loaded: "status-loaded",
  "In Transit": "status-transit",
  "At Port": "status-port",
  "Under Customs": "status-customs",
  Cleared: "status-cleared",
};

const STATUS_OPTIONS = Object.keys(STATUS_CLASS);

function formatValue(pkr) {
  if (pkr == null) return "—";
  if (pkr >= 1_000_000) return `PKR ${(pkr / 1_000_000).toFixed(1)}M`;
  return `PKR ${Number(pkr).toLocaleString("en-PK", { minimumFractionDigits: 2 })}`;
}

export default function Dashboard({ setActivePage }) {
  const showToast = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [dateFilter, setDateFilter] = useState("All Time");
  const [typeFilter, setTypeFilter] = useState("All Types");

  useEffect(() => {
    setLoading(true);
    let days = "";
    if (dateFilter === "Last 3 Days") days = 3;
    else if (dateFilter === "Last 7 Days") days = 7;
    else if (dateFilter === "Last Month") days = 30;
    else if (dateFilter === "Last Year") days = 365;

    let params = [];
    if (days) params.push(`days=${days}`);
    if (typeFilter && typeFilter !== "All Types") params.push(`type=${typeFilter}`);
    const qs = params.length > 0 ? `?${params.join("&")}` : "";

    api.dashboard(qs)
      .then(setData)
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, [dateFilter, typeFilter, showToast]);

  const shipments = (data?.recentShipments || []).map((s) => ({
    id: s.shipmentId,
    ref: s.reference,
    desc: s.productDesc,
    type: s.shipmentType || "—",
    route: s.route || "—",
    date: new Date(s.createdAt).toLocaleDateString("en-PK", { day: "2-digit", month: "short", year: "numeric" }),
    value: formatValue(s.totalValuePKR),
    status: s.status,
    statusClass: STATUS_CLASS[s.status] || "status-booked",
  }));

  const updateStatus = async (id, status, e) => {
    e.stopPropagation();
    try {
      await api.updateShipmentStatus(id, status);
      setData((prev) => prev ? {
        ...prev,
        recentShipments: prev.recentShipments.map((s) =>
          s.shipmentId === id ? { ...s, status } : s),
      } : prev);
      showToast(`Status updated to ${status}.`);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading dashboard from API...</p></div>;

  const stats = [
    { label: "Total Shipments", value: String(data?.totalShipments ?? 0), icon: "ship", color: "#1a7a5e" },
    { label: "Imports", value: String(data?.importShipments ?? 0), icon: "doc", color: "#2563eb" },
    { label: "Exports", value: String(data?.exportShipments ?? 0), icon: "box", color: "#7c3aed" },
    { label: "Total Trade (PKR)", value: formatValue(data?.totalTradeValue), icon: "dollar", color: "#16a34a" },
  ];

  const forex = (data?.forexRates || []).map((f) => ({
    pair: `${f.currencyCode}/PKR`,
    rate: Number(f.exchangeRate).toFixed(2),
    up: f.isUp ?? true,
  }));

  const partners = (data?.topTradePartners || []).map((p) => ({
    id: p.partyId,
    name: p.name,
    type: p.partyType,
    country: p.country || "—",
    count: p.shipmentCount,
    value: p.tradeValuePkr,
  }));

  const icons = {
    ship: <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M2 21c.6.5 1.2 1 2.5 1C7 22 7 21 9.5 21s2.5 1 5 1 2.5-1 5-1c1.3 0 1.9.5 2.5 1"/></svg>,
    dollar: <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>,
    doc: <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/></svg>,
    box: <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/></svg>,
  };

  return (
    <div className="dashboard">
      <div className="tag-banner">Pakistan import & export dashboard · values in PKR · FBR duty estimates on imports</div>
      <div className="filters-bar" style={{ marginBottom: "20px" }}>
        <select className="filter-select" value={dateFilter} onChange={(e) => setDateFilter(e.target.value)}>
          <option>All Time</option>
          <option>Last 3 Days</option>
          <option>Last 7 Days</option>
          <option>Last Month</option>
          <option>Last Year</option>
        </select>
        <select className="filter-select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option>All Types</option>
          <option>Import</option>
          <option>Export</option>
        </select>
      </div>
      <div className="stats-grid">
        {stats.map((s, i) => (
          <div key={i} className="stat-card" style={{ "--accent": s.color }}>
            <div className="stat-top">
              <div className="stat-label">{s.label}</div>
              <div className="stat-icon">{icons[s.icon]}</div>
            </div>
            <div className="stat-value">{s.value}</div>
            <div className="stat-bar" />
          </div>
        ))}
      </div>

      <div className="dashboard-grid">
        <div className="card shipments-card">
          <div className="card-header">
            <h2>Recent Shipments</h2>
            <button type="button" className="link-btn" onClick={() => setActivePage("shipments")}>View All →</button>
          </div>
          {shipments.length === 0 ? (
            <p className="muted-text" style={{ padding: "16px" }}>No shipments yet — create your first import or export.</p>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>REF</th>
                    <th>TYPE</th>
                    <th>ROUTE</th>
                    <th>PRODUCT</th>
                    <th>VALUE</th>
                    <th>STATUS</th>
                    <th>UPDATE</th>
                  </tr>
                </thead>
                <tbody>
                  {shipments.map((s) => (
                    <tr key={s.id} className="table-row-hover" onClick={() => setActivePage("shipment-detail", s.id)} style={{ cursor: "pointer" }}>
                      <td className="shipment-id">{s.ref}</td>
                      <td>{s.type}</td>
                      <td>{s.route}</td>
                      <td>{s.desc}</td>
                      <td className="value">{s.value}</td>
                      <td><span className={`status-badge ${s.statusClass}`}>{s.status}</span></td>
                      <td onClick={(e) => e.stopPropagation()}>
                        <select className="filter-select status-quick-select" value={s.status} onChange={(e) => updateStatus(s.id, e.target.value, e)}>
                          {STATUS_OPTIONS.map((opt) => <option key={opt}>{opt}</option>)}
                        </select>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="side-col">
          <div className="card forex-card">
            <div className="card-header">
              <h2>Forex (PKR)</h2>
              <button type="button" className="link-btn" onClick={() => setActivePage("forex-rates")}>View All →</button>
            </div>
            <div className="forex-list">
              {forex.length === 0 ? (
                <p className="muted-text">Live forex loading — refresh in a moment.</p>
              ) : forex.slice(0, 8).map((f) => (
                <div key={f.pair} className="forex-item">
                  <span className="forex-pair">{f.pair}</span>
                  <div className="forex-right">
                    <span className="forex-rate">{f.rate}</span>
                    <span className={`forex-change ${f.up ? "up" : "down"}`}>{f.up ? "↗" : "↘"}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card mt-8">
            <div className="card-header">
              <h2>Top Trade Partners</h2>
              <span className="muted-text">From shipment activity</span>
            </div>
            {partners.length === 0 ? (
              <p className="muted-text" style={{ padding: "16px" }}>No trade partners yet — create shipments to populate this panel.</p>
            ) : (
              <div className="forex-list">
                {partners.map((p) => (
                  <div key={p.id} className="forex-item">
                    <div>
                      <div className="forex-pair">{p.name}</div>
                      <div className="muted-text" style={{ fontSize: "0.85rem" }}>{p.type} · {p.country}</div>
                    </div>
                    <div className="forex-right" style={{ flexDirection: "column", alignItems: "flex-end", gap: "2px" }}>
                      <span className="forex-rate">{p.count} shipments</span>
                      <span className="muted-text">{formatValue(p.value)}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
