import { useEffect, useRef, useState } from "react";
import Sidebar from "./components/Sidebar";
import Topbar from "./components/Topbar";
import Dashboard from "./pages/Dashboard";
import TradeParties from "./pages/TradeParties";
import Shipments from "./pages/Shipments";
import ShipmentDetail from "./pages/ShipmentDetail";
import Documents from "./pages/Documents";
import HSCodes from "./pages/HSCodes";
import ForexRates from "./pages/ForexRates";
import ComplianceWizard from "./pages/ComplianceWizard";
import NewShipment from "./pages/NewShipment";
import Profile from "./pages/Profile";
import Login from "./pages/Login";
import Register from "./pages/Register";
import ForgotPassword from "./pages/ForgotPassword";
import ResetPassword from "./pages/ResetPassword";
import SchemeChecker from "./pages/SchemeChecker";
import UserManagement from "./pages/UserManagement";
import AdminPortal from "./pages/AdminPortal";
import AdminShipments from "./pages/AdminShipments";
import AdminHSCodes from "./pages/AdminHSCodes";
import AdminSchemes from "./pages/AdminSchemes";
import AdminSystem from "./pages/AdminSystem";
import AdminReports from "./pages/AdminReports";
import { useToast } from "./components/Toast";
import { api, clearAuth, getToken, setAuth } from "./api/client";

const SESSION_MS = 60 * 60 * 1000;
const ADMIN_PAGES = new Set([
  "admin-portal", "user-management", "admin-shipments",
  "admin-hs-codes", "admin-schemes", "admin-system", "admin-reports",
]);
const TRADER_PAGES = new Set([
  "dashboard", "trade-parties", "shipments", "documents",
  "hs-codes", "forex-rates", "compliance-wizard", "scheme-checker", "new-shipment",
]);

