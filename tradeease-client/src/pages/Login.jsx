import { useEffect, useState } from "react";
import { api, clearAuth } from "../api/client";

import TradeLogo from "../components/TradeLogo";

export default function Login({ onSignIn, onRegister, onForgotPassword }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [dbStatus, setDbStatus] = useState("checking");

  useEffect(() => {
    clearAuth();
    api.health()
      .then((h) => setDbStatus(h.database?.connected ? "connected" : "offline"))
      .catch(() => setDbStatus("offline"));
  }, []);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!email.trim() || !password.trim()) {
      setError("Please enter email and password.");
      return;
    }
    setError("");
    setLoading(true);
    try {
      const auth = await api.login(email.trim(), password);
      onSignIn(auth);
    } catch (err) {
      setError(err.message || "Sign in failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-panel auth-login-panel">
        <TradeLogo centered />
        <p className="auth-subtitle">
          Pakistan import & export management
        </p>
        <p className="auth-hint">
          {dbStatus === "connected" && "Database connected · TradeEaseDB on SQL Server"}
          {dbStatus === "offline" && "Database offline — run database\\setup-database.ps1 first"}
          {dbStatus === "checking" && "Checking database connection..."}
        </p>

        <form className="auth-form" onSubmit={handleSubmit}>
          {error && <div className="auth-error">{error}</div>}

          <label className="auth-field">
            <span>Email address</span>
            <div className="auth-input-wrap">
              <MailIcon />
              <input
                type="email"
                placeholder="admin@tradeease.pk"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
          </label>

          <label className="auth-field">
            <span>Password</span>
            <div className="auth-input-wrap">
              <LockIcon />
              <input
                type={showPassword ? "text" : "password"}
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <button
                type="button"
                className="auth-input-right auth-eye-btn"
                onClick={() => setShowPassword((v) => !v)}
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                {showPassword ? "Hide" : "Show"}
              </button>
            </div>
          </label>

          <div className="auth-options">
            <label className="auth-check">
              <input type="checkbox" />
              <span>Remember me</span>
            </label>
            <button type="button" className="text-action" onClick={onForgotPassword}>
              Forgot password?
            </button>
          </div>

          <button type="submit" className="auth-submit" disabled={loading}>
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <div className="auth-divider"><span>OR</span></div>
        <p className="auth-switch">
          Don&apos;t have an account? <button type="button" onClick={onRegister}>Register</button>
        </p>
      </section>
    </main>
  );
}

function MailIcon() {
  return <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/></svg>;
}

function LockIcon() {
  return <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></svg>;
}
