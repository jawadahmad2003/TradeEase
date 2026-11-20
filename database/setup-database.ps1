# TradeEase - Setup database (SQL Server Express / LocalDB)
# Usage:
#   powershell -ExecutionPolicy Bypass -File setup-database.ps1
#   powershell -ExecutionPolicy Bypass -File setup-database.ps1 -Server "(localdb)\MSSQLLocalDB"

param(
    [string]$Server = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$DbFolder = Join-Path $Root "database"
$AppSettings = Join-Path $Root "Trade Ease\appsettings.json"
$AppSettingsDev = Join-Path $Root "Trade Ease\appsettings.Development.json"

Write-Host ""
Write-Host "=== TradeEase Database Setup ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: sqlcmd not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Install SQL Server Express (free):" -ForegroundColor Yellow
    Write-Host "  https://www.microsoft.com/sql-server/sql-server-downloads"
    Write-Host ""
    exit 1
}

function Get-SqlInstances {
    $instances = @()

    $regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    if (Test-Path $regPath) {
        $props = Get-ItemProperty $regPath
        foreach ($name in $props.PSObject.Properties.Name) {
            if ($name -eq "PSPath") { continue }
            if ($name -eq "PSParentPath") { continue }
            if ($name -eq "PSChildName") { continue }
            if ($name -eq "PSDrive") { continue }
            if ($name -eq "PSProvider") { continue }

            if ($name -eq "MSSQLSERVER") {
                $instances += "localhost"
            }
            else {
                $instances += "localhost\$name"
                $instances += ".\$name"
            }
        }
    }

    $extras = @("(localdb)\MSSQLLocalDB", "localhost\SQLEXPRESS", ".\SQLEXPRESS", "localhost")
    foreach ($extra in $extras) {
        if ($instances -notcontains $extra) {
            $instances += $extra
        }
    }

    return $instances
}

function Test-SqlConnection {
    param([string]$Instance)

    $null = sqlcmd -S $Instance -Q "SELECT 1" -h -1 -W 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Run-SqlFile {
    param(
        [string]$Instance,
        [string]$File
    )

    $fileName = [System.IO.Path]::GetFileName($File)
    Write-Host "Running $fileName..." -ForegroundColor Yellow
    sqlcmd -S $Instance -b -i $File
    if ($LASTEXITCODE -ne 0) {
        throw "SQL failed: $File"
    }
}

function Update-ConnectionString {
    param([string]$Instance)

    $conn = "Server=$Instance;Database=TradeEaseDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    $paths = @($AppSettings, $AppSettingsDev)

    foreach ($path in $paths) {
        if (-not (Test-Path $path)) { continue }

        try {
            $content = Get-Content $path -Raw
            if ($content -match '"DefaultConnection"\s*:') {
                $content = $content -replace '"DefaultConnection"\s*:\s*"[^"]*"', ('"DefaultConnection": "' + $conn + '"')
            }
            else {
                Write-Host "Skipping $path - no DefaultConnection key found." -ForegroundColor Yellow
                continue
            }

            Set-Content -Path $path -Value $content -Encoding UTF8
            $leaf = [System.IO.Path]::GetFileName($path)
            Write-Host "Updated connection string in $leaf" -ForegroundColor Green
        }
        catch {
            Write-Host "Could not auto-update $path" -ForegroundColor Yellow
            Write-Host "Set DefaultConnection manually to:"
            Write-Host "  $conn"
        }
    }
}

# Pick SQL Server instance
if ([string]::IsNullOrWhiteSpace($Server)) {
    Write-Host "Searching for SQL Server on this PC..." -ForegroundColor Cyan
    $candidates = Get-SqlInstances

    foreach ($candidate in $candidates) {
        Write-Host "  Trying $candidate ..." -NoNewline
        if (Test-SqlConnection -Instance $candidate) {
            Write-Host " OK" -ForegroundColor Green
            $Server = $candidate
            break
        }
        Write-Host " failed" -ForegroundColor DarkGray
    }
}
else {
    Write-Host "Using server: $Server"
    if (-not (Test-SqlConnection -Instance $Server)) {
        throw "Cannot connect to $Server"
    }
    Write-Host "SQL Server connection OK" -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($Server)) {
    Write-Host ""
    Write-Host "FAILED: No SQL Server instance found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Try one of these:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Start SQL Server (SQLEXPRESS) in services.msc"
    Write-Host "2. Install SQL Server Express from microsoft.com/sql-server"
    Write-Host "3. Use LocalDB (Visual Studio):"
    Write-Host '   powershell -ExecutionPolicy Bypass -File setup-database.ps1 -Server "(localdb)\MSSQLLocalDB"'
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "Using server: $Server" -ForegroundColor Green
Write-Host ""

try {
    $schema = Join-Path $DbFolder "TradeEase_Schema.sql"
    $sample = Join-Path $DbFolder "TradeEase_SampleData.sql"
    $verify = Join-Path $DbFolder "TradeEase_Verify.sql"

    if (-not (Test-Path $schema)) {
        Write-Host "ERROR: Missing $schema" -ForegroundColor Red
        exit 1
    }

    Run-SqlFile -Instance $Server -File $schema

    if (Test-Path $sample) {
        Run-SqlFile -Instance $Server -File $sample
    }

    Update-ConnectionString -Instance $Server

    $patch = Join-Path $DbFolder "TradeEase_Patch_v2.sql"
    if (Test-Path $patch) {
        Run-SqlFile -Instance $Server -File $patch
    }

    $patch3 = Join-Path $DbFolder "TradeEase_Patch_v3.sql"
    if (Test-Path $patch3) {
        Run-SqlFile -Instance $Server -File $patch3
    }

    $taxpayers = Join-Path $DbFolder "TradeEase_Patch_Taxpayers.sql"
    if (Test-Path $taxpayers) {
        Run-SqlFile -Instance $Server -File $taxpayers
    }

    $reporting = Join-Path $DbFolder "TradeEase_Reporting_Schema.sql"
    if (Test-Path $reporting) {
        Run-SqlFile -Instance $Server -File $reporting
    }


    $patchHs = Join-Path $DbFolder "TradeEase_Patch_HS_Extended.sql"
    if (Test-Path $patchHs) {
        Run-SqlFile -Instance $Server -File $patchHs
    }

    $patchHsBulk = Join-Path $DbFolder "TradeEase_Patch_HS_Bulk.sql"
    if (Test-Path $patchHsBulk) {
        Run-SqlFile -Instance $Server -File $patchHsBulk
    }

    $patch4 = Join-Path $DbFolder "TradeEase_Patch_v4.sql"
    if (Test-Path $patch4) {
        Run-SqlFile -Instance $Server -File $patch4
    }

    Write-Host ""
    Write-Host "Running verification..." -ForegroundColor Yellow
    sqlcmd -S $Server -i $verify

    Write-Host ""
    Write-Host "SUCCESS: TradeEaseDB is ready on $Server" -ForegroundColor Green
    Write-Host "Next: run BUILD-UI.bat, then press F5 in Visual Studio." -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "FAILED: $_" -ForegroundColor Red
    Write-Host "If DB already exists, run TradeEase_Reset.sql in SSMS, then run this script again."
    exit 1
}
