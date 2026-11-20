import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

const FLAG_MAP = {
  USD: "🇺🇸", EUR: "🇪🇺", GBP: "🇬🇧", CNY: "🇨🇳", AED: "🇦🇪", SAR: "🇸🇦",
  JPY: "🇯🇵", SGD: "🇸🇬", CHF: "🇨🇭", CAD: "🇨🇦", AUD: "🇦🇺", INR: "🇮🇳",
  TRY: "🇹🇷", MYR: "🇲🇾", THB: "🇹🇭", KRW: "🇰🇷", NZD: "🇳🇿", QAR: "🇶🇦",
  KWD: "🇰🇼", BHD: "🇧🇭", OMR: "🇴🇲", SEK: "🇸🇪", NOK: "🇳🇴", DKK: "🇩🇰",
  PLN: "🇵🇱", BRL: "🇧🇷", MXN: "🇲🇽", ZAR: "🇿🇦", HKD: "🇭🇰", RUB: "🇷🇺",
};

function unwrapForex(data) {
  const rates = data?.rates ?? (Array.isArray(data) ? data : []);
  return {
    rates,
    lastUpdatedUtc: data?.lastUpdatedUtc,
    source: data?.source || "frankfurter.app",
    refreshIntervalSeconds: data?.refreshIntervalSeconds || 60,
  };
}

function mapRates(rates) {
  return rates.map((r) => ({
    flag: FLAG_MAP[r.currencyCode] || r.currencyCode.slice(0, 2),
    pair: `${r.currencyCode} / PKR`,
    code: r.currencyCode,
    rate: Number(r.exchangeRate),
    change: r.changePct != null ? `${r.changePct >= 0 ? "+" : ""}${Number(r.changePct).toFixed(3)}%` : "—",
    up: r.isUp ?? true,
  }));
}

export default function ForexRates() {
  const showToast = useToast();
  const [rates, setRates] = useState([]);
  const [lastUpdated, setLastUpdated] = useState("—");
  const [source, setSource] = useState("");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [amount, setAmount] = useState("5000");
  const [currency, setCurrency] = useState("USD");
  const [chartCurrency, setChartCurrency] = useState("USD");
  const [trendData, setTrendData] = useState([]);

  const applyForex = (data) => {
    const { rates: list, lastUpdatedUtc, source: src } = unwrapForex(data);
    setRates(mapRates(list));
    setSource(src);
    if (lastUpdatedUtc) {
      setLastUpdated(new Date(lastUpdatedUtc).toLocaleString("en-GB", {
        day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit",
      }) + " PKT");
    }
  };

  const loadRates = () => {
    setLoading(true);
    api.forex()
      .then(applyForex)
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadRates();
    const timer = window.setInterval(loadRates, 5 * 60 * 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!chartCurrency) return;
    api.forexTrend(chartCurrency)
      .then((points) => setTrendData(Array.isArray(points) ? points.map(Number) : []))
      .catch(() => setTrendData([]));
  }, [chartCurrency, rates.length]);

  const selectedRate = rates.find((r) => r.code === chartCurrency) || rates.find((r) => r.code === currency);
  const converted = selectedRate ? (parseFloat(amount) * selectedRate.rate).toLocaleString("en-PK") : "";

  const refreshRates = async () => {
    setRefreshing(true);
    try {
      const data = await api.refreshForexRates();
      applyForex(data);
      showToast("Live forex refreshed (in-memory, not saved to database).");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setRefreshing(false);
    }
  };

  const chartPoints = trendData.length >= 2 ? trendData : (selectedRate ? [selectedRate.rate * 0.998, selectedRate.rate] : []);
  const minVal = Math.min(...chartPoints);
  const maxVal = Math.max(...chartPoints);
  const range = maxVal - minVal || 1;
  const w = 400;
  const h = 100;
  const pad = 10;
  const points = chartPoints.map((v, i) => {
    const x = pad + (i / Math.max(chartPoints.length - 1, 1)) * (w - 2 * pad);
    const y = h - pad - ((v - minVal) / range) * (h - 2 * pad);
    return `${x},${y}`;
  }).join(" ");

  if (loading) return <div className="card"><p className="muted-text">Loading live forex rates...</p></div>;

  return (
    <div className="forex-page">
      <div className="forex-meta">
        <span>Last updated: <span className="link-text">{lastUpdated}</span> · auto-refresh every 60s</span>
        <button type="button" className="refresh-btn" onClick={refreshRates} disabled={refreshing}>
          {refreshing ? "Refreshing..." : "Refresh Now"}
        </button>
      </div>

      <div className="alert-banner info-banner">
        Indicative PKR rates for trade planning (USD, AED, CNY, SAR prioritised). Refreshed every minute — use official SBP / customs valuation rate for WeBOC filing. {source && `Source: ${source}`}
      </div>

      <div className="rates-grid">
        {rates.length === 0 ? (
          <div className="card" style={{ gridColumn: "1 / -1", padding: 20 }}>
            <p className="muted-text">No live rates yet. Click Refresh Now or wait for the background service (every 60s).</p>
          </div>
        ) : rates.map((r) => (
          <div key={r.pair} className="rate-card">
            <div className="rate-header">
              <span className="flag-badge">{r.flag}</span>
              <span className="rate-pair">{r.pair}</span>
            </div>
            <div className="rate-value">{r.rate.toFixed(2)}</div>
            <div className={`rate-change ${r.up ? "up" : "down"}`}>
              {r.up ? "↗" : "↘"} {r.change} <span className="vs-text">vs prior refresh</span>
            </div>
          </div>
        ))}
      </div>

      <div className="forex-bottom">
        <div className="card converter-card">
          <h3>Currency Converter</h3>
          <div className="form-group">
            <label>Amount</label>
            <input className="form-input" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
          </div>
          <div className="converter-row">
            <select className="filter-select flex-1" value={currency} onChange={(e) => { setCurrency(e.target.value); setChartCurrency(e.target.value); }}>
              {rates.map((r) => <option key={r.code} value={r.code}>{r.code}</option>)}
            </select>
          </div>
          {amount && (
            <div className="converted-result">
              <span className="conv-label">= PKR</span>
              <span className="conv-value">{converted}</span>
            </div>
          )}
        </div>

        <div className="card trend-card">
          <div className="card-header">
            <h3>{chartCurrency}/PKR — 7 Day Trend</h3>
            <select className="filter-select" value={chartCurrency} onChange={(e) => setChartCurrency(e.target.value)}>
              {rates.map((r) => <option key={r.code} value={r.code}>{r.code}</option>)}
            </select>
          </div>
          <svg viewBox={`0 0 ${w} ${h}`} className="trend-svg">
            <polyline fill="none" stroke="#1a7a5e" strokeWidth="2" points={points} />
          </svg>
          <div className="trend-labels">
            <span>7 days ago</span><span>Today (live API)</span>
          </div>
        </div>
      </div>
    </div>
  );
}
