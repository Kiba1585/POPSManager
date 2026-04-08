Write-Host "============================================="
Write-Host " Instalador del certificado público de POPSManager"
Write-Host "============================================="
Write-Host ""

$certPath = Join-Path -Path (Get-Location) -ChildPath "POPSManager_Public.cer"

if (-Not (Test-Path $certPath)) {
    Write-Host "ERROR: No se encontró el archivo POPSManager_Public.cer"
    Pause
    exit
}

Write-Host "Instalando certificado en 'TrustedPeople'..."
Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null

Write-Host ""
Write-Host "✔ Certificado instalado correctamente."
Write-Host "✔ Ahora Windows reconocerá a POPSManager como editor confiable."
Write-Host ""
Pause
