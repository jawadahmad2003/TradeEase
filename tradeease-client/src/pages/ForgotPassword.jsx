import { useState } from "react";
import { api } from "../api/client";

export default function ForgotPassword({ onBack, onSent }) {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);

    const [error, setError] = useState(null);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!email.trim()) return;
    setError(null);
    try {
      await api.forgotPassword(email);
      setSent(true);
      onSent?.(email);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-panel auth-login-panel">
        <div className="auth-brand">
          <div className="auth-logo">TE</div>
          <strong>TradeEase</strong>
        </div>

        {!sent ? (
          <>
            <h1 className="auth-title">Reset your password</h1>
            <p className="auth-subtitle">Enter your email and we'll send you a link to reset your password.</p>
            {error && <div className="auth-error">{error}</div>}
            <form className="auth-form" onSubmit={handleSubmit}>
              <label className="auth-field">
                <span>Email address</span>
                <input
                  className="auth-plain-input"
                  type="email"
                  placeholder="ahmed@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </label>
              <button type="submit" className="auth-submit">Send Reset Link</button>
            </form>
          </>
        ) : (
          <div className="auth-success-box">
            <div className="auth-success-icon">✓</div>
            <h2>Check your email</h2>
            <p>
              If <strong>{email}</strong> is registered, a password reset link has been sent.
              Check your inbox and spam folder.
            </p>
            <button type="button" className="auth-submit" onClick={onBack}>Back to Sign In</button>
          </div>
        )}

        {!sent && (
          <p className="auth-switch">
            Remember your password? <button type="button" onClick={onBack}>Sign in</button>
          </p>
        )}
      </section>
    </main>
  );
}
