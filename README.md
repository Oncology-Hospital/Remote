# Oncology Hospital Remote

[Tiếng Việt](README.md) | [English](README.en.md)

Oncology Hospital Remote là ứng dụng điều khiển máy tính từ xa trong mạng nội
bộ. Dự án được viết bằng .NET 9, WinForms, ASP.NET Core và SignalR. Giao diện
quản trị có thể chạy trong ứng dụng Windows hoặc mở trực tiếp bằng trình duyệt.

Dự án hiện phù hợp để thử nghiệm và sử dụng trong mạng LAN được kiểm soát. Phần
máy chủ chưa xác thực kết nối SignalR của Agent và chưa mã hóa lưu lượng HTTP,
do đó không nên mở cổng trực tiếp ra Internet.

## Thành phần

Solution gồm ba project:

- `RemoteDesktop.AdminApp`: ứng dụng chính được đóng gói để phát hành. Người
  dùng có thể chọn vai trò quản trị viên hoặc máy cần hỗ trợ ngay trên màn hình
  đầu tiên. Khi vào vai trò quản trị viên, ứng dụng khởi chạy máy chủ và hiển
  thị giao diện quản trị bằng WebView2.
- `RemoteDesktop.Agent`: chạy trên máy cần hỗ trợ, gửi thông tin máy, hình ảnh
  màn hình và nhận thao tác chuột, bàn phím từ quản trị viên.
- `RemoteDesktop.Server`: máy chủ ASP.NET Core và SignalR, đồng thời cung cấp
  giao diện quản trị tại `http://localhost:5000`.

`RemoteDesktop.AdminApp` tham chiếu hai project còn lại, vì vậy đây là project
nên dùng khi cài đặt hoặc gửi ứng dụng cho người dùng.

## Chức năng hiện có

- Tìm máy chủ tự động trong mạng LAN qua UDP.
- Hiển thị danh sách máy đang kết nối.
- Xem màn hình máy từ xa.
- Gửi thao tác chuột và bàn phím.
- Khóa hoặc mở khóa chuột vật lý trên máy Agent.
- Gửi yêu cầu hỗ trợ từ Agent đến quản trị viên.
- Giao diện tiếng Việt và tiếng Anh.
- Tự kiểm tra, tải và cài đặt phiên bản mới từ GitHub Releases.

## Yêu cầu

Môi trường phát triển cần có:

- Windows 10 hoặc Windows 11 bản 64-bit.
- .NET 9 SDK.
- Node.js và npm.
- Visual Studio 2022 nếu chạy và gỡ lỗi bằng IDE.
- Microsoft Edge WebView2 Runtime để hiển thị giao diện quản trị trong
  `RemoteDesktop.AdminApp`.

Bản cài đặt được phát hành ở chế độ self-contained nên máy người dùng không cần
cài riêng .NET Runtime. WebView2 Runtime vẫn phải có trên máy.

## Chuẩn bị mã nguồn

Clone repository và cài các gói JavaScript:

```powershell
git clone https://github.com/Oncology-Hospital/Remote.git
cd Remote
npm ci --prefix .\src\RemoteDesktop.Server
dotnet restore .\RemoteDesktop.sln
```

Thư mục `node_modules` không được lưu trong Git. Lệnh `npm ci` sử dụng đúng các
phiên bản đã ghi trong `package-lock.json`.

## Cấu hình tài khoản quản trị

Tài khoản quản trị được đọc từ file:

```text
%LocalAppData%\Oncology-Hospital\Remote\accounts.json
```

Nếu file chưa tồn tại, ứng dụng sẽ tạo một danh sách trống. Có thể sao chép cấu
trúc từ `src/RemoteDesktop.AdminApp/accounts.example.json`:

```json
[
  {
    "username": "your-admin-account",
    "password": "change-this-password",
    "role": "admin"
  }
]
```

File tài khoản thật đã được thêm vào `.gitignore` và không nằm trong installer
hoặc gói cập nhật. Dữ liệu đặt trong `%LocalAppData%` cũng không bị ghi đè khi
nâng cấp ứng dụng.

Mật khẩu hiện vẫn được lưu dưới dạng văn bản thuần. Chỉ nên dùng tài khoản dành
riêng cho ứng dụng, không dùng lại mật khẩu Windows, email hoặc các hệ thống
khác.

## Chạy bằng Visual Studio

1. Mở `RemoteDesktop.sln`.
2. Chọn `Configure Startup Projects` trong menu chuột phải của solution.
3. Chọn `Single startup project`.
4. Đặt `RemoteDesktop.AdminApp` thành project khởi động.
5. Nhấn `F5`.
6. Chọn `Quản trị` để mở máy chủ và giao diện quản trị, hoặc chọn `Máy người
   dùng` để mở Agent.

Khi thử cả hai vai trò trên cùng một máy, mở hai instance của AdminApp. Instance
quản trị phải được chạy trước để máy chủ lắng nghe tại cổng `5000`.

## Kết nối Agent

