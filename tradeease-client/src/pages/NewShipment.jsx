import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";
import ProductSearchDropdown, { CountrySelect } from "../components/ProductSearchDropdown";
import PaymentModal from "../components/PaymentModal";

function matchCountryCode(partyCountry, countries) {
  if (!partyCountry) return "PK";
  const lower = partyCountry.toLowerCase();
  const byName = countries.find((c) => c.name.toLowerCase() === lower);
  if (byName) return byName.code;
  const byCode = countries.find((c) => c.code.toLowerCase() === lower);
  if (byCode) return byCode.code;
  const partial = countries.find((c) => lower.includes(c.name.toLowerCase()) || c.name.toLowerCase().includes(lower));
  return partial?.code || "PK";
}

function normalizeUnit(unit) {
  const u = (unit || "KG").toUpperCase();
  if (["KG", "MT", "PCS", "CTN", "L", "NOS", "MTR", "PRS", "GRM", "SQM", "CBM", "LTR"].includes(u)) return u;
  if (u === "KGS") return "KG";
  return "KG";
}

export default function NewShipment({ setActivePage, onAddParty }) {
  const showToast = useToast();
  const [type, setType] = useState("Import");
  const [parties, setParties] = useState([]);
  const [partyId, setPartyId] = useState("");
  const [countries, setCountries] = useState([]);
  const [countryInfo, setCountryInfo] = useState({ total: 0, source: "" });
  const [originPorts, setOriginPorts] = useState([]);
  const [destPorts, setDestPorts] = useState([]);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [forexCurrencies, setForexCurrencies] = useState(["USD", "PKR", "AED", "CNY", "SAR", "EUR", "GBP"]);
  const [exchangeRate, setExchangeRate] = useState(282.5);
  const [dutyPreview, setDutyPreview] = useState(null);
  const [dutyMeta, setDutyMeta] = useState(null);
  const [saving, setSaving] = useState(false);
  const [showPayment, setShowPayment] = useState(false);
  const [form, setForm] = useState({
    productDesc: "",
    hsCode: "",
    quantity: "",
    unit: "KG",
    invoiceValue: "",
    currency: "USD",
    originCountry: "CN",
    originPort: "",
    destCountry: "PK",
    destPort: "",
    shipmentDate: "",
    clearanceDate: "",
    notes: "",
  });

  const set = (key, val) => setForm((f) => ({ ...f, [key]: val }));

  const loadPorts = async (countryCode, target) => {
    if (!countryCode) return;
    try {
      const ports = await api.referencePorts(countryCode);
      if (target === "origin") {
        setOriginPorts(ports);
        setForm((f) => ({ ...f, originPort: f.originPort || ports[0]?.name || "" }));
      } else {
        setDestPorts(ports);
        setForm((f) => ({ ...f, destPort: f.destPort || ports[0]?.name || "" }));
      }
    } catch {
      if (target === "origin") setOriginPorts([]);
      else setDestPorts([]);
    }
  };

  useEffect(() => {
    // Load preferences or draft
    api.getShipmentDraft().then((draft) => {
      if (draft && draft.form) {
        setType(draft.type || "Import");
        setPartyId(draft.partyId || "");
        setForm(draft.form);
      } else {
        loadPreferences();
      }
    }).catch(() => {
      loadPreferences();
    });

    const loadPreferences = () => {
      const prefs = localStorage.getItem("tradeease_shipping_prefs");
      if (prefs) {
        try {
          const parsed = JSON.parse(prefs);
          setType(parsed.shipmentType || "Import");
          setForm(f => ({ ...f, currency: parsed.currency || "USD" }));
        } catch(e) {}
      }
    };
  }, []);

  useEffect(() => {
    api.referenceCountries()
      .then((data) => {
        setCountries(data);
        setCountryInfo({
          total: data.length,
          source: "embedded ISO list (ports load on demand)",
        });
      })
      .catch(() => {});

    loadPorts("CN", "origin");
    loadPorts("PK", "dest");
  }, []);

  const PK_CURRENCIES = ["USD", "PKR", "AED", "CNY", "SAR", "EUR", "GBP"];

  const loadForex = () => {
    api.forex().then((data) => {
      const rates = data?.rates ?? (Array.isArray(data) ? data : []);
      const codes = rates.map((r) => r.currencyCode).filter(Boolean);
      setForexCurrencies([...new Set([...PK_CURRENCIES, ...codes])]);
      const usd = rates.find((r) => r.currencyCode === "USD");
      if (usd) setExchangeRate(Number(usd.exchangeRate));
    }).catch(() => {});
  };

  useEffect(() => {
    api.tradeParties()
      .then((data) => {
        const filtered = data.filter((p) => type === "Export" ? p.partyType === "Buyer" : p.partyType === "Supplier");
        setParties(filtered);
        if (filtered.length > 0) setPartyId(String(filtered[0].partyId));
      })
      .catch(() => {});

    loadForex();
    const forexTimer = window.setInterval(loadForex, 5 * 60 * 1000);
    return () => window.clearInterval(forexTimer);
  }, [type]);

  useEffect(() => {
    if (countries.length === 0) return;
    const party = parties.find((p) => String(p.partyId) === partyId);
    const partyCode = matchCountryCode(party?.country, countries);
    if (type === "Export") {
      setForm((f) => ({ ...f, originCountry: "PK", destCountry: partyCode === "PK" ? "AE" : partyCode }));
      loadPorts("PK", "origin");
      loadPorts(partyCode === "PK" ? "AE" : partyCode, "dest");
    } else {
      const originCode = partyCode === "PK" ? "CN" : partyCode;
      setForm((f) => ({ ...f, originCountry: originCode, destCountry: "PK" }));
      loadPorts(originCode, "origin");
      loadPorts("PK", "dest");
    }
  }, [type, partyId, parties, countries]);

  useEffect(() => {
    loadPorts(form.originCountry, "origin");
  }, [form.originCountry]);

  useEffect(() => {
    loadPorts(form.destCountry, "dest");
  }, [form.destCountry]);

  useEffect(() => {
    if (!form.hsCode || !form.invoiceValue) {
      setDutyPreview(null);
      setDutyMeta(null);
      return;
    }
    api.calculateDuty({
      hsCode: form.hsCode,
      invoiceValue: parseFloat(form.invoiceValue),
      currency: form.currency,
      shipmentType: type,
      originCountry: form.originCountry,
      destinationCountry: form.destCountry,
    })
      .then((r) => {
        setDutyMeta({ regime: r.tariffRegime, source: r.tariffSource, live: r.liveApi, route: r.tradeRoute });
        if (type === "Import") {
          setDutyPreview(`PKR ${Number(r.totalLandedCostPkr).toLocaleString("en-PK", { maximumFractionDigits: 2 })} landed`);
        } else {
          const pct = r.destinationDutyPct != null ? `${r.destinationDutyPct}%` : "—";
          setDutyPreview(`Destination import duty ${pct} · est. PKR ${Number(r.totalDuty).toLocaleString("en-PK", { maximumFractionDigits: 2 })} at ${form.destCountry}`);
        }
        if (r.productHash && selectedProduct) {
          setSelectedProduct((p) => (p ? { ...p, productHash: r.productHash } : p));
        }
      })
      .catch(() => {
        setDutyPreview(null);
        setDutyMeta(null);
      });
  }, [form.hsCode, form.invoiceValue, form.currency, type, form.originCountry, form.destCountry]);

  useEffect(() => {
    const timer = setTimeout(() => {
      if (form.productDesc || form.invoiceValue || form.quantity) {
        api.saveShipmentDraft({ type, partyId, form }).catch(() => {});
      }
    }, 2000);
    return () => clearTimeout(timer);
  }, [type, partyId, form]);

  const tradeContext = {
    shipmentType: type,
    originCountry: form.originCountry,
    destinationCountry: form.destCountry,
  };

  const onProductSelect = (product) => {
    setSelectedProduct(product);
    if (!product) {
      setForm((f) => ({ ...f, productDesc: "", hsCode: "", unit: "KG" }));
      return;
    }
    setForm((f) => ({
      ...f,
      productDesc: product.description,
      hsCode: product.code,
      unit: normalizeUnit(product.unit),
    }));
  };

  const createShipment = async () => {
    if (!selectedProduct || !form.quantity || !form.invoiceValue || !form.originPort || !form.destPort || !form.shipmentDate) {
      showToast("Select a product from search and fill quantity, value, and dates.", "error");
      return;
    }
    setSaving(true);
    try {
      const result = await api.createShipment({
        shipmentType: type,
        partyId: partyId ? parseInt(partyId, 10) : null,
        productDesc: form.productDesc,
        hsCode: form.hsCode,
        quantity: parseFloat(form.quantity),
        unitOfMeasure: form.unit,
        invoiceValue: parseFloat(form.invoiceValue),
        currency: form.currency,
        originCountry: form.originCountry,
        originPort: form.originPort,
        destinationCountry: form.destCountry,
        destinationPort: form.destPort,
        shipmentDate: form.shipmentDate,
        clearanceDate: form.clearanceDate || null,
        notes: form.notes || null,
      });
      showToast(`Shipment ${result.reference} created successfully.`);
      api.clearShipmentDraft().catch(() => {});
      setActivePage("shipments");
    } catch (err) {
      showToast("Shipment creation failed.", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleStartBooking = () => {
    if (!partyId) {
      showToast("Please select a Trade Party.", "error");
      return;
    }
    if (!form.productDesc || !form.quantity || !form.invoiceValue) {
      showToast("Please fill all required shipment details.", "error");
      return;
    }
    setShowPayment(true);
  };

  const selectedParty = parties.find((p) => String(p.partyId) === partyId);
  const countryName = (code) => countries.find((c) => c.code === code)?.name || code;

  return (
    <div className="new-shipment-page">
      <div className="shipment-form-col">
        <div className="card">
          <h3 className="section-title">Shipment Type</h3>
          <div className="type-toggle">
            <button type="button" className={`type-btn ${type === "Import" ? "active" : ""}`} onClick={() => setType("Import")}>
              Import {type === "Import" && <span className="check-mark">✓</span>}
            </button>
            <button type="button" className={`type-btn ${type === "Export" ? "active" : ""}`} onClick={() => setType("Export")}>Export</button>
          </div>
          <p className="form-hint">
            Imports clear at KPT / Port Qasim · {countryInfo.total || countries.length} countries (live) · any HS product via live tariff API
          </p>
        </div>

        <div className="card">
          <h3 className="section-title">Shipment Details</h3>

          <div className="form-group">
            <label className="form-label">Trade Party <span className="required">*</span></label>
            <select className="form-select" value={partyId} onChange={(e) => setPartyId(e.target.value)}>
              {parties.length === 0 ? (
                <option value="">No parties — add one first</option>
              ) : parties.map((p) => (
                <option key={p.partyId} value={p.partyId}>
                  {p.name} ({p.country || "Pakistan"} · {p.partyType})
                </option>
              ))}
            </select>
            <div className="form-hint">
              Only {type === "Export" ? "Buyer" : "Supplier"}-type parties shown.{" "}
              <button type="button" className="link-text-btn" onClick={onAddParty}>+ Add new party</button>
            </div>
          </div>

          <ProductSearchDropdown tradeContext={tradeContext} onSelect={onProductSelect} />

          <div className="form-row">
            <div className="form-group flex-1">
              <label className="form-label">Quantity <span className="required">*</span></label>
              <input className="form-input" placeholder="20000" value={form.quantity} onChange={(e) => set("quantity", e.target.value)} />
            </div>
            <div className="form-group w-120">
              <label className="form-label">Unit</label>
              <input className="form-input" value={form.unit} readOnly />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group flex-1">
              <label className="form-label">Invoice Value <span className="required">*</span></label>
              <input className="form-input" placeholder="10000.00" value={form.invoiceValue} onChange={(e) => set("invoiceValue", e.target.value)} />
            </div>
            <div className="form-group w-120">
              <label className="form-label">Currency <span className="required">*</span></label>
              <select className="form-select" value={form.currency} onChange={(e) => set("currency", e.target.value)}>
                {forexCurrencies.map((c) => <option key={c}>{c}</option>)}
              </select>
            </div>
          </div>

          <div className="form-row">
            <div className="form-group flex-1">
              <CountrySelect
                label={type === "Import" ? "Origin Country (supplier) *" : "Origin Country *"}
                countries={countries}
                value={form.originCountry}
                onChange={(v) => set("originCountry", v)}
                disabled={type === "Export"}
                ports={originPorts}
                portValue={form.originPort}
                onPortChange={(v) => set("originPort", v)}
                liveHint={type === "Import" ? "Select supplier country · live tariff applied on import to Pakistan" : undefined}
              />
            </div>
            <div className="form-group flex-1">
              <CountrySelect
                label={type === "Export" ? "Destination Country (buyer) *" : "Destination Country *"}
                countries={countries}
                value={form.destCountry}
                onChange={(v) => set("destCountry", v)}
                disabled={type === "Import"}
                ports={destPorts}
                portValue={form.destPort}
                onPortChange={(v) => set("destPort", v)}
                liveHint={type === "Export" ? "Select buyer country · live destination import duty shown" : undefined}
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group flex-1">
              <label className="form-label">Shipment Date <span className="required">*</span></label>
              <input type="date" className="form-input" value={form.shipmentDate} onChange={(e) => set("shipmentDate", e.target.value)} />
            </div>
            <div className="form-group flex-1">
              <label className="form-label">Expected Clearance Date</label>
              <input type="date" className="form-input" value={form.clearanceDate} onChange={(e) => set("clearanceDate", e.target.value)} />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Internal Notes</label>
            <textarea className="form-textarea" placeholder="Additional notes..." value={form.notes} onChange={(e) => set("notes", e.target.value)} rows={3} />
          </div>
        </div>
      </div>

      <div className="shipment-summary-col">
        <div className="card summary-card">
          <h3>Summary</h3>
          <div className="summary-item"><span className="s-label">Reference</span><span className="s-val muted">Auto-generated on save</span></div>
          <div className="summary-item"><span className="s-label">Type</span><span className="s-val">{type}</span></div>
          <div className="summary-item"><span className="s-label">Party</span><span className="s-val">{selectedParty?.name || "—"}</span></div>
          <div className="summary-item"><span className="s-label">Route</span><span className="s-val">{countryName(form.originCountry)} → {countryName(form.destCountry)}</span></div>
          {selectedProduct && (
            <div className="summary-item"><span className="s-label">Route Product Hash</span><code className="hash-chip">{selectedProduct.productHash}</code></div>
          )}
          {dutyMeta?.source && (
            <div className="summary-item"><span className="s-label">Tariff source</span><span className="s-val">{dutyMeta.source}{dutyMeta.live ? " · live" : ""}</span></div>
          )}
          <div className="summary-item"><span className="s-label">USD/PKR (indicative)</span><span className="s-val">{exchangeRate}</span></div>
        </div>

        <div className="card duty-card">
          <h3>{type === "Import" ? "Live Import Duty (FBR)" : "Live Destination Duty (Export)"}</h3>
          {dutyPreview ? (
            <>
              <div className="duty-estimate">{dutyPreview}</div>
              {dutyMeta?.regime && <p className="form-hint">{dutyMeta.regime}</p>}
            </>
          ) : (
            <p className="muted-text">
              Select product and countries — hash and duty refresh automatically from live APIs when route changes
            </p>
          )}
        </div>

        <div className="card actions-card">
          <h3>Actions</h3>
          <button type="button" className="btn-primary full-width" onClick={handleStartBooking} disabled={saving}>
            {saving ? "Creating..." : "Proceed to Payment"}
          </button>
          <button type="button" className="btn-ghost full-width mt-8" onClick={() => setActivePage("shipments")}>Cancel</button>
        </div>
      </div>
      {showPayment && (
        <PaymentModal
          amount={50.00} // $50 platform booking fee
          currency="usd"
          onSuccess={() => {
            setShowPayment(false);
            createShipment();
          }}
          onCancel={() => setShowPayment(false)}
        />
      )}
    </div>
  );
}
