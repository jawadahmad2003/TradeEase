import { useCallback, useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

function typeClass(type) {
  if (type === "Commercial Invoice") return "tag-invoice";
  if (type === "Packing List") return "tag-packing";
  if (type === "Uploaded") return "tag-cert";
  return "tag-invoice";
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
}

export default function Documents({
  openUploadOnMount = false,
  openGenerateOnMount = false,
  onUploadModalClose,
  onGenerateModalClose,
}) {
  const showToast = useToast();
  const [documents, setDocuments] = useState([]);
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("All Types");
  const [showGenerator, setShowGenerator] = useState(false);
  const [showUpload, setShowUpload] = useState(false);
  const [showPreviewDoc, setShowPreviewDoc] = useState(null);
  const [docType, setDocType] = useState("Commercial Invoice");
  const [shipmentId, setShipmentId] = useState("");
  const [preview, setPreview] = useState(false);
  const [uploadName, setUploadName] = useState("");
  const [docNotes, setDocNotes] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    Promise.all([api.documents(), api.shipments()])
      .then(([docs, ships]) => {
        setDocuments(docs.map((d) => ({
          id: d.documentId,
          name: d.fileName,
          type: d.documentType,
          typeClass: typeClass(d.documentType),
          shipment: d.shipmentReference || "—",
          shipmentId: d.shipmentId,
          date: formatDate(d.uploadedAt),
          size: d.fileSizeKb ? `${d.fileSizeKb} KB` : "—",
        })));
        setShipments(ships);
        if (ships.length > 0) setShipmentId(String(ships[0].shipmentId));
      })
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (openUploadOnMount) { setShowUpload(true); onUploadModalClose?.(); }
  }, [openUploadOnMount, onUploadModalClose]);

  useEffect(() => {
    if (openGenerateOnMount) { setShowGenerator(true); setPreview(false); onGenerateModalClose?.(); }
  }, [openGenerateOnMount, onGenerateModalClose]);

  const filtered = documents.filter((d) => {
    const matchSearch = d.name.toLowerCase().includes(search.toLowerCase()) || String(d.id).includes(search);
    const matchType = typeFilter === "All Types" || d.type === typeFilter;
    return matchSearch && matchType;
  });

  const selectedShipment = shipments.find((s) => String(s.shipmentId) === shipmentId);
  const nextNumber = docType === "Commercial Invoice" ? `INV-2026-${String(documents.length + 25).padStart(4, "0")}` : `PKL-2026-${String(documents.length + 25).padStart(4, "0")}`;

  const generateDocument = async () => {
    try {
      const created = await api.createDocument({
        documentType: docType,
        documentName: `${docType} - ${selectedShipment?.reference || "General"}`,
        shipmentId: shipmentId ? parseInt(shipmentId, 10) : null,
        notes: docNotes || null,
      });
      setShowGenerator(false);
      showToast(`${docType} generated successfully.`);
      await api.downloadDocumentPdf(created.documentId, created.fileName);
      load();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const downloadPdf = async (doc) => {
    try {
      await api.downloadDocumentPdf(doc.id, doc.name);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  const uploadDocument = async () => {
    if (!uploadName.trim()) {
      showToast("Please enter a document name.", "error");
      return;
    }
    try {
      await api.createDocument({
        documentType: "Uploaded",
        documentName: uploadName,
        shipmentId: null,
        notes: null,
      });
      setShowUpload(false);
      setUploadName("");
      showToast("Document uploaded successfully.");
      load();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading documents...</p></div>;

  return (
    <div className="documents-page">
      <div className="filters-bar">
        <div className="search-wrap flex-1">
          <input className="search-input" placeholder="Search documents..." value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="filter-select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option>All Types</option>
          <option>Commercial Invoice</option>
          <option>Packing List</option>
          <option>Bill of Entry (B/E)</option>
          <option>Goods Declaration (GD)</option>
          <option>Bill of Lading / AWB</option>
          <option>Certificate of Origin</option>
          <option>Uploaded</option>
        </select>
        <button type="button" className="btn-ghost" onClick={() => setShowUpload(true)}>Upload</button>
        <button type="button" className="btn-primary" onClick={() => { setShowGenerator(true); setPreview(false); }}>
          Generate Document
        </button>
      </div>

      <div className="card">
        {filtered.length === 0 ? (
          <p className="muted-text" style={{ padding: 20 }}>No documents yet.</p>
        ) : filtered.map((d) => (
          <div key={d.id} className="doc-row">
            <div className="doc-icon-wrap">📄</div>
            <div className="doc-main">
              <div className="doc-title-row">
                <span className="doc-name">{d.name}</span>
                <span className={`type-tag ${d.typeClass}`}>{d.type}</span>
              </div>
              <div className="doc-meta">
                <span>#{d.id}</span>
                <span className="meta-sep">·</span>
                <span>Shipment: {d.shipment}</span>
                <span className="meta-sep">·</span>
                <span>{d.date}</span>
              </div>
            </div>
            <div className="doc-actions">
              <button type="button" className="btn-outline-sm" onClick={() => setShowPreviewDoc(d)}>Preview</button>
              <button type="button" className="btn-outline-sm" onClick={() => downloadPdf(d)}>Download PDF</button>
            </div>
          </div>
        ))}
      </div>

      {showPreviewDoc && (
        <div className="modal-overlay" onClick={() => setShowPreviewDoc(null)}>
          <div className="modal-card doc-generator-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Document Preview — {showPreviewDoc.name}</h3>
              <button type="button" className="btn-outline-sm" onClick={() => downloadPdf(showPreviewDoc)}>Download PDF</button>
              <button type="button" className="btn-outline-sm" onClick={() => setShowPreviewDoc(null)}>Close</button>
            </div>
            <div className="modal-body doc-html-preview">
              <h4>{showPreviewDoc.type}</h4>
              <p><strong>Shipment:</strong> {showPreviewDoc.shipment}</p>
              <p><strong>Date:</strong> {showPreviewDoc.date}</p>
            </div>
          </div>
        </div>
      )}

      {showUpload && (
        <div className="modal-overlay" onClick={() => setShowUpload(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Upload Document</h3>
              <button type="button" className="icon-btn" onClick={() => setShowUpload(false)}>✕</button>
            </div>
            <div className="modal-body">
              <label className="form-group">
                <span className="form-label">Document Name</span>
                <input className="form-input" value={uploadName} onChange={(e) => setUploadName(e.target.value)} placeholder="e.g. Bill of Lading" />
              </label>
              <label className="form-group">
                <span className="form-label">File</span>
                <input className="form-input" type="file" />
              </label>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => setShowUpload(false)}>Cancel</button>
              <button type="button" className="btn-primary" onClick={uploadDocument}>Upload</button>
            </div>
          </div>
        </div>
      )}

      {showGenerator && (
        <div className="modal-overlay" onClick={() => setShowGenerator(false)}>
          <div className="modal-card doc-generator-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Document Generator</h3>
              <button type="button" className="icon-btn" onClick={() => setShowGenerator(false)}>✕</button>
            </div>
            <div className="modal-body">
              <label className="form-group">
                <span className="form-label">Document Type</span>
                <select className="form-select" value={docType} onChange={(e) => setDocType(e.target.value)}>
                  <option>Commercial Invoice</option>
                  <option>Packing List</option>
                  <option>Bill of Entry (B/E)</option>
                  <option>Goods Declaration (GD)</option>
                  <option>Bill of Lading / AWB</option>
                  <option>Certificate of Origin</option>
                </select>
              </label>
              <label className="form-group">
                <span className="form-label">Shipment</span>
                <select className="form-select" value={shipmentId} onChange={(e) => setShipmentId(e.target.value)}>
                  {shipments.map((s) => (
                    <option key={s.shipmentId} value={s.shipmentId}>{s.reference} — {s.productDesc}</option>
                  ))}
                </select>
              </label>
              <div className="doc-number-preview">Next number: <strong>{nextNumber}</strong></div>
              <label className="form-group">
                <span className="form-label">Custom Notes (optional)</span>
                <textarea className="form-textarea" rows={2} value={docNotes} onChange={(e) => setDocNotes(e.target.value)} />
              </label>
              {preview && selectedShipment && (
                <div className="doc-html-preview">
                  <h4>{docType}</h4>
                  <p><strong>Shipment:</strong> {selectedShipment.reference}</p>
                  <p><strong>Product:</strong> {selectedShipment.productDesc}</p>
                  {docNotes && <p><strong>Notes:</strong> {docNotes}</p>}
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn-ghost" onClick={() => setPreview(true)}>HTML Preview</button>
              <button type="button" className="btn-primary" onClick={generateDocument}>Generate & Download PDF</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
