import { useEffect, useState } from "react";
import { api } from "../api/client";
import { Icon } from "../components/Icons";

export default function AdminPortal({ setActivePage }) {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api.adminStats()
      .then((data) => {
        setStats(data);
        setError("");
      })
      .catch((err) => {
        setStats(null);
        setError(err.message || "Could not load admin stats.");
      })
      .finally(() => setLoading(false));
  }, []);

  const modules = [
    { id: "user-management", title: "User Management", desc: "Activate, deactivate, roles, invite traders", icon: "users" },
    { id: "admin-shipments", title: "All Shipments", desc: "View and manage shipments across all accounts", icon: "package" },
    { id: "admin-hs-codes", title: "HS Tariff Database", desc: "Add, edit, import FBR tariff CSV", icon: "hash" },
    { id: "admin-schemes", title: "Export Scheme Rules", desc: "Update EFS, DDT, SBP-EFS eligibility rules", icon: "check-square" },
    { id: "admin-system", title: "System Settings", desc: "Forex refresh, tariff year, maintenance mode", icon: "settings" },
    { id: "admin-reports", title: "Reports & Analytics", desc: "System-wide trade reports and exports", icon: "chart" },
  ];

  const statCards = stats ? [
    { label: "Total Users", value: String(stats.totalUsers), sub: `${stats.activeTraders} active traders`, icon: "users", color: "#1a7a5e" },
    { label: "Total Shipments", value: String(stats.totalShipments), sub: `${stats.pendingShipments} pending`, icon: "package", color: "#2563eb" },
    { label: "HS Codes", value: String(stats.totalHsCodes), sub: "Active tariff entries", icon: "hash", color: "#16a34a" },
    { label: "Export Schemes", value: String(stats.activeSchemes), sub: "Active scheme rules", icon: "shield", color: "#d97706" },
  ] : Array.from({ length: 4 }, (_, index) => ({
    label: loading ? "Loading admin stats..." : "Stats unavailable",
    value: loading ? "—" : "!",
    sub: loading ? "Preparing portal data" : "Check server and database connection",
    icon: "settings",
    color: index % 2 === 0 ? "#64748b" : "#94a3b8",
  }));

  return (
    <div className="admin-portal">
      <div className="admin-banner card">
        <div>
          <h2>Admin Control Portal</h2>
          <p>Manage users, shipments, tariff data, scheme rules, and system settings for all TradeEase traders.</p>
        </div>
        <span className="admin-badge">Administrator</span>
      </div>

      {error && (
        <div className="card mt-8">
          <p className="auth-error">{error}</p>
          <p className="muted-text">The portal is still available. Stats can be retried after SQL Server is ready.</p>
          <button type="button" className="btn-primary mt-8" onClick={() => window.location.reload()}>
            Retry stats
          </button>
        </div>
      )}

      <div className="stats-grid">
        {statCards.map((s, idx) => (
          <div key={`${s.label}-${s.value}-${idx}`} className="stat-card" style={{ "--accent": s.color }}>
            <div className="stat-top">
              <div className="stat-label">{s.label}</div>
              <div className="stat-icon"><Icon name={s.icon} size={22} /></div>
            </div>
            <div className="stat-value">{s.value}</div>
            <div className="stat-sub">{s.sub}</div>
            <div className="stat-bar" />
          </div>
        ))}
      </div>

      <h3 className="section-heading">Management Modules</h3>
      <div className="admin-modules-grid">
        {modules.map((m) => (
          <button key={m.id} type="button" className="card admin-module-card" onClick={() => setActivePage(m.id)}>
            <div className="module-icon-wrap"><Icon name={m.icon} size={22} /></div>
            <strong>{m.title}</strong>
            <p>{m.desc}</p>
            <span className="module-link">Open <Icon name="arrow-right" size={14} /></span>
          </button>
        ))}
      </div>
    </div>
  );
}
