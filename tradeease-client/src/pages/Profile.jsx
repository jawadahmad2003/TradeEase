import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

function splitName(fullName) {
  const parts = (fullName || "").trim().split(/\s+/);
  if (parts.length <= 1) return { firstName: parts[0] || "", lastName: "" };
  return { firstName: parts[0], lastName: parts.slice(1).join(" ") };
}

function initials(first, last) {
  return `${(first[0] || "").toUpperCase()}${(last[0] || first[1] || "").toUpperCase()}`.slice(0, 2);
}

export default function Profile({ userRole = "Trader", isVerified = false, setIsVerified }) {
  const showToast = useToast();
  const tabs = ["Personal Info", "Security", "Notifications", "Shipping Preferences", "Activity Log"];
  const [activeTab, setActiveTab] = useState("Personal Info");
  const [loading, setLoading] = useState(true);
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "", phone: "", company: "", city: "", ntn: "", cnic: "" });
  const [savedForm, setSavedForm] = useState(form);
  const [notifPrefs, setNotifPrefs] = useState({ shipments: true, forex: true, compliance: true });
  const [twoFaEnabled, setTwoFaEnabled] = useState(false);
  const [pwdForm, setPwdForm] = useState({ current: "", next: "", confirm: "" });
  const [pwdSaving, setPwdSaving] = useState(false);
  const [twoFaSetup, setTwoFaSetup] = useState(null);
  const [twoFaCode, setTwoFaCode] = useState("");
  const [twoFaBusy, setTwoFaBusy] = useState(false);
  const [disableForm, setDisableForm] = useState({ code: "", password: "" });
  const [shippingPrefs, setShippingPrefs] = useState({ shipmentType: "Import", currency: "USD" });

  useEffect(() => {
    api.me()
      .then((data) => {
        setProfile(data);
        const { firstName, lastName } = splitName(data.fullName);
        const next = {
          firstName,
          lastName,
          email: data.email,
          phone: data.phone || "",
          company: data.businessName || "",
          city: data.city || "",
          ntn: data.ntn || "",
          cnic: data.cnic || "",
        };
        setForm(next);
        setSavedForm(next);
        setNotifPrefs({
          shipments: data.notifyShipments ?? true,
          forex: data.notifyForex ?? true,
          compliance: data.notifyCompliance ?? true,
        });
        setTwoFaEnabled(!!data.twoFactorEnabled);
        if (data.isVerified) setIsVerified?.(true);
      })
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));

    const prefs = localStorage.getItem("tradeease_shipping_prefs");
    if (prefs) {
      try {
        setShippingPrefs(JSON.parse(prefs));
      } catch(e) {}
    }
  }, []);

  const set = (key, val) => setForm((f) => ({ ...f, [key]: val }));

  const saveChanges = async () => {
    try {
      const updated = await api.updateProfile({
        fullName: `${form.firstName} ${form.lastName}`.trim(),
        phone: form.phone || null,
        businessName: form.company || null,
        city: form.city || null,
        ntn: form.ntn || null,
        cnic: form.cnic || null,
        notifyShipments: notifPrefs.shipments,
        notifyForex: notifPrefs.forex,
        notifyCompliance: notifPrefs.compliance,
      });
      setProfile(updated);
      setSavedForm(form);
      if (updated.isVerified) setIsVerified?.(true);
      showToast("Profile saved successfully.");
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const cancelChanges = () => {
    setForm(savedForm);
    showToast("Changes discarded.", "info");
  };

  const updatePassword = async () => {
    if (!pwdForm.current || !pwdForm.next) {
      showToast("Enter current and new password.", "error");
      return;
    }
    if (pwdForm.next.length < 6) {
      showToast("New password must be at least 6 characters.", "error");
      return;
    }
    if (pwdForm.next !== pwdForm.confirm) {
      showToast("New passwords do not match.", "error");
      return;
    }
    setPwdSaving(true);
    try {
      await api.changePassword(pwdForm.current, pwdForm.next);
      setPwdForm({ current: "", next: "", confirm: "" });
      showToast("Password updated successfully.");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setPwdSaving(false);
    }
  };

  const start2faSetup = async () => {
    setTwoFaBusy(true);
    try {
      const setup = await api.setup2fa();
      setTwoFaSetup(setup);
      setTwoFaCode("");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setTwoFaBusy(false);
    }
  };

  const confirm2faEnable = async () => {
    if (!twoFaSetup || twoFaCode.length !== 6) {
      showToast("Enter the 6-digit code from your authenticator app.", "error");
      return;
    }
    setTwoFaBusy(true);
    try {
      const updated = await api.enable2fa(twoFaSetup.secret, twoFaCode);
      setTwoFaEnabled(updated.twoFactorEnabled);
      setTwoFaSetup(null);
      setTwoFaCode("");
      setProfile(updated);
      showToast("Two-factor authentication enabled.");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setTwoFaBusy(false);
    }
  };

  const confirm2faDisable = async () => {
    if (disableForm.code.length !== 6 || !disableForm.password) {
      showToast("Enter your authenticator code and password.", "error");
      return;
    }
    setTwoFaBusy(true);
    try {
      const updated = await api.disable2fa(disableForm.code, disableForm.password);
      setTwoFaEnabled(updated.twoFactorEnabled);
      setDisableForm({ code: "", password: "" });
      setProfile(updated);
      showToast("Two-factor authentication disabled.");
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setTwoFaBusy(false);
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading profile...</p></div>;

  const memberSince = profile?.createdAt
    ? new Date(profile.createdAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" })
    : "—";
  const lastLogin = profile?.lastLogin
    ? new Date(profile.lastLogin).toLocaleString("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" })
    : "—";

  return (
    <div className="profile-page profile-page-full">
      <section className="profile-summary-card">
        <div className="profile-avatar-xl">{initials(savedForm.firstName, savedForm.lastName)}</div>
        <div className="profile-summary-main">
          <h2>
            {savedForm.firstName} {savedForm.lastName}{" "}
            {(isVerified || profile?.isVerified) && <span className="verified-badge">Verified</span>}
          </h2>
          <p>{savedForm.email}</p>
          <div className="profile-meta-row">
            <span className="profile-role-pill">{userRole}</span>
            <span>{savedForm.company} · {savedForm.city.split(",")[0] || "Pakistan"}</span>
          </div>
        </div>
        <div className="member-since">
          <span>Member since</span>
          <strong>{memberSince}</strong>
        </div>
      </section>

      <nav className="profile-tabs" aria-label="Profile sections">
        {tabs.map((tab) => (
          <button key={tab} type="button" className={activeTab === tab ? "active" : ""} onClick={() => setActiveTab(tab)}>
            {tab}
          </button>
        ))}
      </nav>

      {activeTab === "Personal Info" && (
        <div className="profile-content-grid">
          <section className="card profile-form-card">
            <h3>Personal Information</h3>
            <div className="profile-form-grid">
              <ProfileField label="FIRST NAME" value={form.firstName} onChange={(v) => set("firstName", v)} />
              <ProfileField label="LAST NAME" value={form.lastName} onChange={(v) => set("lastName", v)} />
              <ProfileField label="EMAIL ADDRESS" value={form.email} onChange={() => {}} readOnly />
              <ProfileField label="PHONE NUMBER" value={form.phone} onChange={(v) => set("phone", v)} />
              <ProfileField label="COMPANY / FIRM" value={form.company} onChange={(v) => set("company", v)} />
              <ProfileField label="CITY" value={form.city} onChange={(v) => set("city", v)} />
              <ProfileField label="NTN NUMBER" value={form.ntn} onChange={(v) => set("ntn", v)} />
              <ProfileField label="CNIC" value={form.cnic} onChange={(v) => set("cnic", v.replace(/\D/g, ""))} wide />
            </div>
            <div className="profile-actions">
              <button type="button" className="btn-primary" onClick={saveChanges}>Save Changes</button>
              <button type="button" className="btn-ghost profile-cancel-btn" onClick={cancelChanges}>Cancel</button>
            </div>
          </section>

          <aside className="profile-side-col">
            <section className="card account-details-card">
              <h3>Account Details</h3>
              <DetailRow label="Role" value={userRole} />
              <DetailRow label="Verification" value={(isVerified || profile?.isVerified) ? "Verified profile" : "Pending approval"} />
              <DetailRow label="User ID" value={`USR-${profile?.userId}`} />
              <DetailRow label="Last Login" value={lastLogin} />
            </section>


          </aside>
        </div>
      )}

      {activeTab === "Security" && (
        <div className="profile-content-grid">
          <section className="card profile-form-card">
            <h3>Change Password</h3>
            <label className="form-group">
              <span className="form-label">Current Password</span>
              <input
                className="form-input"
                type="password"
                value={pwdForm.current}
                onChange={(e) => setPwdForm((p) => ({ ...p, current: e.target.value }))}
              />
            </label>
            <label className="form-group">
              <span className="form-label">New Password</span>
              <input
                className="form-input"
                type="password"
                value={pwdForm.next}
                onChange={(e) => setPwdForm((p) => ({ ...p, next: e.target.value }))}
              />
            </label>
            <label className="form-group">
              <span className="form-label">Confirm New Password</span>
              <input
                className="form-input"
                type="password"
                value={pwdForm.confirm}
                onChange={(e) => setPwdForm((p) => ({ ...p, confirm: e.target.value }))}
              />
            </label>
            <button type="button" className="btn-primary" disabled={pwdSaving} onClick={updatePassword}>
              {pwdSaving ? "Updating..." : "Update Password"}
            </button>
          </section>


        </div>
      )}

      {activeTab === "Notifications" && (
        <section className="card profile-form-card">
          <h3>Notification Preferences</h3>
          {[
            { key: "shipments", label: "Shipment status updates" },
            { key: "forex", label: "Live forex rate alerts (every minute when enabled)" },
            { key: "compliance", label: "Compliance deadline reminders" },
          ].map(({ key, label }) => (
            <label key={key} className="auth-check profile-notif-row">
              <input
                type="checkbox"
                checked={notifPrefs[key]}
                onChange={(e) => setNotifPrefs((p) => ({ ...p, [key]: e.target.checked }))}
              />
              <span>{label}</span>
            </label>
          ))}
          <button type="button" className="btn-primary mt-8" onClick={saveChanges}>Save Preferences</button>
        </section>
      )}

      {activeTab === "Shipping Preferences" && (
        <section className="card profile-form-card">
          <h3>Shipping Defaults</h3>
          <p className="muted-text mb-4" style={{marginBottom: '1rem'}}>Set your default preferences to auto-fill the New Shipment form.</p>
          <label className="form-group">
            <span className="form-label">Default Shipment Type</span>
            <select className="form-select" value={shippingPrefs.shipmentType} onChange={(e) => setShippingPrefs(p => ({ ...p, shipmentType: e.target.value }))}>
              <option>Import</option>
              <option>Export</option>
            </select>
          </label>
          <label className="form-group" style={{marginTop: '1rem'}}>
            <span className="form-label">Default Currency</span>
            <select className="form-select" value={shippingPrefs.currency} onChange={(e) => setShippingPrefs(p => ({ ...p, currency: e.target.value }))}>
              <option>USD</option>
              <option>PKR</option>
              <option>AED</option>
              <option>CNY</option>
              <option>SAR</option>
              <option>EUR</option>
              <option>GBP</option>
            </select>
          </label>
          <button type="button" className="btn-primary mt-8" onClick={() => {
            localStorage.setItem("tradeease_shipping_prefs", JSON.stringify(shippingPrefs));
            showToast("Shipping preferences saved to your browser.");
          }}>Save Preferences</button>
        </section>
      )}

      {activeTab === "Activity Log" && (
        <section className="card profile-form-card">
          <h3>Recent Activity</h3>
          <div className="activity-log">
            <div className="activity-row"><span>{lastLogin}</span><strong>Signed in</strong></div>
            <div className="activity-row"><span>{memberSince}</span><strong>Account created</strong></div>
          </div>
        </section>
      )}
    </div>
  );
}

function ProfileField({ label, value, onChange, wide, readOnly }) {
  return (
    <label className={`profile-field ${wide ? "wide" : ""}`}>
      <span>{label}</span>
      <div className="profile-input-wrap">
        <input value={value} onChange={(e) => onChange(e.target.value)} readOnly={readOnly} />
      </div>
    </label>
  );
}

function DetailRow({ label, value }) {
  return (
    <div className="account-detail-row">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
