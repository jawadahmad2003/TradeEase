import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";
import { Icon } from "../components/Icons";

const REPORT_ICON = { users: "users", shipments: "package", duty: "hash", schemes: "check-square", documents: "file-text" };

export default function AdminReports({ setActivePage }) {
  const showToast = useToast();
  const [reports, setReports] = useState([]);
  const [usage, setUsage] = useState([]);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(null); // "xlsx-shipments" | "pdf-users" etc.

  useEffect(() => {
    api.adminReports()
      .then((data) => {
        setReports(data?.reports || []);
        setUsage(data?.monthlyUsage || []);
      })
      .catch((err) => showToast(err?.message || "Could not load reports.", "error"))
      .finally(() => setLoading(false));
  }, []);

  const doExport = async (type, format) => {
    const key = `${format}-${type}`;
    setExporting(key);
    try {
      if (format === "xlsx") {
        await api.exportAdminReportXlsx(type);
        showToast(`Excel report downloaded — check your Downloads folder.`);
      } else {
        await api.exportAdminReportPdf(type);
        showToast(`PDF report downloaded — check your Downloads folder.`);
      }
    } catch (err) {
      showToast(err?.message || "Export failed.", "error");
    } finally {
      setExporting(null);
    }
  };

  if (loading) return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>
      <div className="card"><p className="muted-text">Loading reports from database…</p></div>
    </div>
  );

  const maxCount = Math.max(...(usage.map((u) => u.shipmentCount ?? u.count ?? 0)), 1);

  return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>

      <div className="card">
        <h2>Reports &amp; Analytics</h2>
        <p className="muted-text">Live counts from TradeEaseDB · Download as locked Excel or formatted PDF</p>
      </div>

      <div className="admin-modules-grid">
        {reports.map((r) => {
          const xlsxKey = `xlsx-${r.exportType}`;
          const pdfKey = `pdf-${r.exportType}`;
          return (
            <div key={r.title} className="card admin-module-card static">
              <div className="module-icon-wrap">
                <Icon name={REPORT_ICON[r.exportType] || "file-text"} size={22} />
              </div>
              <strong>{r.title}</strong>
              <p>{r.description}</p>
              <span className="stat-sub">{r.count ?? 0} records</span>

              <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.75rem", flexWrap: "wrap" }}>
                {/* Excel download — locked sheet */}
                <button
                  type="button"
                  className="btn-outline-sm"
                  disabled={!!exporting}
                  onClick={() => doExport(r.exportType, "xlsx")}
                  title="Download as locked Excel (.xlsx)"
                  style={{ display: "flex", alignItems: "center", gap: "4px" }}
                >
                  🔒 {exporting === xlsxKey ? "Exporting…" : "Excel"}
                </button>

                {/* PDF download — Crystal Report style */}
                <button
                  type="button"
                  className="btn-outline-sm"
                  disabled={!!exporting}
                  onClick={() => doExport(r.exportType, "pdf")}
                  title="Download as formatted PDF report"
                  style={{ display: "flex", alignItems: "center", gap: "4px" }}
                >
                  📄 {exporting === pdfKey ? "Generating…" : "PDF"}
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {usage.length > 0 && (
        <div className="card chart-card">
          <h3>Platform Usage — Last 6 Months</h3>
          <div className="bar-chart">
            {usage.map((u) => {
              const cnt = u.shipmentCount ?? u.count ?? 0;
              return (
                <div key={u.month} className="bar-col">
                  <div className="bar-fill" style={{ height: `${(cnt / maxCount) * 100}%` }} />
                  <span className="bar-label">{u.month}</span>
                  <span className="bar-value">{cnt}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
