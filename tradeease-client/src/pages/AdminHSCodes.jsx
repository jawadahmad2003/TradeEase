import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function AdminHSCodes({ setActivePage }) {
  const showToast = useToast();
  const [codes, setCodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [currentId, setCurrentId] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [form, setForm] = useState({ code: "", desc: "", customs: "", salesTax: "", ait: "" });

  const mapCode = (h) => ({
    id: h.hsCodeId || h.id,
    code: h.code,
    desc: h.description,
    customs: Number(h.customsDutyPct ?? 0),
    salesTax: Number(h.salesTaxPct ?? 0),
    ait: Number(h.aITPct ?? h.aitPct ?? 0),
    tariffYear: h.tariffYear ?? "FY 2025-26",
  });

  // Initial / empty-search: load straight from DB (all active codes)
  const loadAll = () => {
    setLoading(true);
    api.adminHSCodes()
      .then((data) => {
        const arr = Array.isArray(data) ? data : [];
        setCodes(arr.map(mapCode));
      })
      .catch((err) => showToast(err?.message || "Failed to load HS codes.", "error"))
      .finally(() => setLoading(false));
  };

  // Typed search: merge DB + live-API results
  const searchCodes = (term) => {
    setLoading(true);
    api.hsCodes(term, 100)
      .then((data) => {
        const arr = data?.items ?? (Array.isArray(data) ? data : []);
        setCodes(arr.map(mapCode));
      })
      .catch((err) => showToast(err?.message || "Search failed.", "error"))
      .finally(() => setLoading(false));
  };

  useEffect(() => { loadAll(); }, []);

  useEffect(() => {
    if (searchTerm.length === 0) { loadAll(); return; }
    if (searchTerm.length < 2) return;
    const timer = setTimeout(() => searchCodes(searchTerm), 400);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  const refresh = () => {
    if (searchTerm.length >= 2) searchCodes(searchTerm);
    else loadAll();
  };

  const importCsv = async () => {
    try {
      await api.createHSCode({ code: "63079000", description: "Other made-up textile articles", customsDutyPct: 10, salesTaxPct: 17, aitPct: 5.5 });
      await api.createHSCode({ code: "42022100", description: "Handbags with shoulder strap", customsDutyPct: 20, salesTaxPct: 18, aitPct: 5.5 });
      showToast("FBR tariff CSV imported — 2 new HS codes added.");
      refresh();
    } catch (err) {
      showToast(err?.message || "Import failed.", "error");
    }
  };

  const saveCode = async () => {
    if (!form.code || !form.desc) { showToast("HS code and description required.", "error"); return; }
    try {
      const payload = {
        code: form.code,
        description: form.desc,
        customsDutyPct: Number(form.customs) || 0,
        salesTaxPct: Number(form.salesTax) || 0,
        aitPct: Number(form.ait) || 0,
      };

      if (editMode) {
        await api.updateHSCode(currentId, payload);
        showToast("HS code updated.");
      } else {
        await api.createHSCode(payload);
        showToast("HS code saved.");
      }

      setForm({ code: "", desc: "", customs: "", salesTax: "", ait: "" });
      setShowAdd(false);
      setEditMode(false);
      refresh();
    } catch (err) {
      showToast(err?.message || "Save failed.", "error");
    }
  };

  const openEdit = (c) => {
    setForm({ code: c.code, desc: c.desc, customs: c.customs, salesTax: c.salesTax, ait: c.ait });
    setCurrentId(c.id);
    setEditMode(true);
    setShowAdd(true);
  };

  const deleteCode = async (item) => {
    if (!window.confirm("Are you sure you want to delete this HS Code?")) return;
    try {
      await api.deleteHSCode(item.id);
      showToast(`HS code ${item.code} removed.`);
      refresh();
    } catch (err) {
      showToast(err?.message || "Delete failed.", "error");
    }
  };

  return (
    <div className="admin-page">
      <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>

      <div className="card">
        <div className="user-management-head" style={{ alignItems: "center", display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: "1rem" }}>
          <div>
            <h2>HS Tariff Database Admin</h2>
            <p>FBR Tariff FY 2025-26 · {codes.length} code{codes.length !== 1 ? "s" : ""} loaded · Search DB &amp; Live API</p>
          </div>
          <div className="search-wrap" style={{ flex: 1, minWidth: "250px", maxWidth: "400px" }}>
            <input
              type="text"
              className="form-input"
              placeholder="Search HS code or description…"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <div className="user-mgmt-actions">
            <button type="button" className="btn-ghost" onClick={importCsv}>Import Sample</button>
            <button type="button" className="btn-primary" onClick={() => { setForm({ code: "", desc: "", customs: "", salesTax: "", ait: "" }); setEditMode(false); setShowAdd(true); }}>+ Add HS Code</button>
          </div>
        </div>

        {loading ? (
          <p className="muted-text" style={{ padding: "1rem" }}>Loading HS codes…</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>HS CODE</th>
                  <th>DESCRIPTION</th>
                  <th>CUSTOMS %</th>
                  <th>SALES TAX %</th>
                  <th>AIT %</th>
                  <th>ACTIONS</th>
                </tr>
              </thead>
              <tbody>
                {codes.length === 0 && (
                  <tr><td colSpan={6} style={{ textAlign: "center", padding: "2rem", color: "var(--muted)" }}>
                    {searchTerm.length >= 2 ? "No results found. Try a different search." : "No HS codes in database yet."}
                  </td></tr>
                )}
                {codes.map((c) => (
                  <tr key={c.code}>
                    <td className="hs-code-cell">{c.code}</td>
                    <td>{c.desc}</td>
                    <td className="center">{c.customs}%</td>
                    <td className="center">{c.salesTax}%</td>
                    <td className="center">{c.ait}%</td>
                    <td>
                      <button type="button" className="btn-outline-sm" onClick={() => openEdit(c)} style={{marginRight: '8px'}}>Edit</button>
                      <button type="button" className="btn-outline-sm danger-text" onClick={() => deleteCode(c)}>Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showAdd && (
        <div className="modal-overlay" onClick={() => { setShowAdd(false); setEditMode(false); }}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{editMode ? "Edit HS Code" : "Add HS Code"}</h3>
              <button type="button" className="icon-btn" onClick={() => { setShowAdd(false); setEditMode(false); }}>✕</button>
            </div>
            <div className="modal-body">
              <label className="form-group"><span className="form-label">HS Code</span><input className="form-input" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></label>
              <label className="form-group"><span className="form-label">Description</span><input className="form-input" value={form.desc} onChange={(e) => setForm({ ...form, desc: e.target.value })} /></label>
              <div className="form-row">
                <label className="form-group flex-1"><span className="form-label">Customs %</span><input className="form-input" type="number" value={form.customs} onChange={(e) => setForm({ ...form, customs: e.target.value })} /></label>
                <label className="form-group flex-1"><span className="form-label">Sales Tax %</span><input className="form-input" type="number" value={form.salesTax} onChange={(e) => setForm({ ...form, salesTax: e.target.value })} /></label>
                <label className="form-group flex-1"><span className="form-label">AIT %</span><input className="form-input" type="number" value={form.ait} onChange={(e) => setForm({ ...form, ait: e.target.value })} /></label>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => { setShowAdd(false); setEditMode(false); }}>Cancel</button>
              <button type="button" className="btn-primary" onClick={saveCode}>{editMode ? "Save Changes" : "Save"}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
