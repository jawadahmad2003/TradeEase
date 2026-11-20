import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function AdminSystem({ setActivePage }) {
  const showToast = useToast();
  const [tariffYear, setTariffYear] = useState("FY 2025-26");
  const [maintenance, setMaintenance] = useState(false);
  const [lastForexSync, setLastForexSync] = useState("—");
  const [appVersion, setAppVersion] = useState("1.0.0");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.adminSettings()
      .then((s) => {
        setTariffYear(s.tariffYear);
        setMaintenance(s.maintenanceMode);
        setLastForexSync(s.lastForexSync || "—");
        setAppVersion(s.appVersion);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const refreshForex = async () => {
    try {
      await api.refreshForex();
      setLastForexSync(new Date().toLocaleString("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }) + " PKT");
      showToast("Forex rates force-refreshed.");
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const saveTariff = async () => {
    try {
      await api.updateSettings({ tariffYear });
      showToast("Tariff year updated.");
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const applyMaintenance = async () => {
    try {
      await api.updateSettings({ maintenanceMode: maintenance });
      showToast(maintenance ? "Maintenance mode enabled." : "Maintenance mode disabled.");
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading settings...</p></div>;

  return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>

      <div className="admin-settings-grid">
        <div className="card">
          <h3>Forex Integration</h3>
          <p className="muted-text">Daily refresh via frankfurter.app</p>
          <div className="account-detail-row"><span>Last sync</span><strong>{lastForexSync}</strong></div>
          <button type="button" className="btn-primary mt-8" onClick={refreshForex}>Force Refresh Now</button>
        </div>

        <div className="card">
          <h3>FBR Tariff Settings</h3>
          <label className="form-group">
            <span className="form-label">Active Tariff Year</span>
            <select className="form-select" value={tariffYear} onChange={(e) => setTariffYear(e.target.value)}>
              <option>FY 2025-26</option>
              <option>FY 2024-25</option>
            </select>
          </label>
          <button type="button" className="btn-ghost" onClick={saveTariff}>Save Tariff Year</button>
        </div>

        <div className="card">
          <h3>Platform Maintenance</h3>
          <label className="auth-check profile-notif-row">
            <input type="checkbox" checked={maintenance} onChange={(e) => setMaintenance(e.target.checked)} />
            <span>Enable maintenance mode (blocks trader login)</span>
          </label>
          <button type="button" className="btn-primary mt-8" onClick={applyMaintenance}>Apply</button>
        </div>

        <div className="card">
          <h3>System Info</h3>
          <div className="account-detail-row"><span>Environment</span><strong>Development</strong></div>
          <div className="account-detail-row"><span>Database</span><strong>SQL Server Express</strong></div>
          <div className="account-detail-row"><span>Version</span><strong>TradeEase v{appVersion}</strong></div>
        </div>
      </div>
    </div>
  );
}
