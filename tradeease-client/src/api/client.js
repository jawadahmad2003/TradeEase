const TOKEN_KEY = "tradeease_token";
const USER_KEY = "tradeease_user";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser() {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function setAuth(token, user) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearAuth() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

const REQUEST_TIMEOUT_MS = 15000;

const PUBLIC_API_PATHS = [
  /^\/Auth\/login$/i,
  /^\/Auth\/register$/i,
  /^\/Auth\/complete-2fa$/i,
  /^\/Auth\/verify-taxpayer$/i,
  /^\/Health$/i,
];

function isPublicApiPath(path) {
  return PUBLIC_API_PATHS.some((pattern) => pattern.test(path));
}

async function request(path, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {}),
  };

  const token = getToken();
  if (token && !isPublicApiPath(path)) {
    headers.Authorization = `Bearer ${token}`;
  }

  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  let res;
  try {
    res = await fetch(`/api${path}`, { ...options, headers, signal: controller.signal });
  } catch (err) {
    if (err?.name === "AbortError") {
      throw new Error("Request timed out — the server may still be starting. Wait a few seconds and retry.");
    }
    throw new Error("Cannot reach TradeEase API. Press F5 in Visual Studio to start the backend.");
  } finally {
    window.clearTimeout(timeoutId);
  }

  if (res.status === 401) {
    const err = await res.json().catch(() => ({}));
    const message = err.message || "Unauthorized.";
    if (isPublicApiPath(path)) {
      throw new Error(message);
    }
    clearAuth();
    throw new Error("Session expired. Please sign in again.");
  }

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    if (res.status === 503) {
      throw new Error(err.message || "Service temporarily unavailable.");
    }
    if (res.status >= 500) {
      throw new Error(err.message || "Server error — check SQL Server and run database\\setup-database.ps1");
    }
    throw new Error(err.message || `Request failed (${res.status})`);
  }

  if (res.status === 204) return null;
  return res.json();
}

