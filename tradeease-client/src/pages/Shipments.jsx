import { useEffect, useState } from "react";
import { api } from "../api/client";

const STATUS_CLASS = {
  Booked: "status-booked",
  Loaded: "status-loaded",
  "In Transit": "status-transit",
  "At Port": "status-port",
  "Under Customs": "status-customs",
  Cleared: "status-cleared",
};

const ALL_STATUSES = Object.keys(STATUS_CLASS);

export default function Shipments({ setActivePage }) {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("All Statuses");
  const [typeFilter, setTypeFilter] = useState("All Types");
  const [dateFilter, setDateFilter] = useState("All Time");
  const [search, setSearch] = useState("");

  useEffect(() => {
    api.shipments()
      .then(setShipments)
      .catch(() => setShipments([]))
      .finally(() => setLoading(false));
  }, []);

  const filtered = shipments.filter((s) => {
    const matchStatus = statusFilter === "All Statuses" || s.status === statusFilter;
    const matchType = typeFilter === "All Types" || s.type === typeFilter;
    const q = search.toLowerCase();
    const matchSearch = s.reference.toLowerCase().includes(q)
      || s.productDesc.toLowerCase().includes(q)
      || (s.hsCode || "").includes(search);
      
    let matchDate = true;
    if (dateFilter !== "All Time" && s.createdAt) {
      const created = new Date(s.createdAt);
      const now = new Date();
      const diffDays = (now - created) / (1000 * 60 * 60 * 24);
      if (dateFilter === "Last 3 Days") matchDate = diffDays <= 3;
      else if (dateFilter === "Last 7 Days") matchDate = diffDays <= 7;
      else if (dateFilter === "Last Month") matchDate = diffDays <= 30;
      else if (dateFilter === "Last Year") matchDate = diffDays <= 365;
    }
    
    return matchStatus && matchType && matchSearch && matchDate;
  });

  if (loading) return <div className="card"><p className="muted-text">Loading shipments...</p></div>;

  return (
    <div className="shipments-page">
      <div className="filters-bar">
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
        <select className="filter-select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          <option>All Statuses</option>
          {ALL_STATUSES.map((s) => <option key={s}>{s}</option>)}
        </select>
        <input className="search-input flex-1" placeholder="Search shipment ID, HS code..." value={search} onChange={(e) => setSearch(e.target.value)} />
        <button className="btn-primary" onClick={() => setActivePage("new-shipment")}>New Shipment</button>
      </div>

      <div className="card table-card">
        {filtered.length === 0 ? (
          <p className="muted-text" style={{ padding: "20px" }}>No shipments found in TradeEaseDB.</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>SHIPMENT ID</th>
                  <th>TYPE</th>
                  <th>HS CODE</th>
                  <th>DESCRIPTION</th>
                  <th>ROUTE</th>
                  <th>VALUE</th>
                  <th>STATUS</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((s) => (
                  <tr key={s.shipmentId} className="table-row-hover" onClick={() => setActivePage("shipment-detail", s.shipmentId)} style={{ cursor: "pointer" }}>
                    <td className="shipment-id">{s.reference}</td>
                    <td><span className={`type-tag ${s.type === "Export" ? "tag-buyer" : "tag-supplier"}`}>{s.type}</span></td>
                    <td className="hs-code-cell">{s.hsCode || "—"}</td>
                    <td>
                      <div className="desc-main">{s.productDesc}</div>
                      {s.subDesc && <div className="desc-sub">{s.subDesc}</div>}
                    </td>
                    <td className="route-cell">{s.route}</td>
                    <td className="value bold">{s.value}</td>
                    <td><span className={`status-badge ${STATUS_CLASS[s.status] || "status-booked"}`}>{s.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
