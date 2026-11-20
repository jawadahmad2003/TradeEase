import { useCallback, useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

const PINNED_COUNTRIES = ["PK", "CN", "AE", "SA", "US", "GB", "DE", "IN"];

function defaultCountryForType(type) {
  if (type === "Supplier") return "CN";
  if (type === "Buyer") return "AE";
  return "PK";
}

function typeClass(type) {
  if (type === "Buyer") return "tag-buyer";
  if (type === "Supplier") return "tag-supplier";
  return "tag-agent";
}

function mapParty(p) {
  return {
    id: p.partyId,
    ref: `TP-${String(p.partyId).padStart(3, "0")}`,
    name: p.name,
    type: p.partyType,
    typeClass: typeClass(p.partyType),
    location: [p.city, p.country].filter(Boolean).join(", ") || "Pakistan",
    email: p.email || "—",
    phone: p.phone || "—",
    bank: "—",
    ntn: p.taxId || "—",
    transactions: 0,
    value: "PKR 0.00",
  };
}

export default function TradeParties({ openAddOnMount = false, onAddModalClose }) {
  const showToast = useToast();
  const [parties, setParties] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("All Types");
  const [selected, setSelected] = useState(null);
  const [showAdd, setShowAdd] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [newParty, setNewParty] = useState({ id: null, name: "", type: "Supplier", country: "CN", email: "", bank: "", ntn: "" });
  const [countries, setCountries] = useState([]);

  const loadParties = useCallback(() => {
    setLoading(true);
    api.tradeParties()
      .then((data) => setParties(data.map(mapParty)))
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => { loadParties(); }, [loadParties]);

  useEffect(() => {
    api.referenceCountries()
      .then((list) => setCountries(Array.isArray(list) ? list : []))
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (openAddOnMount) {
      setShowAdd(true);
      onAddModalClose?.();
    }
  }, [openAddOnMount, onAddModalClose]);

  const saveParty = async () => {
    if (!newParty.name.trim()) {
      showToast("Party name is required.", "error");
      return;
    }
    try {
      const payload = {
        partyType: newParty.type,
        name: newParty.name,
        country: countries.find((c) => c.code === newParty.country)?.name || newParty.country || "China",
        email: newParty.email || null,
        taxId: newParty.ntn || null,
      };

      if (editMode && newParty.id) {
        await api.updateTradeParty(newParty.id, payload);
        showToast(`${newParty.name} updated successfully.`);
      } else {
        await api.createTradeParty(payload);
        showToast(`${newParty.name} added successfully.`);
      }

      setShowAdd(false);
      setEditMode(false);
      setNewParty({ id: null, name: "", type: "Supplier", country: "CN", email: "", bank: "", ntn: "" });
      loadParties();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const deleteParty = async (id) => {
    if (!window.confirm("Are you sure you want to delete this trade party?")) return;
    try {
      await api.deleteTradeParty(id);
      showToast("Trade party deleted.");
      setSelected(null);
      loadParties();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const openEdit = (party) => {
    setNewParty({
      id: party.id,
      name: party.name,
      type: party.type,
      country: countries.find(c => c.name === party.location.split(",").pop().trim())?.code || "CN",
      email: party.email === "—" ? "" : party.email,
      ntn: party.ntn === "—" ? "" : party.ntn,
      bank: ""
    });
    setEditMode(true);
    setShowAdd(true);
    setSelected(null);
  };


  const filtered = parties.filter((p) => {
    const matchSearch = p.name.toLowerCase().includes(search.toLowerCase()) || p.email.toLowerCase().includes(search.toLowerCase());
    const matchType = typeFilter === "All Types" || p.type === typeFilter;
    return matchSearch && matchType;
  });

  const counts = {
    buyers: parties.filter((p) => p.type === "Buyer").length,
    suppliers: parties.filter((p) => p.type === "Supplier").length,
    agents: parties.filter((p) => p.type === "Agent" || p.type === "Forwarder").length,
  };

  if (loading) return <div className="card"><p className="muted-text">Loading trade parties...</p></div>;

  return (
    <div className="trade-parties">
      <div className="party-summary-bar card">
        <div className="party-summary-item"><strong>{counts.buyers}</strong><span>Buyers</span></div>
        <div className="party-summary-item"><strong>{counts.suppliers}</strong><span>Suppliers</span></div>
        <div className="party-summary-item"><strong>{counts.agents}</strong><span>Agents</span></div>
        <div className="party-summary-item"><strong>{parties.length}</strong><span>Total Parties</span></div>
      </div>
      <div className="filters-bar">
        <div className="search-wrap flex-1">
          <input className="search-input" placeholder="Search by name, contact, or country..." value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="filter-select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option>All Types</option>
          <option>Buyer</option>
          <option>Supplier</option>
          <option>Agent</option>
        </select>
        <button type="button" className="btn-primary" onClick={() => setShowAdd(true)}>Add Party</button>
      </div>

      <div className="parties-grid">
        {filtered.length === 0 ? (
          <div className="card"><p className="muted-text">No trade parties yet. Add your first buyer or supplier.</p></div>
        ) : filtered.map((p) => (
          <div key={p.id} className="party-card clickable" onClick={() => setSelected(p)}>
            <div className="party-header">
              <div>
                <div className="party-name-row">
                  <span className="party-name">{p.name}</span>
                  <span className={`type-tag ${p.typeClass}`}>{p.type}</span>
                </div>
                <div className="party-id">{p.ref}</div>
              </div>
            </div>
            <div className="party-divider" />
            <div className="party-details">
              <div className="party-detail"><span>{p.location}</span></div>
              <div className="party-detail"><span>{p.email}</span></div>
              <div className="party-detail"><span>{p.phone}</span></div>
            </div>
            <div className="party-stats">
              <div className="party-stat">
                <div className="stat-label-sm">Shipments</div>
                <div className="stat-num">{p.transactions}</div>
              </div>
              <div className="party-stat">
                <div className="stat-label-sm">Total Value</div>
                <div className="stat-val-green">{p.value}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {selected && (
        <div className="modal-overlay" onClick={() => setSelected(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{selected.name}</h3>
              <button type="button" className="icon-btn" onClick={() => setSelected(null)}>✕</button>
            </div>
            <div className="modal-body party-detail-body">
              <p><strong>Type:</strong> {selected.type}</p>
              <p><strong>Location:</strong> {selected.location}</p>
              <p><strong>Email:</strong> {selected.email}</p>
              <p><strong>Phone:</strong> {selected.phone}</p>
              <p><strong>Tax / NTN:</strong> {selected.ntn}</p>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => setSelected(null)}>Close</button>
              <button type="button" className="btn-ghost" onClick={() => openEdit(selected)}>Edit</button>
              <button type="button" className="btn-primary" style={{backgroundColor: '#e53e3e'}} onClick={() => deleteParty(selected.id)}>Delete</button>
            </div>
          </div>
        </div>
      )}

      {showAdd && (
        <div className="modal-overlay" onClick={() => setShowAdd(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{editMode ? "Edit Trade Party" : "Add Trade Party"}</h3>
              <button type="button" className="icon-btn" onClick={() => { setShowAdd(false); setEditMode(false); }}>✕</button>
            </div>
            <div className="modal-body">
              <label className="form-group"><span className="form-label">Name *</span><input className="form-input" placeholder="Company name" value={newParty.name} onChange={(e) => setNewParty({ ...newParty, name: e.target.value })} /></label>
              <label className="form-group"><span className="form-label">Type</span><select className="form-select" value={newParty.type} onChange={(e) => { const type = e.target.value; setNewParty({ ...newParty, type, country: defaultCountryForType(type) }); }}><option>Supplier</option><option>Buyer</option><option>Agent</option></select></label>
              <label className="form-group">
                <span className="form-label">Country</span>
                <select className="form-select" value={newParty.country} onChange={(e) => setNewParty({ ...newParty, country: e.target.value })}>
                  {[...countries].sort((a, b) => {
                    const ai = PINNED_COUNTRIES.indexOf(a.code);
                    const bi = PINNED_COUNTRIES.indexOf(b.code);
                    if (ai !== -1 || bi !== -1) return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi);
                    return a.name.localeCompare(b.name);
                  }).map((c) => (
                    <option key={c.code} value={c.code}>{c.name} ({c.code})</option>
                  ))}
                  {countries.length === 0 && (
                    <>
                      <option value="CN">China (CN)</option>
                      <option value="AE">United Arab Emirates (AE)</option>
                      <option value="PK">Pakistan (PK)</option>
                    </>
                  )}
                </select>
              </label>
              <label className="form-group"><span className="form-label">Email</span><input className="form-input" type="email" value={newParty.email} onChange={(e) => setNewParty({ ...newParty, email: e.target.value })} /></label>
              <label className="form-group"><span className="form-label">NTN / Tax ID</span><input className="form-input" value={newParty.ntn} onChange={(e) => setNewParty({ ...newParty, ntn: e.target.value })} /></label>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => { setShowAdd(false); setEditMode(false); }}>Cancel</button>
              <button type="button" className="btn-primary" onClick={saveParty}>{editMode ? "Save Changes" : "Save Party"}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