export default function App() {
  const showToast = useToast();
  const [sessionReady, setSessionReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [authPage, setAuthPage] = useState("login");
  const [activePage, setActivePage] = useState("dashboard");
  const [selectedShipmentId, setSelectedShipmentId] = useState(null);
  const [userRole, setUserRole] = useState("Trader");
  const [userName, setUserName] = useState("User");
  const [isVerified, setIsVerified] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [openAddParty, setOpenAddParty] = useState(false);
  const [openUploadDoc, setOpenUploadDoc] = useState(false);
  const [openGenerateDoc, setOpenGenerateDoc] = useState(false);
  const [compliancePending, setCompliancePending] = useState(0);
  const lastActivity = useRef(Date.now());

  const [resetToken, setResetToken] = useState(null);

  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get("token");
    if (token) {
      setResetToken(token);
      setAuthPage("reset-password");
      // Clean up the URL to not leak the token if they navigate away
      window.history.replaceState({}, document.title, window.location.pathname);
    }

    const savedToken = getToken();
    if (!savedToken) {
      setSessionReady(true);
      return undefined;
    }

    api.me()
      .then((profile) => {
        setIsAuthenticated(true);
        setUserRole(profile.role);
        setUserName(profile.fullName);
        setIsVerified(profile.isVerified);
        setActivePage(profile.role === "Admin" ? "admin-portal" : "dashboard");
      })
      .catch(() => {
        clearAuth();
        setIsAuthenticated(false);
      })
      .finally(() => setSessionReady(true));

    return undefined;
  }, []);

  const handleLogout = () => {
    clearAuth();
    setIsAuthenticated(false);
    setAuthPage("login");
    setUserRole("Trader");
    setUserName("User");
    setSidebarOpen(false);
  };

  useEffect(() => {
    if (!isAuthenticated || userRole === "Admin") return undefined;
    api.compliance()
      .then((steps) => {
        const pending = Array.isArray(steps) ? steps.filter((s) => !s.isCompleted).length : 0;
        setCompliancePending(pending);
      })
      .catch(() => setCompliancePending(0));
    return undefined;
  }, [isAuthenticated, userRole, activePage]);

  useEffect(() => {
    if (!isAuthenticated) return undefined;
    const touch = () => { lastActivity.current = Date.now(); };
    const events = ["mousedown", "keydown", "scroll", "touchstart"];
    events.forEach((e) => window.addEventListener(e, touch));
    const timer = window.setInterval(() => {
      if (Date.now() - lastActivity.current >= SESSION_MS) {
        showToast("Session expired after 60 minutes of inactivity.", "info");
        handleLogout();
      }
    }, 30000);
    return () => {
      events.forEach((e) => window.removeEventListener(e, touch));
      window.clearInterval(timer);
    };
  }, [isAuthenticated]);

  const navigate = (page, shipmentId = null) => {
    if (ADMIN_PAGES.has(page) && userRole !== "Admin") {
      showToast("Admin access only.", "error");
      return;
    }
    if (userRole === "Admin" && TRADER_PAGES.has(page)) {
      showToast("Admin account is for platform management only.", "info");
      setActivePage("admin-portal");
      return;
    }
    setActivePage(page);
    if (shipmentId != null) setSelectedShipmentId(shipmentId);
  };

  const handleSignIn = (auth) => {
    setAuth(auth.token, {
      token: auth.token,
      userId: auth.userId,
      fullName: auth.fullName,
      email: auth.email,
      role: auth.role,
      isVerified: auth.isVerified,
    });
    setIsAuthenticated(true);
    setUserRole(auth.role);
    setUserName(auth.fullName);
    setIsVerified(auth.isVerified);
    setActivePage(auth.role === "Admin" ? "admin-portal" : "dashboard");
    setSidebarOpen(true);
  };

  const pages = {
    dashboard: <Dashboard setActivePage={navigate} />,
    "admin-portal": <AdminPortal setActivePage={navigate} />,
    "admin-shipments": <AdminShipments setActivePage={navigate} />,
    "admin-hs-codes": <AdminHSCodes setActivePage={navigate} />,
    "admin-schemes": <AdminSchemes setActivePage={navigate} />,
    "admin-system": <AdminSystem setActivePage={navigate} />,
    "admin-reports": <AdminReports setActivePage={navigate} />,
    "trade-parties": <TradeParties openAddOnMount={openAddParty} onAddModalClose={() => setOpenAddParty(false)} />,
    shipments: <Shipments setActivePage={navigate} />,
    "shipment-detail": (
      <ShipmentDetail
        shipmentId={selectedShipmentId}
        setActivePage={navigate}
        onGenerateDoc={() => { setOpenGenerateDoc(true); setActivePage("documents"); }}
        backPage={userRole === "Admin" ? "admin-shipments" : "shipments"}
        readOnly={userRole === "Admin"}
      />
    ),
    documents: (
      <Documents
        openUploadOnMount={openUploadDoc}
        openGenerateOnMount={openGenerateDoc}
        onUploadModalClose={() => setOpenUploadDoc(false)}
        onGenerateModalClose={() => setOpenGenerateDoc(false)}
      />
    ),
    "hs-codes": <HSCodes setActivePage={navigate} />,
    "forex-rates": <ForexRates />,
    "compliance-wizard": <ComplianceWizard setActivePage={navigate} onUploadDoc={() => { setOpenUploadDoc(true); setActivePage("documents"); }} />,
    "scheme-checker": <SchemeChecker />,
    "user-management": <UserManagement setActivePage={navigate} />,
    "new-shipment": <NewShipment setActivePage={navigate} onAddParty={() => { setOpenAddParty(true); setActivePage("trade-parties"); }} />,
    profile: <Profile userRole={userRole} isVerified={isVerified} setIsVerified={setIsVerified} userName={userName} />,
  };

  const pageTitles = {
    dashboard: { title: "Dashboard", breadcrumb: ["Home", "Dashboard"] },
    "admin-portal": { title: "Admin Portal", breadcrumb: ["Home", "Admin", "Control Portal"] },
    "admin-shipments": { title: "All Shipments", breadcrumb: ["Home", "Admin", "All Shipments"] },
    "admin-hs-codes": { title: "HS Tariff Admin", breadcrumb: ["Home", "Admin", "HS Tariff Database"] },
    "admin-schemes": { title: "Scheme Rules", breadcrumb: ["Home", "Admin", "Export Scheme Rules"] },
    "admin-system": { title: "System Settings", breadcrumb: ["Home", "Admin", "System Settings"] },
    "admin-reports": { title: "Reports", breadcrumb: ["Home", "Admin", "Reports & Analytics"] },
    "trade-parties": { title: "Trade Parties", breadcrumb: ["Home", "Trade Parties"] },
    shipments: { title: "Shipments", breadcrumb: ["Home", "Shipments"] },
    "shipment-detail": { title: "Shipment Detail", breadcrumb: ["Home", "Shipments", String(selectedShipmentId || "Detail")] },
    documents: { title: "Documents", breadcrumb: ["Home", "Documents"] },
    "hs-codes": { title: "FBR HS Tariff & Import Duty", breadcrumb: ["Home", "FBR PCT & Duty"] },
    "forex-rates": { title: "Forex Rates (PKR)", breadcrumb: ["Home", "Forex"] },
    "compliance-wizard": { title: "FBR / WeBOC Compliance", breadcrumb: ["Home", "Compliance", "Registration Wizard"] },
    "scheme-checker": { title: "Import & Export Scheme Checker", breadcrumb: ["Home", "Compliance", "Schemes"] },
    "user-management": { title: "User Management", breadcrumb: ["Home", "Admin", "User Management"] },
    "new-shipment": { title: "New Import / Export Shipment", breadcrumb: ["Home", "Shipments", "New"] },
    profile: { title: "My Profile", breadcrumb: ["Home", "Account", "Profile"] },
  };

  if (!sessionReady) {
    return (
      <main className="auth-page">
        <p className="auth-subtitle">Loading TradeEase...</p>
      </main>
    );
  }

  if (!isAuthenticated) {
    if (authPage === "register") {
      return <Register onSignIn={handleSignIn} onGoLogin={() => setAuthPage("login")} />;
    }
    if (authPage === "forgot-password") {
      return <ForgotPassword onBack={() => setAuthPage("login")} onSent={() => {}} />;
    }
    if (authPage === "reset-password") {
      return <ResetPassword token={resetToken} onBack={() => setAuthPage("login")} onSuccess={() => setAuthPage("login")} />;
    }
    return (
      <Login
        onSignIn={handleSignIn}
        onRegister={() => setAuthPage("register")}
        onForgotPassword={() => setAuthPage("forgot-password")}
      />
    );
  }

  return (
    <div className="app-shell">
      <Sidebar
        activePage={activePage}
        setActivePage={navigate}
        sidebarOpen={sidebarOpen}
        onLogout={handleLogout}
        userRole={userRole}
        userName={userName}
        isVerified={isVerified}
        compliancePending={compliancePending}
      />
      <div className={`main-content ${sidebarOpen ? "sidebar-open" : "sidebar-closed"}`}>
        <Topbar
          pageInfo={pageTitles[activePage]}
          activePage={activePage}
          userRole={userRole}
          userName={userName}
          setSidebarOpen={setSidebarOpen}
          sidebarOpen={sidebarOpen}
          setActivePage={navigate}
          onAddParty={() => { setOpenAddParty(true); setActivePage("trade-parties"); }}
          onUploadDoc={() => { setOpenUploadDoc(true); setActivePage("documents"); }}
        />
        <div className="page-body">{pages[activePage]}</div>
      </div>
    </div>
  );
}
