; ============================================
;  POPSManager - Instalador Profesional
;  Inno Setup Script
; ============================================

[Setup]
AppId={{A1B2C3D4-E5F6-1234-9876-ABCDEF123456}
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

; ============================================
;  ARCHIVOS DEL PROGRAMA
; ============================================
[Files]
Source: "..\installer_build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ============================================
;  ACCESOS DIRECTOS
; ============================================
[Icons]
Name: "{group}\POPSManager"; Filename: "{app}\POPSManager.exe"
Name: "{commondesktop}\POPSManager"; Filename: "{app}\POPSManager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"

; ============================================
;  EJECUTAR AL FINAL
; ============================================
[Run]
Filename: "{app}\POPSManager.exe"; Description: "Iniciar POPSManager"; Flags: nowait postinstall skipifsilent

; ============================================
;  DETECCIÓN DE VERSIONES PREVIAS
;  (DEBE IR AL FINAL DEL ARCHIVO)
; ============================================
[Code]
function InitializeSetup(): Boolean;
var
  UninstallCmd: String;
  ExecResult: Boolean;
  ExitCode: Integer;
begin
  if RegQueryStringValue(HKLM,
     'Software\Microsoft\Windows\CurrentVersion\Uninstall\POPSManager_is1',
     'UninstallString', UninstallCmd) then
  begin
    MsgBox('Se encontró una versión previa de POPSManager. Será desinstalada antes de continuar.',
           mbInformation, MB_OK);

    ExecResult := Exec(UninstallCmd, '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ExitCode);
  end;

  Result := True;
end;
