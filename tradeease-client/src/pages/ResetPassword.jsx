import { useState, useEffect } from "react";
import { api } from "../api/client";

export default function ResetPassword({ token, onBack, onSuccess }) {
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (!token) {
      setError("Invalid or missing password reset token.");
    }
  }, [token]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    if (password.length < 6) {
      setError("Password must be at least 6 characters.");
      return;
    }
    
    setError(null);
    try {
      await api.resetPassword(token, password);
      setSuccess(true);
      setTimeout(() => {
        onSuccess?.();
      }, 2000);
    } catch (err) {
      setError(err.message);
    }
  };

  if (!token) {
    return (
      <main className="auth-page">
        <section className="auth-panel auth-login-panel">
          <div className="auth-brand">
            <div className="auth-logo">TE</div>
            <strong>TradeEase</strong>
          </div>
          <h1 className="auth-title">Invalid Link</h1>
          <p className="auth-subtitle">The password reset link is invalid or expired.</p>
          <button type="button" className="auth-submit" onClick={onBack}>Back to Sign In</button>
        </section>
      </main>
    );
  }

  return (
    <main className="auth-page">
      <section className="auth-panel auth-login-panel">
        <div className="auth-brand">
          <div className="auth-logo">TE</div>
          <strong>TradeEase</strong>
        </div>

        {!success ? (
          <>
            <h1 className="auth-title">Create new password</h1>
            <p className="auth-subtitle">Please enter your new password below.</p>
            {error && <div className="auth-error">{error}</div>}
            <form className="auth-form" onSubmit={handleSubmit}>
              <label className="auth-field">
                <span>New Password</span>
                <input
                  className="auth-plain-input"
                  type="password"
                  placeholder="At least 6 characters"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </label>
              <label className="auth-field">
                <span>Confirm Password</span>
                <input
                  className="auth-plain-input"
                  type="password"
                  placeholder="Repeat new password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                />
              </label>
              <button type="submit" className="auth-submit">Reset Password</button>
            </form>
          </>
        ) : (
          <div className="auth-success-box">
            <div className="auth-success-icon">✓</div>
            <h2>Password Reset!</h2>
            <p>Your password has been successfully reset. You will be redirected to sign in...</p>
            <button type="button" className="auth-submit" onClick={onSuccess}>Sign In Now</button>
          </div>
        )}
      </section>
    </main>
  );
}
