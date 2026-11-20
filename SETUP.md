# TradeEase — Complete Setup Guide

Follow these steps **in order** on your Windows PC.

---

## What you need (all free)

| Tool | Download | Purpose |
|------|----------|---------|
| **SQL Server Express** | https://www.microsoft.com/sql-server/sql-server-downloads | Database |
| **SSMS** (optional) | Same page / Visual Studio installer | View/edit database |
| **.NET 10 SDK** | https://dotnet.microsoft.com/download | Backend API |
| **Node.js LTS** | https://nodejs.org | React frontend |

**No paid API keys required.** Forex rates use [frankfurter.app](https://www.frankfurter.app) (free, no registration).

---

## Step 1 — Install SQL Server Express

1. Install **SQL Server Express** with default instance name **`SQLEXPRESS`**
2. Enable **Windows Authentication**
3. Open **Services** (`services.msc`) → start **SQL Server (SQLEXPRESS)**

Connection string used by the project:

```
Server=localhost\SQLEXPRESS;Database=TradeEaseDB;Trusted_Connection=True;TrustServerCertificate=True
```

To use a different server, edit `Trade Ease\appsettings.json` → `ConnectionStrings:DefaultConnection`.

---

## Step 2 — Create the database

Open **PowerShell** and run:

```powershell
cd "D:\Trade Ease\Trade Ease\database"
powershell -ExecutionPolicy Bypass -File setup-database.ps1
```

This runs:

1. `TradeEase_Schema.sql` — 15 tables + seed users/roles/HS codes
2. `TradeEase_SampleData.sql` — demo shipments and trade parties
3. `TradeEase_Verify.sql` — confirms 15 tables, 2 users

**If it fails** (database already exists with errors):

```powershell
# In SSMS or sqlcmd, run Reset then setup again:
sqlcmd -S localhost\SQLEXPRESS -i TradeEase_Reset.sql
powershell -ExecutionPolicy Bypass -File setup-database.ps1
```

---

## Step 3 — Start the app (development)

```powershell
cd "D:\Trade Ease\Trade Ease"
powershell -ExecutionPolicy Bypass -File start-dev.ps1
```

Or manually in **two terminals**:

**Terminal A — Backend**

```powershell
cd "D:\Trade Ease\Trade Ease\Trade Ease"
dotnet restore
dotnet run
```

Wait for: `Now listening on: http://localhost:5003`

**Terminal B — Frontend**

```powershell
cd "D:\Trade Ease\Trade Ease\tradeease-client"
npm.cmd install
npm.cmd run dev
```

Wait for: `http://localhost:5173`

---

## Step 4 — Verify everything works

| Check | URL / Action |
|-------|----------------|
| Database health | http://localhost:5003/api/Health |
| Login page | http://localhost:5173 |
| Admin login | `admin@tradeease.pk` / any password |
| Trader login | `ahmed.raza@razaexports.pk` / any password |

Login page shows **"Database connected"** when SQL Server is linked.

---

## Step 5 — Real-time features (already built in)

| Feature | How it works |
|---------|----------------|
| **Forex rates** | Fetched from frankfurter.app → saved to `ForexRates` table |
| **Auto refresh** | Background job every 6 hours |
| **Manual refresh** | Forex page → "Refresh Now" or Admin → System Settings |
| **All modules** | Live JWT API → SQL Server (no mock data) |

---

## Production deploy (single server)

```powershell
cd "D:\Trade Ease\Trade Ease"
powershell -ExecutionPolicy Bypass -File build-production.ps1
```

Set environment variables before running:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SERVER;Database=TradeEaseDB;Trusted_Connection=True;TrustServerCertificate=True"
$env:TRADEEASE_JWT_KEY = "ReplaceWithLongRandomSecretKey32CharsMin!"
cd publish
dotnet "Trade Ease.dll"
```

Open: **http://localhost:5003/app/** (React + API same port)

Update `appsettings.Production.json` → `Cors:AllowedOrigins` with your domain.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Login shows "Database offline" | Run `setup-database.ps1`; check SQL service running |
| `Cannot connect to SQLEXPRESS` | Use `localhost\SQLEXPRESS` not `.` in connection string |
| Registration 500 error | Re-run Schema script (EF keys must match SQL) |
| Forex refresh fails | Internet required; frankfurter.app is free, no API key |
| Port 5003 in use | Change port in `Properties\launchSettings.json` and `vite.config.js` |
| Maintenance mode blocks traders | Admin → System Settings → disable maintenance |

---

## Demo credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@tradeease.pk | any |
| Trader | ahmed.raza@razaexports.pk | any |

New traders can register from the login page (saved to real database with hashed password).

---

## Project structure

```
D:\Trade Ease\Trade Ease\
├── SETUP.md                 ← this guide
├── start-dev.ps1            ← one-click dev start
├── build-production.ps1     ← production build
├── database\                ← SQL scripts + setup-database.ps1
├── Trade Ease\              ← ASP.NET Core API (port 5003)
└── tradeease-client\        ← React UI (port 5173 dev)
```

---

*TradeEase — SCD Spring 2026 · Abdullah Kashif & Team*
