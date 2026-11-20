import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

const STATUS_CLASS = {
  Booked: "status-booked",
  Loaded: "status-loaded",
  "In Transit": "status-transit",
  "At Port": "status-port",
  "Under Customs": "status-customs",
  Cleared: "status-cleared",
};

export default function AdminShipments({ setActivePage }) {
  const showToast = useToast();
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [dateFilter, setDateFilter] = useState("All Time");
  const [typeFilter, setTypeFilter] = useState("All Types");

  useEffect(() => {
    api.adminShipments()
      .then(setShipments)
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, []);

  const filtered = shipments.filter((s) => {
    const q = search.toLowerCase();
    const matchSearch = s.reference.toLowerCase().includes(q)
      || s.traderName.toLowerCase().includes(q)
      || (s.company || "").toLowerCase().includes(q);
    const matchStatus = statusFilter === "All" || s.status === statusFilter;
    const matchType = typeFilter === "All Types" || s.type === typeFilter;
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
    return matchSearch && matchStatus && matchType && matchDate;
  });

  if (loading) return <div className="card"><p className="muted-text">Loading shipments...</p></div>;

  return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>

      <div className="card">
        <div className="user-management-head">
          <div>
            <h2>All Shipments (System-wide)</h2>
            <p>{filtered.length} shipments across all trader accounts</p>
          </div>
        </div>

        <div className="filters-bar">
          <input className="search-input flex-1" placeholder="Search ID, trader, company..." value={search} onChange={(e) => setSearch(e.target.value)} />
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
            <option>All</option>
            <option>Booked</option>
            <option>In Transit</option>
            <option>At Port</option>
            <option>Under Customs</option>
            <option>Cleared</option>
          </select>
        </div>

        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>SHIPMENT ID</th>
                <th>TRADER</th>
                <th>COMPANY</th>
                <th>TYPE</th>
                <th>PRODUCT</th>
                <th>VALUE</th>
                <th>STATUS</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.shipmentId}>
                  <td className="shipment-id">{s.reference}</td>
                  <td>{s.traderName}</td>
                  <td>{s.company || "—"}</td>
                  <td>{s.type}</td>
                  <td>{s.productDesc}</td>
                  <td>{s.value}</td>
                  <td><span className={`status-badge ${STATUS_CLASS[s.status] || "status-transit"}`}>{s.status}</span></td>
                  <td>
                    <button type="button" className="btn-outline-sm" onClick={() => setActivePage("shipment-detail", s.shipmentId)}>
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
