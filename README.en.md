# Oncology Hospital Remote

[Tiếng Việt](README.md) | [English](README.en.md)

Oncology Hospital Remote is a remote-control application intended for use on a
local network. It is built with .NET 9, WinForms, ASP.NET Core, and SignalR. The
administration interface can run inside the Windows application or directly in
a web browser.

The current version is suitable for testing and controlled LAN environments.
The server does not yet authenticate Agent connections, and its default HTTP
traffic is not encrypted. Do not expose port `5000` directly to the Internet.

## Projects

The solution contains three projects:

- `RemoteDesktop.AdminApp`: the main distributable application. It lets the
  operator choose between the administrator and Agent roles. In administrator
  mode, it starts the server and displays the administration page through
  WebView2.
- `RemoteDesktop.Agent`: runs on the computer receiving support. It sends
  machine details and screen frames, and receives mouse and keyboard input from
  the administrator.
- `RemoteDesktop.Server`: the ASP.NET Core and SignalR server. It also serves
  the administration page at `http://localhost:5000`.

`RemoteDesktop.AdminApp` references the other two projects and is the project
that should be installed or distributed to users.

## Current features

- Automatic server discovery over the local network.
- A list of connected computers.
- Remote screen viewing.
- Mouse and keyboard input forwarding.
- Locking and unlocking the physical mouse on an Agent computer.
- Support requests sent from an Agent to the administrator.
- Vietnamese and English interfaces.
- Automatic updates from GitHub Releases.

## Requirements

The development environment requires:

- 64-bit Windows 10 or Windows 11.
- .NET 9 SDK.
- Node.js and npm.
- Visual Studio 2022 when running or debugging through the IDE.
- Microsoft Edge WebView2 Runtime for the embedded administration interface in
  `RemoteDesktop.AdminApp`.

Release builds are self-contained, so users do not need to install the .NET
Runtime separately. WebView2 Runtime is still required.

## Preparing the source tree

Clone the repository and install the JavaScript packages:

```powershell
git clone https://github.com/Oncology-Hospital/Remote.git
cd Remote
npm ci --prefix .\src\RemoteDesktop.Server
dotnet restore .\RemoteDesktop.sln
```

The `node_modules` directory is not committed. `npm ci` installs the exact
versions recorded in `package-lock.json`.

## Administrator account configuration

The application creates the default administrator account when the account file
does not exist or contains `[]`:

```text
Username: .\administrator
Password: cntt@it
```

The account is stored at:

```text
%LocalAppData%\Oncology-Hospital\Remote\accounts.json
```

The generated content is:

```json
[
  {
    "username": ".\\administrator",
    "password": "cntt@it",
    "role": "admin"
  }
]
```

The real `accounts.json` file is excluded by `.gitignore` and is not included in
the installer or update packages. Files under `%LocalAppData%` are preserved
when the application is updated.

The password is stored as plain text and is the same on every installation. It
is an application account, not the actual Windows administrator account.

## Running from Visual Studio

1. Open `RemoteDesktop.sln`.
2. Right-click the solution and select `Configure Startup Projects`.
3. Select `Single startup project`.
4. Set `RemoteDesktop.AdminApp` as the startup project.
5. Press `F5`.
6. Select `Admin` to start the server and administration interface, or select
   `Agent` to open the support client.

To test both roles on one computer, start two instances of AdminApp. Start the
administrator instance first so that the server is listening on port `5000`.

## Connecting an Agent

The Agent attempts to discover the server on the local network and fills in the
SignalR URL when one is found. The address can also be entered manually:

```text
# Server and Agent on the same computer
http://localhost:5000/remoteHub

# Agent on another computer in the LAN
http://SERVER_IP:5000/remoteHub
```

Press `Connect` in the Agent. After a successful connection, the same button
changes to `Disconnect` and uses a light-red background. When the computer
appears in the administration list, select it and press `Connect` to start
viewing its screen.

The main administration shortcuts are:

- `F1`: lock or unlock the physical mouse on the Agent computer.
- `F5`: connect or disconnect the remote-control session.
- `F11`: enter or leave full-screen mode.

The `Unlock mouse` button is enabled only while the administrator is blocking
physical mouse input on the Agent computer. The local user can press it to
restore mouse control immediately. It does not disconnect the Agent or stop
screen streaming.

## Running the server in a browser

The server project can be started separately:

```powershell
dotnet run --project .\src\RemoteDesktop.Server --launch-profile http
```

Open either of these addresses:

```text
http://localhost:5000
http://SERVER_IP:5000
```

Do not run `RemoteDesktop.Server` separately while the administrator role in
`RemoteDesktop.AdminApp` is active. Both processes use port `5000`.

## Windows Firewall

The computer running AdminApp or Server must accept two ports on the Private
network profile:

- TCP `5000` for the web interface, SignalR, and remote-session data.
- UDP `50505` for LAN server discovery.

Open an elevated PowerShell window and run:

```powershell
New-NetFirewallRule -DisplayName "RemoteDesktop Server 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "RemoteDesktop Discovery 50505" -Direction Inbound -Protocol UDP -LocalPort 50505 -Action Allow -Profile Private
```

UDP port `50505` is not required when every Agent is configured with the server
address manually.

## Manual build and publish

Install the web dependency before publishing:

```powershell
npm ci --prefix .\src\RemoteDesktop.Server
dotnet publish .\src\RemoteDesktop.AdminApp\RemoteDesktop.AdminApp.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  --output .\publish
```

The `publish` directory is an input for the installer build. End users should
install `CNTT_Remote.exe` from GitHub Releases so that
Velopack can manage future updates.

## Publishing a new version

The project follows Semantic Versioning in the form `MAJOR.MINOR.PATCH`:

- Increase `PATCH` for compatible bug fixes.
- Increase `MINOR` for compatible new features.
- Increase `MAJOR` for incompatible changes.

The `.github/workflows/release.yml` workflow runs when a `vX.Y.Z` tag is pushed.
For example, to publish version `1.0.1`:

```powershell
git add .
git commit -m "Describe the change"
git push

git tag -a v1.0.1 -m "Release 1.0.1"
git push origin v1.0.1
```

GitHub Actions installs the dependencies, publishes the application, creates
the Velopack packages, and uploads the installer to GitHub Releases. Installed
copies check for a newer release at startup, download it, apply it, and restart
the application.

Published versions are available at:

https://github.com/Oncology-Hospital/Remote/releases

Updater errors are written to:

```text
%LocalAppData%\Oncology-Hospital\Remote\logs\updater.log
```

## Repository layout

```text
RemoteDesktop.sln
.github/
  workflows/
    release.yml
src/
  RemoteDesktop.AdminApp/
  RemoteDesktop.Agent/
  RemoteDesktop.Server/
```

## Current security limitations

The following items should be addressed before using the application outside a
test environment:

- The SignalR Hub does not require an Agent or administrator token.
- LAN connections use HTTP by default and do not use TLS or an internal
  certificate.
- Administrator passwords are stored as plain text.
- Remote sessions and administrator actions are not fully audited.
- There is no detailed permission model, device revocation, or user-side session
  approval flow.
- The installer is not code-signed, so Windows SmartScreen or antivirus software
  may display a warning.

Use the application only on a trusted internal network until these items have
been addressed.
