; ============================================
;  POPSManager - Instalador Profesional
;  Inno Setup Script
; ============================================

[Setup]
AppName=POPSManager
AppVersion=1.0.0
AppPublisher=POPSManager Team
AppPublisherURL=https://github.com/raideldev/POPSManager
DefaultDirName={pf}\POPSManager
DefaultGroupName=POPSManager
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=POPSManager_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=Assets\AppIcon.ico
WizardImageFile=Assets\WizardImage.bmp
WizardSmallImageFile=Assets\WizardSmall.bmp
UninstallDisplayIcon={app}\POPSManager.exe
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Files]
; ============================================
;  ARCHIVOS DEL PROGRAMA
;  (Se copian desde la carpeta publish)
; ============================================
Source: "..\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\POPSManager"; Filename: "{app}\POPSManager.exe"
Name: "{commondesktop}\POPSManager"; Filename: "{app}\POPSManager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"

[Run]
Filename: "{app}\POPSManager.exe"; Description: "Iniciar POPSManager"; Flags: nowait postinstall skipifsilent
