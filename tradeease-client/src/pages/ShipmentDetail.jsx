import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";
import { MapContainer, TileLayer, Marker, Popup, Polyline } from "react-leaflet";
import L from "leaflet";

// Leaflet default icon fix
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

async function geocode(portName) {
  if (!portName) return null;
  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(portName)}&limit=1`);
    const data = await res.json();
    if (data && data.length > 0) {
      return [parseFloat(data[0].lat), parseFloat(data[0].lon)];
    }
  } catch (err) {
    console.error("Geocoding failed for", portName, err);
  }
  return null;
}

const STAGES = ["Booked", "Loaded", "In Transit", "At Port", "Under Customs", "Cleared"];

const STATUS_CLASS = {
  Booked: "status-booked",
  Loaded: "status-loaded",
  "In Transit": "status-transit",
  "At Port": "status-port",
  "Under Customs": "status-customs",
  Cleared: "status-cleared",
};

function statusClass(status) {
  return STATUS_CLASS[status] || "status-booked";
}

function formatDate(iso) {
  if (!iso) return "";
  return new Date(iso).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
}

export default function ShipmentDetail({ shipmentId, setActivePage, onGenerateDoc, backPage = "shipments", readOnly = false }) {
  const showToast = useToast();
  const [shipment, setShipment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [notes, setNotes] = useState("");
  const [previewDoc, setPreviewDoc] = useState(null);
  const [originCoords, setOriginCoords] = useState(null);
  const [destCoords, setDestCoords] = useState(null);

  useEffect(() => {
    if (!shipmentId) return;
    setLoading(true);
    api.shipment(shipmentId)
      .then(setShipment)
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, [shipmentId]);

  useEffect(() => {
    if (!shipment) return;
    if (shipment.originPort) {
      geocode(shipment.originPort).then(setOriginCoords);
    }
    const dest = shipment.destinationPort || shipment.destinationCountry;
    if (dest) {
      geocode(dest).then(setDestCoords);
    }
  }, [shipment?.originPort, shipment?.destinationPort, shipment?.destinationCountry]);

  const mapKey = (originCoords ? originCoords.join() : "none") + "-" + (destCoords ? destCoords.join() : "none");
  const isAtDestination = ["At Port", "Under Customs", "Cleared"].includes(shipment?.status);
  const isInTransit = shipment?.status === "In Transit";
  const currentCoords = isAtDestination ? destCoords : (isInTransit ? null : originCoords);

  const activeStage = shipment ? STAGES.indexOf(shipment.status) : 0;
  const route = `${shipment?.originPort || "—"} → ${shipment?.destinationPort || shipment?.destinationCountry || "—"}`;

  const stages = STAGES.map((stage) => {
    const hist = shipment?.statusHistory?.find((h) => h.status === stage);
    return {
      stage,
      date: hist ? formatDate(hist.changedAt) : "",
      notes: hist?.notes || "",
    };
  });

  const downloadPdf = async (doc) => {
    try {
      await api.downloadDocumentPdf(doc.documentId, doc.documentName);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const markNextStage = async () => {
    const next = Math.min(activeStage + 1, STAGES.length - 1);
    try {
      await api.updateShipmentStatus(shipment.shipmentId, STAGES[next]);
      const updated = await api.shipment(shipment.shipmentId);
      setShipment(updated);
      setNotes(updated.statusHistory?.[updated.statusHistory.length - 1]?.notes || "");
      showToast(`Stage updated to ${STAGES[next]}.`);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const cancelShipment = async () => {
    if (!window.confirm("Are you sure you want to cancel this shipment? This action cannot be fully undone.")) return;
    try {
      await api.deleteShipment(shipment.shipmentId);
      showToast("Shipment cancelled.");
      setActivePage(backPage);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading shipment...</p></div>;
  if (!shipment) return <div className="card"><p className="muted-text">Shipment not found.</p></div>;

  const value = `${shipment.currency} ${Number(shipment.invoiceValue).toLocaleString("en-US", { minimumFractionDigits: 2 })}`;
  const valuePkr = shipment.totalValuePKR
    ? `PKR ${Number(shipment.totalValuePKR).toLocaleString("en-PK", { minimumFractionDigits: 2 })}`
    : "—";

  return (
    <div className="shipment-detail-page">
      <div style={{display: 'flex', justifyContent: 'space-between'}}>
        <button type="button" className="link-btn back-link" onClick={() => setActivePage(backPage)}>
          ← Back to {backPage === "admin-shipments" ? "All Shipments" : "Shipments"}
        </button>
        {!readOnly && shipment.status !== "Cleared" && shipment.status !== "Cancelled" && (
          <button type="button" className="link-btn back-link" style={{color: '#e53e3e'}} onClick={cancelShipment}>
            Cancel Shipment
          </button>
        )}
      </div>

      <div className="detail-header card">
        <div>
          <div className="detail-id-row">
            <h2>{shipment.reference}</h2>
            <span className="type-tag tag-buyer">{shipment.type}</span>
            <span className={`status-badge ${statusClass(shipment.status)}`}>{shipment.status}</span>
          </div>
          <p className="detail-desc">{shipment.productDesc}</p>
          <div className="detail-meta">
            <span>Party: {shipment.partyName || "—"}</span>
            <span className="meta-sep">·</span>
            <span>HS: {shipment.hsCode || "—"}</span>
            <span className="meta-sep">·</span>
            <span>{route}</span>
          </div>
        </div>
        <div className="detail-values">
          <div><span className="s-label">Invoice Value</span><strong>{value}</strong></div>
          <div><span className="s-label">PKR Equivalent</span><strong className="value">{valuePkr}</strong></div>
        </div>
      </div>

      <div className="card timeline-card">
        <h3>Shipment Status Timeline</h3>
        <div className="status-timeline">
          {stages.map((info, index) => {
            const done = index <= activeStage;
            const current = index === activeStage;
            return (
              <div key={info.stage} className={`timeline-step ${done ? "done" : ""} ${current ? "current" : ""}`}>
                <div className="timeline-marker">{done ? "✓" : index + 1}</div>
                <div className="timeline-content">
                  <div className="timeline-title">{info.stage}</div>
                  {info.date && <div className="timeline-date">{info.date}</div>}
                  {info.notes && index !== activeStage && <div className="timeline-notes">{info.notes}</div>}
                  {current && !readOnly && index < STAGES.length - 1 && (
                    <button type="button" className="btn-outline-sm mt-8" onClick={markNextStage}>
                      Mark Next Stage Complete
                    </button>
                  )}
                  {current && !readOnly && index === STAGES.length - 1 && (
                    <button type="button" className="btn-primary mt-8" disabled>
                      Shipment Completed
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {originCoords || destCoords ? (
        <div className="card map-card" style={{ marginBottom: "20px" }}>
          <h3>Live Tracking</h3>
          <div style={{ height: "300px", width: "100%", marginTop: "16px", borderRadius: "8px", overflow: "hidden", zIndex: 0 }}>
            <MapContainer 
               key={mapKey}
               bounds={originCoords && destCoords ? [originCoords, destCoords] : undefined}
               center={currentCoords || originCoords || destCoords || [0,0]} 
               zoom={originCoords && destCoords ? undefined : 5} 
               style={{ height: "100%", width: "100%", zIndex: 0 }}
            >
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; OpenStreetMap'
              />
              {originCoords && (
                <Marker position={originCoords}>
                  <Popup>Origin: {shipment.originPort}</Popup>
                </Marker>
              )}
              {destCoords && (
                <Marker position={destCoords}>
                  <Popup>Destination: {shipment.destinationPort || shipment.destinationCountry}</Popup>
                </Marker>
              )}
              {isInTransit && originCoords && destCoords && (
                <Marker position={[(originCoords[0] + destCoords[0]) / 2, (originCoords[1] + destCoords[1]) / 2]}>
                  <Popup>Shipment is currently In Transit</Popup>
                </Marker>
              )}
              {originCoords && destCoords && (
                <Polyline positions={[originCoords, destCoords]} color="var(--primary)" dashArray="5, 10" />
              )}
            </MapContainer>
          </div>
        </div>
      ) : null}

      <div className="detail-grid">
        <div className="card">
          <div className="card-header">
            <h3>Generated Documents</h3>
            {!readOnly && (
              <button type="button" className="btn-primary btn-sm" onClick={onGenerateDoc}>Generate Invoice</button>
            )}
          </div>
          <div className="doc-mini-list">
            {(shipment.documents || []).length === 0 ? (
              <p className="muted-text">No documents yet.</p>
            ) : shipment.documents.map((doc) => (
              <div key={doc.documentId} className="doc-mini-row">
                <div>
                  <strong>{doc.documentType}</strong>
                  <div className="doc-meta">{doc.documentName} · {formatDate(doc.uploadedAt)}</div>
                </div>
                <div className="doc-actions">
                  <button type="button" className="btn-outline-sm" onClick={() => setPreviewDoc(doc)}>Preview</button>
                  <button type="button" className="btn-outline-sm" onClick={() => downloadPdf(doc)}>Download</button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="card">
          <h3>Stage Notes</h3>
          <textarea className="form-textarea" rows={4} placeholder="Add notes for the current stage..." value={notes} onChange={(e) => setNotes(e.target.value)} readOnly={readOnly} />
          {!readOnly && (
            <button type="button" className="btn-primary mt-8" onClick={() => showToast("Stage notes saved.")}>Save Notes</button>
          )}
        </div>
      </div>

      {previewDoc && (
        <div className="modal-overlay" onClick={() => setPreviewDoc(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Preview — {previewDoc.documentName}</h3>
              <div className="doc-actions">
                <button type="button" className="btn-outline-sm" onClick={() => downloadPdf(previewDoc)}>Download PDF</button>
                <button type="button" className="icon-btn" onClick={() => setPreviewDoc(null)}>✕</button>
              </div>
            </div>
            <div className="modal-body doc-html-preview">
              <h4>{previewDoc.documentType}</h4>
              <p><strong>Shipment:</strong> {shipment.reference}</p>
              <p><strong>Date:</strong> {formatDate(previewDoc.uploadedAt)}</p>
              <p><strong>Consignee:</strong> {shipment.partyName || "—"}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
