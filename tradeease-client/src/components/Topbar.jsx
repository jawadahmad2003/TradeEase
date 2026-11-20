import { useEffect, useState } from "react";
import { api } from "../api/client";
import LiveSyncBadge from "./LiveSyncBadge";

function formatTime(iso) {
  if (!iso) return "";
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${Math.max(mins, 1)}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

export default function Topbar({ pageInfo, activePage, userRole = "Trader", userName = "User", setSidebarOpen, setActivePage, onAddParty, onUploadDoc }) {
  const [showNotifs, setShowNotifs] = useState(false);
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    api.notifications()
      .then((data) => setNotifications(Array.isArray(data) ? data : []))
      .catch(() => setNotifications([]));
    const timer = window.setInterval(() => {
      api.notifications()
        .then((data) => setNotifications(Array.isArray(data) ? data : []))
        .catch(() => {});
    }, 60_000);
    return () => window.clearInterval(timer);
  }, []);

  const ADMIN_PAGES = new Set([
    "admin-portal", "user-management", "admin-shipments",
    "admin-hs-codes", "admin-schemes", "admin-system", "admin-reports",
  ]);

  function initials(name) {
    return name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
  }

  const isAdminPage = ADMIN_PAGES.has(activePage);
  const avatar = initials(userName);
  const showNewShipment = !isAdminPage && ["dashboard", "shipments"].includes(activePage);
  const showAddParty = activePage === "trade-parties";
  const showUploadDoc = activePage === "documents";
  const showAdminPortal = isAdminPage && activePage !== "admin-portal";
  const hasUnread = notifications.length > 0;

  return (
    <header className="topbar">
      <div className="topbar-left">
        <button type="button" className="menu-toggle" onClick={() => setSidebarOpen((o) => !o)}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/>
          </svg>
        </button>
        <div className="topbar-title-area">
          <h1 className="page-title">{pageInfo?.title}</h1>
          <div className="breadcrumb">
            {pageInfo?.breadcrumb.map((crumb, i) => (
              <span key={i}>
                {i > 0 && <span className="bc-sep"> / </span>}
                <span className={i === pageInfo.breadcrumb.length - 1 ? "bc-current" : "bc-link"}>{crumb}</span>
              </span>
            ))}
          </div>
        </div>
      </div>
      <div className="topbar-right">
        <LiveSyncBadge />
        {showNewShipment && (
          <button type="button" className="btn-primary" onClick={() => setActivePage("new-shipment")}>
            + New Shipment
          </button>
        )}
        {showAddParty && (
          <button type="button" className="btn-primary" onClick={onAddParty}>+ Add Party</button>
        )}
        {showUploadDoc && (
          <button type="button" className="btn-primary" onClick={onUploadDoc}>+ Upload Document</button>
        )}
        {showAdminPortal && (
          <button type="button" className="btn-ghost" onClick={() => setActivePage("admin-portal")}>Admin Portal</button>
        )}

        <div className="notif-wrap">
          <button type="button" className="icon-btn notif-btn" onClick={() => setShowNotifs((v) => !v)}>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/>
            </svg>
            {hasUnread && <span className="notif-dot" />}
          </button>
          {showNotifs && (
            <div className="notif-dropdown">
              <div className="notif-dropdown-head">Notifications</div>
              {notifications.length === 0 && (
                <div className="notif-item muted-text">No notifications right now.</div>
              )}
              {notifications.map((n) => (
                <div key={n.id} className="notif-item">
                  <div>{n.text}</div>
                  <span>{formatTime(n.occurredAt)}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <button type="button" className="user-chip" onClick={() => setActivePage("profile")}>
          <div className="user-avatar-sm">{avatar}</div>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="6 9 12 15 18 9"/></svg>
        </button>
      </div>
    </header>
  );
}