async function downloadBlob(path, filename) {
  const headers = {};
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`/api${path}`, { headers });
  if (!res.ok) {
    const contentType = res.headers.get("content-type") || "";
    if (contentType.includes("application/json")) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || "Download failed.");
    }
    throw new Error(`Download failed (${res.status}).`);
  }
  const blob = await res.blob();
  if (!blob.size) throw new Error("Download returned an empty file.");

  const disposition = res.headers.get("content-disposition") || "";
  const match = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
  const finalName = match ? decodeURIComponent(match[1].replace(/"/g, "")) : filename;

  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = finalName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  // Small delay prevents browsers from aborting the download immediately
  setTimeout(() => URL.revokeObjectURL(url), 500);
}

export const api = {
  login: (email, password) =>
    request("/Auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),

  forgotPassword: (email) =>
    request("/Auth/forgot-password", { method: "POST", body: JSON.stringify({ email }) }),

  resetPassword: (token, newPassword) =>
    request("/Auth/reset-password", { method: "POST", body: JSON.stringify({ token, newPassword }) }),

  complete2fa: (twoFactorToken, code) =>
    request("/Auth/complete-2fa", { method: "POST", body: JSON.stringify({ twoFactorToken, code }) }),

  changePassword: (currentPassword, newPassword) =>
    request("/Auth/change-password", { method: "POST", body: JSON.stringify({ currentPassword, newPassword }) }),

  setup2fa: () => request("/Auth/2fa/setup"),

  enable2fa: (secret, code) =>
    request("/Auth/2fa/enable", { method: "POST", body: JSON.stringify({ secret, code }) }),

  disable2fa: (code, password) =>
    request("/Auth/2fa/disable", { method: "POST", body: JSON.stringify({ code, password }) }),

  register: (data) =>
    request("/Auth/register", { method: "POST", body: JSON.stringify(data) }),

  verifyTaxpayer: (data) =>
    request("/Auth/verify-taxpayer", { method: "POST", body: JSON.stringify(data) }),

  me: () => request("/Auth/me"),

  updateProfile: (data) =>
    request("/Auth/profile", { method: "PUT", body: JSON.stringify(data) }),

  dashboard: (qs = "") => request("/Dashboard" + qs),

  shipments: () => request("/Shipments"),

  shipment: (id) => request(`/Shipments/${id}`),

  createShipment: (data) =>
    request("/Shipments", { method: "POST", body: JSON.stringify(data) }),

  updateShipment: (id, data) =>
    request(`/Shipments/${id}`, { method: "PUT", body: JSON.stringify(data) }),

  deleteShipment: (id) =>
    request(`/Shipments/${id}`, { method: "DELETE" }),

  getShipmentDraft: () => request("/Shipments/draft"),

  saveShipmentDraft: (data) =>
    request("/Shipments/draft", { method: "POST", body: JSON.stringify(data) }),

  clearShipmentDraft: () =>
    request("/Shipments/draft", { method: "DELETE" }),

  updateShipmentStatus: (id, status) =>
    request(`/Shipments/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }),

  tradeParties: () => request("/TradeParties"),

  createTradeParty: (data) =>
    request("/TradeParties", { method: "POST", body: JSON.stringify(data) }),

  updateTradeParty: (id, data) =>
    request(`/TradeParties/${id}`, { method: "PUT", body: JSON.stringify(data) }),

  deleteTradeParty: (id) =>
    request(`/TradeParties/${id}`, { method: "DELETE" }),

  // Payments
  createPaymentIntent: (amount, currency = "usd") =>
    request("/PaymentsApi/create-intent", { method: "POST", body: JSON.stringify({ amount, currency }) }),

  hsCodes: (q = "", limit = 50) => {
    const params = new URLSearchParams({ limit: String(limit) });
    if (q) params.set("q", q);
    return request(`/HSCodes?${params}`);
  },

  hsCodeCount: () => request("/HSCodes/count"),

  productSearch: (q = "", limit = 40, route = {}) => {
    const params = new URLSearchParams({ limit: String(limit) });
    if (q) params.set("q", q);
    if (route.shipmentType) params.set("shipmentType", route.shipmentType);
    if (route.originCountry) params.set("originCountry", route.originCountry);
    if (route.destinationCountry) params.set("destinationCountry", route.destinationCountry);
    return request(`/HSCodes/products?${params}`);
  },

  resolveProduct: (hsCode, route = {}) => {
    const params = new URLSearchParams({ hsCode });
    if (route.shipmentType) params.set("shipmentType", route.shipmentType);
    if (route.originCountry) params.set("originCountry", route.originCountry);
    if (route.destinationCountry) params.set("destinationCountry", route.destinationCountry);
    return request(`/HSCodes/resolve?${params}`);
  },

  referenceCountries: async () => {
    const data = await request("/Reference/countries");
    if (Array.isArray(data)) return data;
    return data?.countries ?? [];
  },

  referenceCountriesInfo: () =>
    request("/Reference/countries").then((data) => ({
      total: data?.total ?? (Array.isArray(data) ? data.length : 0),
      source: data?.source ?? "",
      lastRefreshUtc: data?.lastRefreshUtc,
    })),

  referencePorts: (countryCode) =>
    request(`/Reference/ports?country=${encodeURIComponent(countryCode)}`),

  calculateDuty: (data) =>
    request("/HSCodes/calculate-duty", { method: "POST", body: JSON.stringify(data) }),

  forex: () => request("/Forex"),

  forexTrend: (currency) => request(`/Forex/trend/${currency}`),

  notifications: () => request("/Notifications"),

  documents: () => request("/Documents"),

  createDocument: (data) =>
    request("/Documents", { method: "POST", body: JSON.stringify(data) }),

  downloadDocumentPdf: (id, filename) =>
    downloadBlob(`/Documents/${id}/pdf`, filename.endsWith(".pdf") ? filename : `${filename}.pdf`),

  compliance: () => request("/Compliance"),

  updateComplianceStep: (stepId, data) =>
    request(`/Compliance/${stepId}`, { method: "PATCH", body: JSON.stringify(data) }),

  schemes: () => request("/Schemes"),

  checkSchemes: (data) =>
    request("/Schemes/check", { method: "POST", body: JSON.stringify(data) }),

  adminStats: () => request("/Admin/stats"),

  adminUsers: () => request("/Admin/users"),

  updateUserStatus: (id, isActive) =>
    request(`/Admin/users/${id}`, { method: "PATCH", body: JSON.stringify({ isActive }) }),

  adminShipments: () => request("/Admin/shipments"),

  adminHSCodes: () => request("/Admin/hscodes"),

  createHSCode: (data) =>
    request("/Admin/hscodes", { method: "POST", body: JSON.stringify(data) }),

  updateHSCode: (id, data) =>
    request(`/Admin/hscodes/${id}`, { method: "PUT", body: JSON.stringify(data) }),

  deleteHSCode: (id) =>
    request(`/Admin/hscodes/${id}`, { method: "DELETE" }),

  adminSchemes: () => request("/Admin/schemes"),

  createScheme: (data) =>
    request("/Admin/schemes", { method: "POST", body: JSON.stringify(data) }),

  updateSchemes: (data) =>
    request("/Admin/schemes", { method: "PUT", body: JSON.stringify(data) }),

  deleteScheme: (id) =>
    request(`/Admin/schemes/${id}`, { method: "DELETE" }),

  adminSettings: () => request("/Admin/settings"),

  updateSettings: (data) =>
    request("/Admin/settings", { method: "PUT", body: JSON.stringify(data) }),

  refreshForex: () => request("/Admin/forex/refresh", { method: "POST" }),

  refreshForexRates: () => request("/Forex/refresh", { method: "POST" }),

  health: () => request("/Health"),

  liveStatus: () => request("/Live/status"),

  adminReports: () => request("/Admin/reports"),

  exportAdminReportXlsx: (type) =>
    downloadBlob(
      `/Admin/reports/export/${type}?format=xlsx`,
      `TradeEase-${type}-${new Date().toISOString().slice(0, 10)}.xlsx`
    ),

  exportAdminReportPdf: (type) =>
    downloadBlob(
      `/Admin/reports/export/${type}?format=pdf`,
      `TradeEase-${type}-${new Date().toISOString().slice(0, 10)}.pdf`
    ),
};

