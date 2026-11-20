import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function AdminSchemes({ setActivePage }) {
  const showToast = useToast();
  const [rules, setRules] = useState([]);
  const [showAdd, setShowAdd] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", agency: "", benefit: "", deadline: "", exportPct: 0, turnover: 0 });
  const [loading, setLoading] = useState(true);

  const loadSchemes = () => {
    setLoading(true);
    api.adminSchemes()
      .then((data) => setRules(data.map((s) => ({
        id: s.schemeId,
        code: s.schemeCode,
        name: s.schemeName,
        minExportPct: Number(s.minExportPct),
        minTurnoverUsd: Number(s.minTurnoverUsd),
        active: s.isActive,
      }))))
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadSchemes();
  }, []);

  const toggleActive = (code) => {
    setRules((prev) => prev.map((r) => r.code === code ? { ...r, active: !r.active } : r));
  };

  const updateRule = (code, field, value) => {
    setRules((prev) => prev.map((r) => r.code === code ? { ...r, [field]: Number(value) || 0 } : r));
  };

  const saveAll = async () => {
    try {
      await api.updateSchemes(rules.map((r) => ({
        schemeCode: r.code,
        minExportPct: r.minExportPct,
        minTurnoverUsd: r.minTurnoverUsd,
        isActive: r.active,
      })));
      showToast("All scheme rules saved to database.");
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const createScheme = async () => {
    if (!form.code || !form.name) {
      showToast("Scheme code and name are required.", "error");
      return;
    }
    try {
      await api.createScheme({
        schemeCode: form.code,
        schemeName: form.name,
        agency: form.agency,
        benefitDetails: form.benefit,
        applicationDeadline: form.deadline,
        minExportPct: Number(form.exportPct) || 0,
        minTurnoverUsd: Number(form.turnover) || 0,
        isActive: true
      });
      showToast("Scheme created.");
      setShowAdd(false);
      setForm({ code: "", name: "", agency: "", benefit: "", deadline: "", exportPct: 0, turnover: 0 });
      loadSchemes();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const deleteScheme = async (id) => {
    if (!window.confirm("Are you sure you want to delete this scheme?")) return;
    try {
      await api.deleteScheme(id);
      showToast("Scheme deleted.");
      loadSchemes();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading schemes...</p></div>;

  return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>

      <div className="card">
        <div className="user-management-head">
          <div>
            <h2>Export Scheme Eligibility Rules</h2>
            <p>Configure rules used by the Scheme Checker</p>
          </div>
          <div className="user-mgmt-actions">
            <button type="button" className="btn-ghost" onClick={() => setShowAdd(true)}>+ Add Scheme</button>
            <button type="button" className="btn-primary" onClick={saveAll}>Save All Rules</button>
          </div>
        </div>

        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>SCHEME</th>
                <th>MIN EXPORT %</th>
                <th>MIN TURNOVER (USD)</th>
                <th>STATUS</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              {rules.map((r) => (
                <tr key={r.code}>
                  <td><strong>{r.code}</strong><div className="desc-sub">{r.name}</div></td>
                  <td>
                    <input className="form-input admin-inline-input" type="number" value={r.minExportPct} onChange={(e) => updateRule(r.code, "minExportPct", e.target.value)} />
                  </td>
                  <td>
                    <input className="form-input admin-inline-input" type="number" value={r.minTurnoverUsd} onChange={(e) => updateRule(r.code, "minTurnoverUsd", e.target.value)} />
                  </td>
                  <td>
                    <span className={`user-status ${r.active ? "active" : "inactive"}`}>{r.active ? "Active" : "Disabled"}</span>
                  </td>
                  <td>
                    <button type="button" className="btn-outline-sm" onClick={() => toggleActive(r.code)}>
                      {r.active ? "Disable" : "Enable"}
                    </button>
                    <button type="button" className="btn-outline-sm danger-text" onClick={() => deleteScheme(r.id)} style={{marginLeft: '8px'}}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showAdd && (
        <div className="modal-overlay" onClick={() => setShowAdd(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Add New Scheme</h3>
              <button type="button" className="icon-btn" onClick={() => setShowAdd(false)}>✕</button>
            </div>
            <div className="modal-body">
              <div className="form-row">
                <label className="form-group flex-1"><span className="form-label">Code *</span><input className="form-input" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></label>
                <label className="form-group flex-2"><span className="form-label">Name *</span><input className="form-input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label>
              </div>
              <label className="form-group"><span className="form-label">Agency</span><input className="form-input" value={form.agency} onChange={(e) => setForm({ ...form, agency: e.target.value })} /></label>
              <label className="form-group"><span className="form-label">Benefits</span><input className="form-input" value={form.benefit} onChange={(e) => setForm({ ...form, benefit: e.target.value })} /></label>
              <label className="form-group"><span className="form-label">Deadline</span><input className="form-input" value={form.deadline} onChange={(e) => setForm({ ...form, deadline: e.target.value })} /></label>
              <div className="form-row">
                <label className="form-group flex-1"><span className="form-label">Min Export %</span><input className="form-input" type="number" value={form.exportPct} onChange={(e) => setForm({ ...form, exportPct: e.target.value })} /></label>
                <label className="form-group flex-1"><span className="form-label">Min Turnover (USD)</span><input className="form-input" type="number" value={form.turnover} onChange={(e) => setForm({ ...form, turnover: e.target.value })} /></label>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => setShowAdd(false)}>Cancel</button>
              <button type="button" className="btn-primary" onClick={createScheme}>Create</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
