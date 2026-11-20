import { useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function SchemeChecker() {
  const showToast = useToast();
  const [answers, setAnswers] = useState({
    traderType: "",
    exportPct: "",
    turnover: "",
    category: "",
    fbrStatus: "",
  });
  const [checked, setChecked] = useState(false);
  const [eligible, setEligible] = useState([]);
  const [savings, setSavings] = useState(0);
  const [allSchemes, setAllSchemes] = useState([]);
  const [checking, setChecking] = useState(false);

  const set = (key, val) => setAnswers((a) => ({ ...a, [key]: val }));

  const runCheck = async () => {
    if (!answers.traderType || !answers.exportPct || !answers.turnover || !answers.category || !answers.fbrStatus) {
      showToast("Please answer all 5 questions.", "error");
      return;
    }
    setChecking(true);
    try {
      const [checkResult, schemes] = await Promise.all([
        api.checkSchemes({
          traderType: answers.traderType,
          exportPct: parseInt(answers.exportPct, 10),
          turnoverUsd: parseInt(answers.turnover, 10),
          category: answers.category,
          fbrStatus: answers.fbrStatus,
        }),
        api.schemes(),
      ]);
      setEligible(checkResult.eligibleSchemes || []);
      setSavings(Number(checkResult.estimatedSavingsPkr) || 0);
      setAllSchemes(schemes.filter((s) => s.isActive));
      setChecked(true);
      showToast("Eligibility check complete.");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setChecking(false);
    }
  };

  return (
    <div className="scheme-page">
      <aside className="card scheme-input-card">
        <h2>Eligibility Questions</h2>
        <p className="scheme-intro">Answer 5 questions to check EFS, DDT, SBP-EFS, EMDF, and LTFF eligibility.</p>

        <label className="form-group">
          <span className="form-label">1. Trader Type</span>
          <select className="form-select" value={answers.traderType} onChange={(e) => set("traderType", e.target.value)}>
            <option value="">Select...</option>
            <option>Manufacturer Exporter</option>
            <option>Commercial Exporter</option>
            <option>SME Exporter</option>
            <option>First-time Importer Only</option>
          </select>
        </label>

        <label className="form-group">
          <span className="form-label">2. Export Percentage of Turnover</span>
          <select className="form-select" value={answers.exportPct} onChange={(e) => set("exportPct", e.target.value)}>
            <option value="">Select...</option>
            <option value="10">10% or more</option>
            <option value="20">20% or more</option>
            <option value="30">30% or more</option>
            <option value="50">50% or more</option>
          </select>
        </label>

        <label className="form-group">
          <span className="form-label">3. Annual Turnover (USD)</span>
          <select className="form-select" value={answers.turnover} onChange={(e) => set("turnover", e.target.value)}>
            <option value="">Select...</option>
            <option value="25000">Under USD 50,000</option>
            <option value="75000">USD 50,000 – 100,000</option>
            <option value="150000">USD 100,000 – 500,000</option>
            <option value="600000">Above USD 500,000</option>
          </select>
        </label>

        <label className="form-group">
          <span className="form-label">4. Product Category</span>
          <select className="form-select" value={answers.category} onChange={(e) => set("category", e.target.value)}>
            <option value="">Select...</option>
            <option>Textiles & Garments</option>
            <option>Surgical & Medical</option>
            <option>Food & Agriculture</option>
            <option>Agriculture Only</option>
            <option>Leather & Sports Goods</option>
          </select>
        </label>

        <label className="form-group">
          <span className="form-label">5. FBR Registration Status</span>
          <select className="form-select" value={answers.fbrStatus} onChange={(e) => set("fbrStatus", e.target.value)}>
            <option value="">Select...</option>
            <option>Active Filer</option>
            <option>Non-Filer</option>
            <option>Not Registered</option>
          </select>
        </label>

        <button type="button" className="btn-primary full-width scheme-check-btn" onClick={runCheck} disabled={checking}>
          {checking ? "Checking..." : "Check Eligibility"}
        </button>
      </aside>

      <section className="scheme-results">
        {!checked ? (
          <div className="card empty-scheme-card">
            <p>Complete the 5 questions and click <strong>Check Eligibility</strong> to see matching government export schemes.</p>
          </div>
        ) : eligible.length === 0 ? (
          <div className="card empty-scheme-card">
            <p>No schemes matched your current profile. Complete FBR registration and increase export share to qualify.</p>
          </div>
        ) : (
          <>
            <div className="card savings-card">
              <h3>Estimated Annual Savings</h3>
              <p className="savings-value">PKR {savings.toLocaleString("en-PK")}</p>
              <p className="muted-text">Based on {eligible.length} eligible scheme(s) at 8% avg. benefit rate.</p>
            </div>
            {eligible.map((scheme) => (
              <article key={scheme.schemeCode} className="scheme-card expanded eligible">
                <div className="scheme-card-head">
                  <div className="scheme-code">{scheme.schemeCode}</div>
                  <div className="scheme-title-block">
                    <h3>{scheme.schemeName}</h3>
                    <p>{scheme.agency}</p>
                  </div>
                  <span className="scheme-status eligible-tag">Eligible</span>
                </div>
                <div className="scheme-card-body">
                  <p><strong>Benefit:</strong> {scheme.benefitDetails}</p>
                  <p><strong>How to apply:</strong> Submit application through {scheme.agency} portal with NTN and export history.</p>
                  <p><strong>Deadline:</strong> {scheme.applicationDeadline}</p>
                </div>
              </article>
            ))}
          </>
        )}

        {checked && allSchemes.length > 0 && (
          <div className="card">
            <h3>All Schemes Reference</h3>
            <div className="scheme-ref-grid">
              {allSchemes.map((s) => (
                <div key={s.schemeCode} className={`scheme-ref-item ${eligible.some((e) => e.schemeCode === s.schemeCode) ? "matched" : ""}`}>
                  <strong>{s.schemeCode}</strong> — {s.schemeName}
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
