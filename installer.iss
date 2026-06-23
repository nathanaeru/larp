[Setup]
; Application Info
AppName=LARP DNS Switcher
AppVersion=1.0
AppPublisher=Naeru
AppId={{f4fd7dc1-e3ac-4128-bab4-583ac9bd1772}

; Installation Paths
DefaultDirName={localappdata}\LARP DNS Switcher
PrivilegesRequired=lowest
DefaultGroupName=LARP DNS Switcher

; Output Settings (Where the final installer.exe will be saved)
OutputDir=build
OutputBaseFilename=LARP_Setup
UninstallDisplayIcon={app}\larp.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
; Tells the installer to grab your compiled exe from the build folder
Source: "build\larp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Creates the Start Menu Shortcut
Name: "{group}\LARP DNS Switcher"; Filename: "{app}\larp.exe"
; Creates an optional Desktop Shortcut
Name: "{autodesktop}\LARP DNS Switcher"; Filename: "{app}\larp.exe"; Tasks: desktopicon

[Tasks]
; Adds a checkbox to the installer asking if they want a desktop icon
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
; Gives the user a checkbox to launch the app immediately after installation finishes
Filename: "{app}\larp.exe"; Description: "{cm:LaunchProgram,LARP DNS Switcher}"; Flags: nowait postinstall skipifsilent