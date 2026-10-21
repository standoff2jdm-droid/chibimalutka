; Inno Setup Script for Chibi Desktop Pet
#define MyAppName "Chibi Desktop Pet"
#define MyAppVersion "0.8.5-beta"
#define MyAppPublisher "standoff2jdm-droid"
#define MyAppExeName "DesktopPet.exe"

[Setup]
AppId={{D37F61D4-0E39-4428-A45F-5C7221A384A9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={userpf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=release_dist
OutputBaseFilename=ChibiDesktopPet-InnoSetup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "DesktopPet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Запустить_Питомца.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "pet_config.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
