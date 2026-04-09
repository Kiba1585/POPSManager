<#
.SYNOPSIS
    POPSManager Certificate Uninstaller
    Removes the POPSManager code-signing certificate from Windows certificate stores.

.DESCRIPTION
    This script removes the POPSManager self-signed certificate from:
      - Trusted People (CurrentUser)
      - Trusted Publisher (LocalMachine) — requires elevation
    
    Use this to cleanly remove trust before uninstalling POPSManager,
    or when rotating to a new certificate.

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
    [Parameter(HelpMessage = "Path to the .cer certificate file to identify which cert to remove.")]
    [string]$CertificatePath,

    [Parameter(HelpMessage = "Certificate thumbprint to remove (alternative to file path).")]
    [string]$Thumbprint,

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
$Script:LogFile       = Join-Path $Script:LogDir "uninstall_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
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
  ║       POPSManager — Certificate Uninstaller         ║
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
            if ($Thumbprint)      { $arguments += " -Thumbprint `"$Thumbprint`"" }
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
function Resolve-Thumbprint {
    if ($Thumbprint) {
        Write-Log "Using provided thumbprint: $Thumbprint" -Level INFO
        return $Thumbprint
    }

    $certPath = if ($CertificatePath) { $CertificatePath } else { $Script:DefaultCert }
    $certPath = [System.IO.Path]::GetFullPath($certPath)

    if (-not (Test-Path $certPath)) {
        Write-Log "Certificate file not found: $certPath" -Level ERROR
        Write-Log "Provide -CertificatePath or -Thumbprint parameter." -Level INFO
        return $null
    }

    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
        Write-Log "Resolved thumbprint from file: $($cert.Thumbprint)" -Level INFO
        Write-Log "Certificate subject: $($cert.Subject)" -Level INFO
        return $cert.Thumbprint
    }
    catch {
        Write-Log "Failed to read certificate: $_" -Level ERROR
        return $null
    }
}

function Remove-CertificateFromStore {
    param(
        [string]$CertThumbprint,
        [System.Security.Cryptography.X509Certificates.StoreLocation]$StoreLocation,
        [System.Security.Cryptography.X509Certificates.StoreName]$StoreName
    )

    $storeFriendly = "$StoreName ($StoreLocation)"

    try {
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $StoreLocation)
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

        $certsToRemove = $store.Certificates | Where-Object { $_.Thumbprint -eq $CertThumbprint }

        if (-not $certsToRemove -or $certsToRemove.Count -eq 0) {
            Write-Log "Certificate not found in $storeFriendly — nothing to remove." -Level WARN
            $store.Close()
            return $true
        }

        foreach ($cert in $certsToRemove) {
            $store.Remove($cert)
            Write-Log "Certificate removed from $storeFriendly" -Level SUCCESS
        }

        $store.Close()
        return $true
    }
    catch {
        Write-Log "Failed to remove certificate from ${storeFriendly}: $_" -Level ERROR
        return $false
    }
}

# ──────────────────────────────────────────────
# User Confirmation
# ──────────────────────────────────────────────
function Request-UserConfirmation {
    param([string]$CertThumbprint)

    if ($Force) { return $true }

    Write-Host ""
    Write-Host "  This script will REMOVE the $Script:AppName certificate from:" -ForegroundColor White
    Write-Host "    • TrustedPeople    (CurrentUser)"  -ForegroundColor Gray
    Write-Host "    • TrustedPublisher (LocalMachine)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Thumbprint: $CertThumbprint" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  After removal, $Script:AppName MSIX packages will no longer be trusted." -ForegroundColor Yellow
    Write-Host ""

    $response = Read-Host "  Proceed with removal? [y/N]"
    return ($response -match "^[Yy]$")
}

# ──────────────────────────────────────────────
# Main Entry Point
# ──────────────────────────────────────────────
function Main {
    Write-Banner

    Write-Log "Starting $Script:AppName Certificate Uninstaller v$Script:AppVersion"

    # Resolve thumbprint
    $certThumbprint = Resolve-Thumbprint
    if (-not $certThumbprint) {
        $Script:ExitCode = 1
        return
    }

    # Request elevation for LocalMachine store
    Request-Elevation

    # Confirm with user
    if (-not (Request-UserConfirmation -CertThumbprint $certThumbprint)) {
        Write-Log "Uninstallation cancelled by user." -Level WARN
        $Script:ExitCode = 2
        return
    }

    Write-Host ""
    Write-Log "Removing certificate..." -Level INFO

    # Remove from CurrentUser\TrustedPeople
    $result1 = Remove-CertificateFromStore -CertThumbprint $certThumbprint `
        -StoreLocation CurrentUser `
        -StoreName TrustedPeople

    # Remove from LocalMachine\TrustedPublisher
    $result2 = Remove-CertificateFromStore -CertThumbprint $certThumbprint `
        -StoreLocation LocalMachine `
        -StoreName TrustedPublisher

    # Summary
    Write-Host ""
    if ($result1 -and $result2) {
        Write-Log "Certificate removal completed successfully!" -Level SUCCESS
        Write-Log "$Script:AppName MSIX packages will no longer be trusted on this system." -Level INFO
        $Script:ExitCode = 0
    }
    else {
        Write-Log "Some removals failed. Check the log for details." -Level ERROR
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
