<#
.SYNOPSIS
    POPSManager Certificate Installer
    Installs the POPSManager code-signing certificate into the Windows certificate stores.

.DESCRIPTION
    This script installs the POPSManager self-signed certificate into the following stores:
      - Trusted People (CurrentUser)
      - Trusted Publisher (LocalMachine) — requires elevation
    
    This is required for MSIX sideloading of POPSManager on systems without
    Microsoft Store distribution.

.NOTES
    Project : POPSManager
    Author  : POPSManager Team
    Version : 1.0.0
    License : MIT
    Requires: PowerShell 5.1+, Windows 10/11

.LINK
    https://github.com/POPSManager/POPSManager
#>

#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter(HelpMessage = "Path to the .cer certificate file.")]
    [string]$CertificatePath,

    [Parameter(HelpMessage = "Skip user confirmation prompts.")]
    [switch]$Force,

    [Parameter(HelpMessage = "Enable verbose logging to file.")]
    [switch]$LogToFile
)

# ──────────────────────────────────────────────
# Configuration
# ──────────────────────────────────────────────
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:AppName       = "POPSManager"
$Script:AppVersion    = "1.0.0"
$Script:LogDir        = Join-Path $PSScriptRoot "logs"
$Script:LogFile       = Join-Path $Script:LogDir "install_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$Script:DefaultCert   = Join-Path $PSScriptRoot "certificates\POPSManager.cer"
$Script:ExitCode      = 0

