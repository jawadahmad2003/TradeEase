import { Icon } from "./Icons";
import TradeLogo from "./TradeLogo";

function initials(name) {
  return name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
}

export default function Sidebar({ activePage, setActivePage, sidebarOpen, onLogout, userRole, isVerified, userName = "User", compliancePending = 0 }) {
  const isItemActive = (itemId) => {
    if (itemId === "shipments" && activePage === "shipment-detail") return true;
    return activePage === itemId;
  };

  const adminSection = {
    section: "MANAGEMENT",
    items: [
      { id: "admin-portal", label: "Control Portal", icon: "admin" },
      { id: "user-management", label: "User Management", icon: "users" },
      { id: "admin-shipments", label: "All Shipments", icon: "package" },
      { id: "admin-hs-codes", label: "HS Tariff Admin", icon: "hash" },
      { id: "admin-schemes", label: "Scheme Rules", icon: "check-square" },
      { id: "admin-system", label: "System Settings", icon: "settings" },
      { id: "admin-reports", label: "Reports", icon: "chart" },
    ],
  };

  const traderSections = [
    { section: "MAIN", items: [
      { id: "dashboard", label: "Dashboard", icon: "grid" },
      { id: "trade-parties", label: "Trade Parties", icon: "users" },
      { id: "shipments", label: "Shipments", icon: "package" },
      { id: "documents", label: "Documents", icon: "file-text" },
    ]},
    { section: "TOOLS", items: [
      { id: "hs-codes", label: "FBR HS & Duty", icon: "hash" },
      { id: "forex-rates", label: "Forex Rates", icon: "trending-up" },
    ]},
    { section: "COMPLIANCE", items: [
      { id: "compliance-wizard", label: "Compliance Wizard", icon: "shield", badge: compliancePending > 0 ? compliancePending : null },
      { id: "scheme-checker", label: "Import / Export Schemes", icon: "check-square" },
    ]},
  ];

  const navItems = userRole === "Admin" ? [adminSection] : traderSections;

  const displayName = userName;
  const avatar = initials(userName);

  return (
    <aside className={`sidebar ${sidebarOpen ? "open" : "closed"}`}>
      <div className="sidebar-logo">
        <TradeLogo size="sm" />
      </div>

      <div className="sidebar-user">
        <div className="user-avatar">{avatar}</div>
        <div className="user-info">
          <div className="user-name-row">
            <span className="user-name">{displayName}</span>
            {isVerified && userRole === "Trader" && <span className="verified-badge">Verified</span>}
            {userRole === "Admin" && <span className="admin-badge-sm">Admin</span>}
          </div>
          <div className="user-role">{userRole}</div>
        </div>
      </div>

      <nav className="sidebar-nav">
        {navItems.map((section) => (
          <div key={section.section} className="nav-section">
            <div className="nav-section-label">{section.section}</div>
            {section.items.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`nav-item ${isItemActive(item.id) ? "active" : ""}`}
                onClick={() => setActivePage(item.id)}
              >
                <span className="nav-icon"><Icon name={item.icon} /></span>
                <span className="nav-label">{item.label}</span>
                {item.badge && <span className="nav-badge">{item.badge}</span>}
              </button>
            ))}
          </div>
        ))}
      </nav>

      <div className="sidebar-footer">
        <button type="button" className={`nav-item ${activePage === "profile" ? "active" : ""}`} onClick={() => setActivePage("profile")}>
          <span className="nav-icon"><Icon name="user" /></span>
          <span className="nav-label">Profile</span>
        </button>
        <button type="button" className="nav-item nav-item-danger" onClick={onLogout}>
          <span className="nav-icon"><Icon name="logout" /></span>
          <span className="nav-label">Logout</span>
        </button>
      </div>
    </aside>
  );
}
