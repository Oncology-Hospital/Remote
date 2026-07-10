# RemoteDesktop MVP

MVP nay gom 3 project, nhung ban gui/test chinh nen dung app tong hop `RemoteDesktop.AdminApp`:

- `RemoteDesktop.Server`: ASP.NET Core + SignalR, dong thoi serve trang admin tai `http://localhost:5000`.
- `RemoteDesktop.AdminApp`: app tong hop co login. Role `admin` se mo admin/server; role `user` se mo agent.
- `RemoteDesktop.Agent`: WinForms app chay tren may user, gui status/chat/screen frame va nhan lenh chuot/phim.

Tai khoan admin duoc luu cuc bo tai:

```txt
%LocalAppData%\Oncology-Hospital\Remote\accounts.json
```

Sao chep cau truc tu `src/RemoteDesktop.AdminApp/accounts.example.json` va thay
bang tai khoan rieng. File `accounts.json` that khong duoc commit len GitHub va
khong bi ghi de khi ung dung cap nhat.

## Chay kieu app admin

1. Mo `RemoteDesktop.sln`.
2. Right click solution, chon `Configure Startup Projects`.
3. Chon `Single startup project`.
4. Dat `RemoteDesktop.AdminApp`: `Start`.
5. Bam `F5`.
6. Chon role `admin` de dang nhap bang tai khoan da cau hinh, hoac chon role
   `user` de mo agent.
7. Neu agent test cung may thi giu URL:

   ```txt
   http://localhost:5000/remoteHub
   ```

   Neu agent chay tren may khac trong LAN, doi thanh:

   ```txt
   http://IP_MAY_SERVER:5000/remoteHub
   ```

8. Bam `Connect` tren agent.
9. Chon may trong sidebar cua AdminApp, bam `Connect` de xem man hinh.
10. Click/di chuot/go phim trong remote view de dieu khien may agent.
11. Bam `F1` khi remote view dang focus, hoac bam nut `Lock mouse (F1)`, de khoa/mo chuot vat ly cua may user.

## Chay kieu web cu

Neu van muon test bang trinh duyet:

1. Chay `RemoteDesktop.Server`.
2. Mo admin page:

   ```txt
   http://localhost:5000
   ```

   May khac trong LAN co the mo:

   ```txt
   http://IP_MAY_SERVER:5000
   ```

## Luu y khi test trong LAN

- Neu may khac khong vao duoc `http://IP_MAY_SERVER:5000`, mo Windows Firewall inbound rule cho port TCP `5000`.
- Agent co the tu tim server trong LAN qua UDP port `50505`. Neu tu dong khong thay server, mo them firewall inbound UDP `50505` tren may chay AdminApp/Server.
- Khi chay `RemoteDesktop.AdminApp`, khong chay dong thoi `RemoteDesktop.Server` tren port `5000`.
- Agent co nut `Unlock local mouse` de mo khoa chuot khi test.
- Day la MVP local, chua co dang nhap admin/token agent. Truoc khi dung that nen them auth, log remote session, va co che chap thuan/phu hop noi bo.

Firewall rules goi y tren may chay AdminApp/Server:

```powershell
New-NetFirewallRule -DisplayName "RemoteDesktop Server 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "RemoteDesktop Discovery 50505" -Direction Inbound -Protocol UDP -LocalPort 50505 -Action Allow -Profile Private
```

## Dong goi AdminApp thanh file chay

```powershell
dotnet publish .\src\RemoteDesktop.AdminApp\RemoteDesktop.AdminApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

File nam trong:

```txt
src\RemoteDesktop.AdminApp\bin\Release\net9.0-windows\win-x64\publish\
```

Hay gui ca thu muc `publish`, vi AdminApp can kem cac thu muc `wwwroot` va `node_modules` de hien giao dien admin.

## Neu SignalR JS bi thieu

Thu muc `src/RemoteDesktop.Server/node_modules` khong duoc commit. Sau khi clone,
cai lai bang:

```powershell
cd src\RemoteDesktop.Server
npm install
```

## Phat hanh va tu dong cap nhat

Ung dung dung Velopack va GitHub Releases tai:

```txt
https://github.com/Oncology-Hospital/Remote/releases
```

Version dung Semantic Versioning (`MAJOR.MINOR.PATCH`). Workflow
`.github/workflows/release.yml` tu dong build installer va update package khi
push tag:

```powershell
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

Nguoi dung cai lan dau bang file `OncologyHospital.Remote-win-Setup.exe` trong
GitHub Release. Cac lan sau, app kiem tra ban moi khi khoi dong, tu tai, cai dat
va mo lai. Khong phat hanh truc tiep thu muc `publish`, vi no khong co day du
thong tin cai dat de Velopack quan ly update.
