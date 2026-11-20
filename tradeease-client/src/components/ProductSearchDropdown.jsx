import { useEffect, useRef, useState } from "react";
import { api } from "../api/client";

function useDebounced(value, ms = 300) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = window.setTimeout(() => setDebounced(value), ms);
    return () => window.clearTimeout(t);
  }, [value, ms]);
  return debounced;
}

const PINNED_COUNTRIES = ["PK", "CN", "AE", "SA", "US", "GB", "DE", "IN", "AF", "IR"];

function sortCountries(countries) {
  return [...countries].sort((a, b) => {
    const ai = PINNED_COUNTRIES.indexOf(a.code);
    const bi = PINNED_COUNTRIES.indexOf(b.code);
    if (ai !== -1 || bi !== -1) {
      if (ai === -1) return 1;
      if (bi === -1) return -1;
      return ai - bi;
    }
    return a.name.localeCompare(b.name);
  });
}

export default function ProductSearchDropdown({
  onSelect,
  tradeContext,
  placeholder = "Search any product or HS code (live worldwide harmonized tariff)...",
}) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [total, setTotal] = useState(0);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState(null);
  const [searchMeta, setSearchMeta] = useState({ source: "", liveApiMerged: false, tradeRoute: "", tariffRegime: "" });
  const debounced = useDebounced(query);
  const wrapRef = useRef(null);
  const selectedCodeRef = useRef(null);

  useEffect(() => {
    if (!debounced || debounced.trim().length < 2) {
      setResults([]);
      setTotal(0);
      setLoading(false);
      return undefined;
    }

    setLoading(true);
    api.productSearch(debounced, 40, tradeContext || {})
      .then((data) => {
        setResults(data.items || []);
        setTotal(data.total || 0);
        setSearchMeta({
          source: data.source || "",
          liveApiMerged: !!data.liveApiMerged,
          tradeRoute: data.tradeRoute || "",
          tariffRegime: data.tariffRegime || "",
        });
      })
      .catch(() => setResults([]))
      .finally(() => setLoading(false));
  }, [
    debounced,
    tradeContext?.shipmentType,
    tradeContext?.originCountry,
    tradeContext?.destinationCountry,
  ]);

  useEffect(() => {
    const code = selectedCodeRef.current;
    if (!code || !tradeContext) return;
    api.resolveProduct(code, tradeContext)
      .then((product) => {
        setSelected(product);
        onSelect?.(product);
      })
      .catch(() => {});
  }, [
    tradeContext?.shipmentType,
    tradeContext?.originCountry,
    tradeContext?.destinationCountry,
  ]);

  useEffect(() => {
    const onDoc = (e) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, []);

  const pick = (product) => {
    selectedCodeRef.current = product.code;
    setSelected(product);
    setQuery(product.description);
    setOpen(false);
    onSelect?.(product);
  };

  const clear = () => {
    selectedCodeRef.current = null;
    setSelected(null);
    setQuery("");
    onSelect?.(null);
  };

  const isImport = tradeContext?.shipmentType === "Import";
  const dutyLabel = isImport ? "CD" : "Dest. duty";

  return (
    <div className="product-search" ref={wrapRef}>
      <label className="form-label">
        Product <span className="required">*</span>
        <span className="muted-text">
          {" "}· live catalog · {total}+ lines
          {searchMeta.tradeRoute && ` · ${searchMeta.tradeRoute}`}
          {searchMeta.liveApiMerged && " · live API"}
        </span>
      </label>
      {searchMeta.tariffRegime && (
        <div className="form-hint">{searchMeta.tariffRegime}{searchMeta.source ? ` · ${searchMeta.source}` : ""}</div>
      )}
      <div className="product-search-input-wrap">
        <input
          className="form-input"
          placeholder={placeholder}
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); setSelected(null); selectedCodeRef.current = null; }}
          onFocus={() => setOpen(true)}
        />
        {selected && (
          <button type="button" className="link-text-btn product-clear-btn" onClick={clear}>Clear</button>
        )}
      </div>

      {open && (query.length > 0 || results.length > 0) && (
        <div className="product-search-dropdown">
          {loading && <div className="product-search-item muted-text">Fetching live tariff for selected route...</div>}
          {!loading && results.length === 0 && (
            <div className="product-search-item muted-text">No products found — try another keyword</div>
          )}
          {!loading && results.map((p) => (
            <button
              key={`${p.code}-${p.productHash}`}
              type="button"
              className={`product-search-item ${selected?.code === p.code ? "selected" : ""}`}
              onClick={() => pick(p)}
            >
              <strong>{p.description}</strong>
              <span className="product-search-meta">
                HS {p.code} · Hash <code>{p.productHash}</code> · {dutyLabel} {p.customsDutyPct}%
                {p.liveApi && " · live"}
              </span>
            </button>
          ))}
        </div>
      )}

      {selected && (
        <div className="product-selected-card">
          <div><span className="s-label">HS Code</span> <strong>{selected.code}</strong></div>
          <div><span className="s-label">Route Product Hash</span> <code className="hash-chip">{selected.productHash}</code></div>
          <div><span className="s-label">Tariff</span> {selected.tariffRegime || (isImport ? "FBR import" : "Export destination")}</div>
          <div><span className="s-label">Source</span> {selected.tariffSource || searchMeta.source || "live catalog"}</div>
          {isImport ? (
            <div><span className="s-label">FBR rates</span> CD {selected.customsDutyPct}% · ST {selected.salesTaxPct}% · AIT {selected.aitPct}%</div>
          ) : (
            <div><span className="s-label">Destination ({selected.destinationCountryCode})</span> import duty {selected.destinationDutyPct ?? selected.customsDutyPct}%</div>
          )}
          <div><span className="s-label">Unit</span> {selected.unitOfMeasure || selected.unit}</div>
        </div>
      )}
    </div>
  );
}

export function CountrySelect({
  label,
  countries,
  value,
  onChange,
  disabled = false,
  ports = [],
  portValue,
  onPortChange,
  liveHint,
}) {
  const [filter, setFilter] = useState("");
  const sorted = sortCountries(countries);
  const filtered = sorted.filter((c) =>
    c.name.toLowerCase().includes(filter.toLowerCase()) || c.code.toLowerCase().includes(filter.toLowerCase()));

  const country = countries.find((c) => c.code === value);

  return (
    <>
      <div className="form-group">
        <label className="form-label">{label}</label>
        {liveHint && <div className="form-hint">{liveHint}</div>}
        {!disabled && (
          <input
            className="form-input"
            placeholder="Filter countries..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            style={{ marginBottom: 8 }}
          />
        )}
        <select className="form-select" value={value} onChange={(e) => onChange(e.target.value)} disabled={disabled}>
          {filtered.map((c) => (
            <option key={c.code} value={c.code}>{c.name} ({c.code}) · {c.region}</option>
          ))}
        </select>
        {disabled && country && (
          <div className="form-hint">Pakistan fixed for this shipment type · tariff applied at PK customs</div>
        )}
        {country && !disabled && (
          <div className="form-hint">{ports.length} ports available · {countries.length} countries</div>
        )}
      </div>
      <div className="form-group">
        <label className="form-label">Port <span className="required">*</span></label>
        <select className="form-select" value={portValue} onChange={(e) => onPortChange(e.target.value)}>
          {ports.map((p) => (
            <option key={p.name} value={p.name}>{p.name} ({p.type})</option>
          ))}
        </select>
      </div>
    </>
  );
}
