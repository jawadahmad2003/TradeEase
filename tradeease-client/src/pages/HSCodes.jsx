import { useEffect, useState } from "react";
import { Icon } from "../components/Icons";
import { api } from "../api/client";
import { useToast } from "../components/Toast";
import ProductSearchDropdown from "../components/ProductSearchDropdown";

function unwrapForexRates(data) {
  return data?.rates ?? (Array.isArray(data) ? data : []);
}

export default function HSCodes({ setActivePage }) {
  const showToast = useToast();
  const [search, setSearch] = useState("");
  const [results, setResults] = useState([]);
  const [exchangeRate, setExchangeRate] = useState(282.5);
  const [invoiceCurrency, setInvoiceCurrency] = useState("USD");
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [invoiceValue, setInvoiceValue] = useState("10000");
  const [calc, setCalc] = useState(null);
  const [history, setHistory] = useState([]);

  const [totalCount, setTotalCount] = useState(0);

  const loadCodes = (q = "") => {
    const term = q.trim();
    if (term.length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    api.hsCodes(term, 50)
      .then((data) => {
        const items = data.items ?? (Array.isArray(data) ? data : []);
        setTotalCount(data.total ?? items.length);
        setResults(items.map((h) => ({
          hsCodeId: h.hsCodeId,
          code: h.code,
          desc: h.description,
          hash: h.productHash,
          customs: Number(h.customsDutyPct),
          salesTax: Number(h.salesTaxPct),
          ait: Number(h.aITPct ?? h.aitPct ?? h.ait),
          tariffYear: h.tariffYear,
        })));
      })
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    api.hsCodeCount().then((d) => setTotalCount(d.total || 0)).catch(() => {});
    api.forex().then((data) => {
      const rates = unwrapForexRates(data);
      const usd = rates.find((r) => r.currencyCode === "USD");
      if (usd) setExchangeRate(Number(usd.exchangeRate));
    }).catch(() => {});
    setLoading(false);
  }, []);

  const handleSearch = () => {
    loadCodes(search.trim());
    if (search.trim().length >= 4 && /^\d/.test(search.trim())) {
      setHistory((prev) => [search.trim(), ...prev.filter((h) => h !== search.trim())].slice(0, 5));
    }
  };

  const onProductPick = async (product) => {
    if (!product) return;
    const h = {
      hsCodeId: 0,
      code: product.code,
      desc: product.description,
      hash: product.productHash,
      customs: Number(product.customsDutyPct),
      salesTax: Number(product.salesTaxPct),
      ait: Number(product.aitPct),
      tariffYear: product.tariffYear,
    };
    setSearch(product.description);
    await selectCode(h);
  };

  const selectCode = async (h) => {
    setSelected(h);
    try {
      const result = await api.calculateDuty({
        hsCode: h.code,
        invoiceValue: parseFloat(invoiceValue) || 0,
        currency: invoiceCurrency,
      });
      setExchangeRate(Number(result.exchangeRate));
      setCalc({
        base: Number(result.totalValuePKR),
        regulatory: Number(result.regulatoryDuty || 0),
        additional: Number(result.additionalCustomsDuty || 0),
        customs: Number(result.customsDuty),
        salesTax: Number(result.salesTax),
        ait: Number(result.ait),
        totalTax: Number(result.totalDuty),
        grand: Number(result.totalLandedCostPkr ?? result.totalValuePKR + result.totalDuty),
        hash: result.productHash,
      });
      showToast(`HS code ${h.code} selected for duty calculation.`);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const recalc = async () => {
    if (!selected) return;
    await selectCode(selected);
  };

  useEffect(() => {
    if (selected && invoiceValue) recalc();
  }, [invoiceValue, invoiceCurrency]);

  return (
    <div className="hs-page">
      <div className="tag-banner">
        FBR Pakistan Customs Tariff · {totalCount}+ products (all HS chapters) · live search · USD/PKR {exchangeRate}
      </div>

      <div className="card" style={{ marginBottom: 16 }}>
        <ProductSearchDropdown
          placeholder="Type product name — e.g. cotton, rice, smartphone, motor car..."
          onSelect={onProductPick}
        />
      </div>

      <div className="hs-layout">
        <div className="card search-card flex-1">
          <div className="hs-search-row">
            <div className="search-wrap flex-1">
              <input
                className="search-input"
                placeholder="Search by product keyword or HS code..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              />
            </div>
            <button className="btn-primary" onClick={handleSearch}>Search</button>
          </div>

          {loading ? (
            <p className="muted-text" style={{ padding: 16 }}>Searching live harmonized tariff...</p>
          ) : results.length === 0 ? (
            <p className="muted-text" style={{ padding: 16 }}>
              Type at least 2 characters and press Search — or pick a product above.
            </p>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>HS Code</th>
                    <th>Description</th>
                    <th>Product Hash</th>
                    <th>Customs %</th>
                    <th>Sales Tax %</th>
                    <th>AIT %</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {results.map((h) => (
                    <tr key={h.code} className={selected?.code === h.code ? "row-selected" : ""}>
                      <td className="hs-code-cell">{h.code}</td>
                      <td>{h.desc}</td>
                      <td><code className="hash-chip">{h.hash || "—"}</code></td>
                      <td className="center">{h.customs}%</td>
                      <td className="center">{h.salesTax}%</td>
                      <td className="center">{h.ait}%</td>
                      <td>
                        <button type="button" className="btn-outline-sm" onClick={() => selectCode(h)}>Select</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card duty-calculator-card">
          <div className="duty-calc-head">
            <Icon name="calculator" size={22} />
            <h3>FBR Import Duty Calculator</h3>
          </div>
          {selected ? (
            <>
              <div className="calc-selected">
                <strong>{selected.code}</strong>
                <p>{selected.desc}</p>
              </div>
              <label className="form-group">
                <span className="form-label">Invoice Value</span>
                <div className="form-row">
                  <input className="form-input flex-1" type="number" value={invoiceValue} onChange={(e) => setInvoiceValue(e.target.value)} />
                  <select className="form-select w-120" value={invoiceCurrency} onChange={(e) => setInvoiceCurrency(e.target.value)}>
                    {["USD", "PKR", "AED", "CNY", "SAR", "EUR", "GBP"].map((c) => <option key={c}>{c}</option>)}
                  </select>
                </div>
              </label>
              {calc && (
                <div className="duty-breakdown">
                  <div className="breakdown-row"><span>Assessed Value (PKR)</span><strong>PKR {calc.base.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  {calc.regulatory > 0 && <div className="breakdown-row"><span>Regulatory Duty (RD)</span><strong>PKR {calc.regulatory.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>}
                  {calc.additional > 0 && <div className="breakdown-row"><span>Additional CD (ACD)</span><strong>PKR {calc.additional.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>}
                  <div className="breakdown-row"><span>Customs Duty (CD)</span><strong>PKR {calc.customs.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  <div className="breakdown-row"><span>Sales Tax (ST)</span><strong>PKR {calc.salesTax.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  <div className="breakdown-row"><span>Advance Income Tax (AIT)</span><strong>PKR {calc.ait.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  <div className="breakdown-row total"><span>Total Tax</span><strong>PKR {calc.totalTax.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  <div className="breakdown-row grand"><span>Total Landed Cost (PKR)</span><strong>PKR {calc.grand.toLocaleString("en-PK", { maximumFractionDigits: 2 })}</strong></div>
                  {calc.hash && <div className="breakdown-row"><span>WeBOC Product Hash</span><code className="hash-chip">{calc.hash}</code></div>}
                </div>
              )}
              {setActivePage && (
                <button type="button" className="btn-primary full-width mt-8" onClick={() => setActivePage("new-shipment")}>
                  Use in New Shipment
                </button>
              )}
            </>
          ) : (
            <p className="muted-text">Select an HS code from the table to calculate duty in PKR.</p>
          )}
        </div>
      </div>

      <div className="card history-card">
        <div className="card-header">
          <h3>Search History</h3>
          <button type="button" className="link-btn red" onClick={() => { setHistory([]); showToast("Search history cleared."); }}>Clear History</button>
        </div>
        <div className="history-chips">
          {history.map((h) => (
            <button key={h} type="button" className="history-chip" onClick={() => { setSearch(h); loadCodes(h); }}>{h}</button>
          ))}
          {history.length === 0 && <span className="muted-text">No recent searches</span>}
        </div>
      </div>
    </div>
  );
}
