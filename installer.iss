; Instalador do Resource Pins (Inno Setup 6)
#define MyAppName "Resource Pins"
#define MyAppVersion "1.1.0"
#define MyAppExeName "ResourcePins.exe"
#define MyPublishDir "bin\Release\net10.0-windows\win-x64\publish"

[Setup]
AppId={{8F3A1B2C-9D4E-4F5A-B6C7-D8E9F0A1B2C3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Aldrin
DefaultDirName={autopf}\ResourcePins
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=ResourcePinsSetup
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "autostart"; Description: "Iniciar com o Windows (recomendado)"; GroupDescription: "Inicialização:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks:

[Registry]
; Autostart via chave Run — aparece em Gerenciador de Tarefas > Aplicativos de inicialização
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "ResourcePins"; ValueData: """{app}\{#MyAppExeName}"""; \
    Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o {#MyAppName} agora"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/im {#MyAppExeName} /f"; Flags: runhidden; RunOnceId: "KillApp"
