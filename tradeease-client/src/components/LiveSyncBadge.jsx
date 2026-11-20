import { useEffect, useState } from "react";
import { api } from "../api/client";

function fmtAgo(iso) {
  if (!iso) return "pending";
  const sec = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (sec < 60) return `${sec}s ago`;
  if (sec < 3600) return `${Math.floor(sec / 60)}m ago`;
  return `${Math.floor(sec / 3600)}h ago`;
}

export default function LiveSyncBadge({ className = "" }) {
  const [status, setStatus] = useState(null);

  useEffect(() => {
    const load = () => api.liveStatus().then(setStatus).catch(() => {});
    load();
    const timer = window.setInterval(load, 5 * 60 * 1000);
    return () => window.clearInterval(timer);
  }, []);

  if (!status) return null;

  return (
    <div className={`live-sync-badge ${className}`} title="Auto-refreshed from public APIs">
      <span className="live-dot" />
      Live · Forex {fmtAgo(status.forexUpdatedUtc)} · FBR tariff {fmtAgo(status.catalogUpdatedUtc)}
    </div>
  );
}
