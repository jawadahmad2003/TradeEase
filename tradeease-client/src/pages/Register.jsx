import { useState } from "react";
import { api } from "../api/client";
import TradeLogo from "../components/TradeLogo";

function formatCnicInput(value) {
  const digits = value.replace(/\D/g, "").slice(0, 13);
  if (digits.length <= 5) return digits;
  if (digits.length <= 12) return `${digits.slice(0, 5)}-${digits.slice(5)}`;
  return `${digits.slice(0, 5)}-${digits.slice(5, 12)}-${digits.slice(12)}`;
}

function cnicDigits(value) {
  return value.replace(/\D/g, "");
}

export default function Register({ onSignIn, onGoLogin }) {
  const [showTerms, setShowTerms] = useState(false);
  const [form, setForm] = useState({
    name: "",
    email: "",
    business: "",
    cnic: "",
    ntn: "",
    password: "",
    confirm: "",
    terms: false,
  });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [verifyMsg, setVerifyMsg] = useState("");
  const [verifyOk, setVerifyOk] = useState(false);
  const [verifying, setVerifying] = useState(false);

  const set = (key, val) => setForm((f) => ({ ...f, [key]: val }));

  const runVerification = async () => {
    if (!form.name || !form.business || !form.cnic || !form.ntn) {
      setVerifyMsg("Enter full name, business name, CNIC, and NTN first.");
      setVerifyOk(false);
      return;
    }
    setVerifying(true);
    setVerifyMsg("");
    try {
      const result = await api.verifyTaxpayer({
        fullName: form.name.trim(),
        businessName: form.business.trim(),
        cnic: cnicDigits(form.cnic),
        ntn: form.ntn.trim(),
      });
      setVerifyOk(result.isVerified);
      setVerifyMsg(`${result.source}: ${result.message}`);
    } catch (err) {
      setVerifyOk(false);
      setVerifyMsg(err.message || "Verification failed.");
    } finally {
      setVerifying(false);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!form.name || !form.email || !form.business || !form.password || !form.cnic || !form.ntn) {
      setError("Please fill all required fields including CNIC and NTN.");
      return;
    }
    if (form.password !== form.confirm) {
      setError("Passwords do not match.");
      return;
    }
    if (!form.terms) {
      setError("Please accept the Terms of Service.");
      return;
    }
    if (!verifyOk) {
      setError("Click “Verify CNIC + NTN (FBR ATL)” first, or use demo credentials on the sign-in page.");
      return;
    }
    if (cnicDigits(form.cnic).length !== 13) {
      setError("CNIC must be 13 digits (FBR / NADRA format).");
      return;
    }
    setError("");
    setLoading(true);
    try {
      const auth = await api.register({
        fullName: form.name.trim(),
        email: form.email.trim(),
        businessName: form.business.trim(),
        cnic: cnicDigits(form.cnic),
        ntn: form.ntn.trim(),
        password: form.password,
      });
      if (auth.verificationMessage && !auth.isVerified) {
        setVerifyMsg(auth.verificationMessage);
      }
      onSignIn(auth);
    } catch (err) {
      setError(err.message || "Registration failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-panel auth-register-panel">
        <TradeLogo centered />
        <h1>Create your account</h1>
        <p className="auth-subtitle">FBR CNIC + NTN verification for Pakistani traders</p>

        <form className="auth-form register-form" onSubmit={handleSubmit}>
          {error && <div className="auth-error">{error}</div>}

          <label className="auth-field">
            <span>Full Name <b>*</b></span>
            <input className="auth-plain-input" placeholder="Ahmed Raza" value={form.name} onChange={(e) => set("name", e.target.value)} />
          </label>

          <label className="auth-field">
            <span>Email <b>*</b></span>
            <input className="auth-plain-input" type="email" placeholder="ahmed@example.com" value={form.email} onChange={(e) => set("email", e.target.value)} />
          </label>

          <label className="auth-field">
            <span>Business / Company Name <b>*</b></span>
            <input className="auth-plain-input" placeholder="Raza Exports Pvt Ltd" value={form.business} onChange={(e) => set("business", e.target.value)} />
          </label>

          <label className="auth-field">
            <span>CNIC <b>*</b></span>
            <input className="auth-plain-input" placeholder="35202-1234567-1" maxLength={15} value={form.cnic} onChange={(e) => set("cnic", formatCnicInput(e.target.value))} />
            <small>13 digits — FBR / NADRA format (SMS 9966 for official ATL proof)</small>
          </label>

          <label className="auth-field">
            <span>NTN <b>*</b></span>
            <input className="auth-plain-input" placeholder="1234567" maxLength={8} value={form.ntn} onChange={(e) => set("ntn", e.target.value.replace(/\D/g, ""))} />
            <small>7-8 digit National Tax Number (FBR)</small>
          </label>

          <label className="auth-field">
            <span>Password <b>*</b></span>
            <input className="auth-plain-input" type="password" placeholder="••••••••" value={form.password} onChange={(e) => set("password", e.target.value)} />
          </label>

          <label className="auth-field">
            <span>Confirm Password <b>*</b></span>
            <input className="auth-plain-input" type="password" placeholder="••••••••" value={form.confirm} onChange={(e) => set("confirm", e.target.value)} />
          </label>

          <div className="auth-field register-verify-block">
            <button type="button" className="btn-outline-sm" onClick={runVerification} disabled={verifying}>
              {verifying ? "Checking FBR..." : "Verify CNIC + NTN (FBR ATL)"}
            </button>
            {verifyMsg && (
              <p className={verifyOk ? "verify-ok" : "verify-warn"}>{verifyMsg}</p>
            )}
          </div>

          <label className="auth-check register-terms">
            <input type="checkbox" checked={form.terms} onChange={(e) => set("terms", e.target.checked)} />
            <span>
              I agree to the{" "}
              <button type="button" onClick={() => setShowTerms(true)}>Terms of Service</button>
            </span>
          </label>

          <button type="submit" className="auth-submit register-submit" disabled={loading}>
            {loading ? "Creating account..." : "Create Account"}
          </button>
        </form>

        <p className="auth-switch auth-register-switch">
          Already have an account? <button type="button" onClick={onGoLogin}>Sign in</button>
        </p>
      </section>

      {showTerms && (
        <div className="modal-overlay" onClick={() => setShowTerms(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Terms of Service</h3>
              <button type="button" className="icon-btn" onClick={() => setShowTerms(false)}>✕</button>
            </div>
            <div className="modal-body terms-body">
              <p>TradeEase verifies CNIC/NTN format and attempts FBR Active Taxpayer List lookup via public sources.</p>
              <p>Official confirmation: SMS ATL [space] CNIC or NTN to 9966, or iris.fbr.gov.pk.</p>
              <p>Duty calculations use FBR tariff tables and live forex — estimates only until customs filing on WeBOC.</p>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-primary" onClick={() => { set("terms", true); setShowTerms(false); }}>
                I Accept
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
