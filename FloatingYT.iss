#define MyAppName "FloatingYT"
#define MyAppVersion "1.0.1"
#define MyAppExeName "FloatingYT.exe"
#define PublishDir "publish"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Jube Hare
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=installer
OutputBaseFilename=FloatingYT_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.xml"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installing WebView2 Runtime..."; Check: NeedsWebView2; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "Launch FloatingYT"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;

function NeedsWebView2(): Boolean;
var
  Version: String;
begin
  Result := not RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version)
         and not RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version);
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if (CurPageID = wpReady) and NeedsWebView2() then
  begin
    DownloadPage.Clear;
    DownloadPage.Add(
      'https://go.microsoft.com/fwlink/p/?LinkId=2124703',
      'MicrosoftEdgeWebview2Setup.exe',
      ''
    );
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        Result := True;
      except
        MsgBox('Failed to download WebView2 Runtime: ' + GetExceptionMessage + #13#10 +
               'Please install it manually from https://developer.microsoft.com/en-us/microsoft-edge/webview2/',
               mbError, MB_OK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end else
    Result := True;
end;
