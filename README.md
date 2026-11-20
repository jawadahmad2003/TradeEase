# TradeEase - Pakistan Import & Export Management

TradeEase is a comprehensive platform for managing shipments, user registration, duty calculations, and export schemes. It consists of a C# .NET Backend and a React (Vite) Frontend.

## Prerequisites

Before running the project, ensure you have the following installed:
- [Node.js](https://nodejs.org/) (for the frontend)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (for the backend)
- SQL Server (for the database)

## How to Run the Project

The project is split into two main parts: the backend API and the frontend client. You will need to start both of them.

### 1. Start the Backend (.NET)
Open your terminal (PowerShell or Command Prompt) and run the following commands:
```powershell
cd "d:\Trade Ease\Trade Ease"
dotnet run --project "Trade Ease\Trade Ease.csproj"
```
*The backend will typically start on port `7126`.*

### 2. Start the Frontend (React / Vite)
Open a **new** terminal window and run:
```powershell
cd "d:\Trade Ease\Trade Ease\tradeease-client"
npm install
npm run dev
```
*The frontend will start and be accessible at `http://localhost:5173/spa/`.*

## Database Setup

If you need to re-initialize the database or insert mock data:
1. Open PowerShell in `d:\Trade Ease\Trade Ease\database`
2. Run `.\setup-database.ps1`

## Testing Registration (Dummy Data)

When a new trader registers, the system verifies their identity against an FBR (Active Taxpayer List) mock registry. 

I have generated a PDF file named **`Dummy_Registration_Data.pdf`** in the root of the project (`d:\Trade Ease\Trade Ease\Dummy_Registration_Data.pdf`). This PDF contains a list of dummy CNICs, NTNs, Full Names, and Business Names that you can use to successfully pass the verification step during registration.