# ──────────────────────────────────────────────
# Logging
# ──────────────────────────────────────────────
function Write-Log {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Message,
        [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry     = "[$timestamp] [$Level] $Message"

    switch ($Level) {
        "INFO"    { Write-Host "  [*] $Message" -ForegroundColor Cyan }
        "WARN"    { Write-Host "  [!] $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "  [-] $Message" -ForegroundColor Red }
        "SUCCESS" { Write-Host "  [+] $Message" -ForegroundColor Green }
    }

    if ($LogToFile) {
        if (-not (Test-Path $Script:LogDir)) {
            New-Item -ItemType Directory -Path $Script:LogDir -Force | Out-Null
        }
        Add-Content -Path $Script:LogFile -Value $entry -Encoding UTF8
    }
}

function Write-Banner {
    $banner = @"

  ╔══════════════════════════════════════════════════════╗
  ║       POPSManager — Certificate Installer           ║
  ║       Version $($Script:AppVersion)                                 ║
  ╚══════════════════════════════════════════════════════╝

"@
    Write-Host $banner -ForegroundColor Magenta
}

# ──────────────────────────────────────────────
# Elevation Check
# ──────────────────────────────────────────────
function Test-Administrator {
    $identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Request-Elevation {
    if (-not (Test-Administrator)) {
        Write-Log "Administrator privileges required. Requesting elevation..." -Level WARN
        try {
            $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
            if ($CertificatePath) { $arguments += " -CertificatePath `"$CertificatePath`"" }
            if ($Force)           { $arguments += " -Force" }
            if ($LogToFile)       { $arguments += " -LogToFile" }

            Start-Process -FilePath "powershell.exe" -ArgumentList $arguments -Verb RunAs -Wait
            exit $LASTEXITCODE
        }
        catch {
            Write-Log "Failed to elevate. Please run as Administrator." -Level ERROR
            exit 1
        }
    }
}

# ──────────────────────────────────────────────
# Certificate Operations
# ──────────────────────────────────────────────
function Test-CertificateFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Log "Certificate file not found: $Path" -Level ERROR
        return $false
    }

    $ext = [System.IO.Path]::GetExtension($Path).ToLower()
    if ($ext -notin @(".cer", ".crt", ".der")) {
        Write-Log "Unsupported certificate format '$ext'. Use .cer, .crt, or .der" -Level ERROR
        return $false
    }

    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Path)
        Write-Log "Certificate subject : $($cert.Subject)" -Level INFO
        Write-Log "Certificate issuer  : $($cert.Issuer)" -Level INFO
        Write-Log "Thumbprint          : $($cert.Thumbprint)" -Level INFO
        Write-Log "Valid from           : $($cert.NotBefore.ToString('yyyy-MM-dd')) to $($cert.NotAfter.ToString('yyyy-MM-dd'))" -Level INFO

        if ($cert.NotAfter -lt (Get-Date)) {
            Write-Log "WARNING: Certificate has expired on $($cert.NotAfter.ToString('yyyy-MM-dd'))!" -Level WARN
        }

        return $true
    }
    catch {
        Write-Log "Invalid certificate file: $_" -Level ERROR
        return $false
    }
}

function Install-CertificateToStore {
    param(
        [string]$Path,
        [System.Security.Cryptography.X509Certificates.StoreLocation]$StoreLocation,
        [System.Security.Cryptography.X509Certificates.StoreName]$StoreName
    )

    $storeFriendly = "$StoreName ($StoreLocation)"

    try {
        $cert  = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Path)
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $StoreLocation)
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

        # Check if already installed
        $existing = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
        if ($existing) {
            Write-Log "Certificate already exists in $storeFriendly — skipping." -Level WARN
            $store.Close()
            return $true
        }

        $store.Add($cert)
        $store.Close()

        Write-Log "Certificate installed successfully in $storeFriendly" -Level SUCCESS
        return $true
    }
    catch {
        Write-Log "Failed to install certificate in ${storeFriendly}: $_" -Level ERROR
        return $false
    }
}

# ──────────────────────────────────────────────
# User Confirmation
# ──────────────────────────────────────────────
function Request-UserConfirmation {
    if ($Force) { return $true }

    Write-Host ""
    Write-Host "  This script will install the $Script:AppName certificate into:" -ForegroundColor White
    Write-Host "    • TrustedPeople   (CurrentUser)"  -ForegroundColor Gray
    Write-Host "    • TrustedPublisher (LocalMachine)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  This allows Windows to trust MSIX packages signed with this certificate." -ForegroundColor Gray
    Write-Host ""

    $response = Read-Host "  Proceed with installation? [Y/n]"
    return ($response -eq "" -or $response -match "^[Yy]")
}

# ──────────────────────────────────────────────
# Main Entry Point
# ──────────────────────────────────────────────
function Main {
    Write-Banner

    # Resolve certificate path
    $certPath = if ($CertificatePath) { $CertificatePath } else { $Script:DefaultCert }
    $certPath = [System.IO.Path]::GetFullPath($certPath)

    Write-Log "Starting $Script:AppName Certificate Installer v$Script:AppVersion"
    Write-Log "Certificate path: $certPath"

    # Validate certificate
    if (-not (Test-CertificateFile -Path $certPath)) {
        $Script:ExitCode = 1
        return
    }

    # Request elevation for LocalMachine store
    Request-Elevation

    # Confirm with user
    if (-not (Request-UserConfirmation)) {
        Write-Log "Installation cancelled by user." -Level WARN
        $Script:ExitCode = 2
        return
    }

    Write-Host ""
    Write-Log "Installing certificate..." -Level INFO

    # Install to CurrentUser\TrustedPeople
    $result1 = Install-CertificateToStore -Path $certPath `
        -StoreLocation CurrentUser `
        -StoreName TrustedPeople

    # Install to LocalMachine\TrustedPublisher
    $result2 = Install-CertificateToStore -Path $certPath `
        -StoreLocation LocalMachine `
        -StoreName TrustedPublisher

    # Summary
    Write-Host ""
    if ($result1 -and $result2) {
        Write-Log "All certificates installed successfully!" -Level SUCCESS
        Write-Log "You can now install $Script:AppName MSIX packages." -Level INFO
        $Script:ExitCode = 0
    }
    else {
        Write-Log "Some installations failed. Check the log for details." -Level ERROR
        $Script:ExitCode = 1
    }

    if ($LogToFile) {
        Write-Log "Log saved to: $Script:LogFile" -Level INFO
    }

    Write-Host ""
    Write-Host "  Press any key to exit..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# Run
try {
    Main
}
catch {
    Write-Log "Unexpected error: $_" -Level ERROR
    $Script:ExitCode = 1
}
finally {
    exit $Script:ExitCode
}