Agent sẽ thử tìm máy chủ trong mạng LAN. Nếu tìm thấy, địa chỉ SignalR được điền
tự động. Có thể nhập thủ công trong các trường hợp sau:

```text
# Máy chủ và Agent cùng một máy
http://localhost:5000/remoteHub

# Agent chạy trên máy khác trong LAN
http://IP_MAY_CHU:5000/remoteHub
```

Nhấn `Kết nối` trên Agent. Sau khi máy xuất hiện trong danh sách quản trị, chọn
máy rồi nhấn `Kết nối` để bắt đầu xem màn hình.

Các phím tắt chính trong giao diện quản trị:

- `F1`: khóa hoặc mở khóa chuột vật lý trên máy Agent.
- `F5`: kết nối hoặc ngắt phiên điều khiển.
- `F11`: bật hoặc thoát chế độ toàn màn hình.

Agent luôn có nút `Mở khóa chuột` để người dùng tại máy đó tự khôi phục chuột
khi cần.

## Chạy máy chủ bằng trình duyệt

Có thể chạy riêng project máy chủ:

```powershell
dotnet run --project .\src\RemoteDesktop.Server --launch-profile http
```

Sau đó mở một trong các địa chỉ:

```text
http://localhost:5000
http://IP_MAY_CHU:5000
```

Không chạy `RemoteDesktop.Server` riêng trong khi vai trò quản trị của
`RemoteDesktop.AdminApp` đang hoạt động, vì cả hai cùng sử dụng cổng `5000`.

## Cấu hình Windows Firewall

Máy chạy AdminApp hoặc Server cần cho phép hai cổng trong mạng Private:

- TCP `5000`: web, SignalR và dữ liệu phiên điều khiển.
- UDP `50505`: thông báo tìm máy chủ trong LAN.

Mở PowerShell với quyền quản trị và chạy:

```powershell
New-NetFirewallRule -DisplayName "RemoteDesktop Server 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "RemoteDesktop Discovery 50505" -Direction Inbound -Protocol UDP -LocalPort 50505 -Action Allow -Profile Private
```

Không cần mở UDP `50505` nếu mọi Agent đều được cấu hình địa chỉ máy chủ thủ
công.

## Build và publish thủ công

Trước khi publish, cài dependency của giao diện web:

```powershell
npm ci --prefix .\src\RemoteDesktop.Server
dotnet publish .\src\RemoteDesktop.AdminApp\RemoteDesktop.AdminApp.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  --output .\publish
```

Thư mục `publish` chỉ dùng làm đầu vào để tạo installer. Người dùng cuối nên
cài file `OncologyHospital.Remote-win-Setup.exe` từ GitHub Releases để Velopack
có thể quản lý các lần cập nhật tiếp theo.

## Phát hành phiên bản mới

Dự án dùng Semantic Versioning theo dạng `MAJOR.MINOR.PATCH`:

- Tăng `PATCH` khi sửa lỗi và không thay đổi cách sử dụng hiện tại.
- Tăng `MINOR` khi bổ sung chức năng vẫn tương thích với phiên bản cũ.
- Tăng `MAJOR` khi có thay đổi không tương thích.

Workflow `.github/workflows/release.yml` chạy khi repository nhận tag dạng
`vX.Y.Z`. Ví dụ phát hành phiên bản `1.0.1`:

```powershell
git add .
git commit -m "Mô tả thay đổi"
git push

git tag -a v1.0.1 -m "Release 1.0.1"
git push origin v1.0.1
```

GitHub Actions sẽ cài dependency, publish ứng dụng, tạo gói Velopack và đăng
installer lên GitHub Releases. Bản đã cài sẽ kiểm tra release mới khi khởi động,
tải gói cập nhật, cài đặt rồi mở lại ứng dụng.

Danh sách phiên bản đã phát hành:

https://github.com/Oncology-Hospital/Remote/releases

Log lỗi của updater được lưu tại:

```text
%LocalAppData%\Oncology-Hospital\Remote\logs\updater.log
```

## Cấu trúc repository

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

## Giới hạn bảo mật hiện tại

Trước khi triển khai ngoài môi trường thử nghiệm, cần xử lý các điểm sau:

- SignalR Hub chưa yêu cầu token cho Agent và quản trị viên.
- Kết nối LAN mặc định dùng HTTP, chưa dùng HTTPS hoặc chứng chỉ nội bộ.
- Mật khẩu quản trị được lưu dưới dạng văn bản thuần.
- Chưa có nhật ký đầy đủ cho từng phiên điều khiển và từng thao tác quản trị.
- Chưa có cơ chế phân quyền chi tiết, thu hồi thiết bị hoặc xác nhận phiên từ
  phía người dùng.
- Installer chưa được ký số nên Windows SmartScreen hoặc phần mềm chống virus
  có thể hiển thị cảnh báo.

Chỉ nên sử dụng ứng dụng trong mạng nội bộ tin cậy cho đến khi các mục trên được
hoàn thiện.
