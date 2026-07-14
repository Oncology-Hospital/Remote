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
- Chọn chất lượng truyền màn hình theo mức Auto, 480p, 720p hoặc 1080p.
- Gửi thao tác chuột và bàn phím.
- Khóa hoặc mở khóa chuột vật lý trên máy Agent.
- Gửi yêu cầu hỗ trợ từ Agent đến quản trị viên.
- Giao diện tiếng Việt và tiếng Anh.
- Hiển thị phiên bản hiện tại trên màn hình chọn chế độ và tiêu đề cửa sổ.
- Tự kiểm tra phiên bản mới từ GitHub Releases, thông báo cho người dùng và
  hiển thị tiến trình tải, chuẩn bị, cài đặt.

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

Ứng dụng tự tạo tài khoản quản trị mặc định khi file tài khoản chưa tồn tại hoặc
đang chứa `[]`:

```text
Tài khoản: .\administrator
Mật khẩu:  cntt@it
```

Tài khoản được lưu tại:

```text
%LocalAppData%\Oncology-Hospital\Remote\accounts.json
```

Nội dung được tạo tự động có dạng:

```json
[
  {
    "username": ".\\administrator",
    "password": "cntt@it",
    "role": "admin"
  }
]
```

File tài khoản thật đã được thêm vào `.gitignore` và không nằm trong installer
hoặc gói cập nhật. Dữ liệu đặt trong `%LocalAppData%` cũng không bị ghi đè khi
nâng cấp ứng dụng.

Mật khẩu được lưu dưới dạng văn bản thuần và giống nhau trên mọi bản cài đặt.
Đây là tài khoản riêng của ứng dụng, không phải tài khoản Windows thật.

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

Nhấn `Kết nối` trên Agent. Khi kết nối thành công, chính nút này đổi thành
`Ngắt kết nối` và có màu đỏ nhạt. Sau khi máy xuất hiện trong danh sách quản
trị, chọn máy rồi nhấn `Kết nối` để bắt đầu xem màn hình.

Trên thanh điều khiển có thể chọn chất lượng hình ảnh:

- `Auto`: bắt đầu ở 720p, tự nâng lên 1080p khi kết nối ổn định và hạ xuống
  480p hoặc 720p khi thời gian truyền tăng cao. Mức đang dùng được hiển thị bên
  cạnh danh sách chọn.
- `480p`: ưu tiên đường truyền yếu và giảm lưu lượng mạng.
- `720p`: cân bằng giữa độ rõ và tốc độ phản hồi.
- `1080p`: ưu tiên độ rõ trên mạng LAN ổn định.

Lựa chọn được ghi nhớ trên máy quản trị và có thể thay đổi ngay trong lúc đang
xem màn hình, không cần ngắt kết nối.

Các phím tắt chính trong giao diện quản trị:

- `F1`: khóa hoặc mở khóa chuột vật lý trên máy Agent.
- `F5`: kết nối hoặc ngắt phiên điều khiển.
- `F11`: bật hoặc thoát chế độ toàn màn hình.

Nút `Mở khóa chuột` chỉ được bật khi quản trị viên đang khóa chuột vật lý của
máy Agent. Người dùng tại máy có thể nhấn nút này để lấy lại quyền điều khiển
chuột ngay lập tức; thao tác này không ngắt kết nối và không dừng truyền màn
hình.

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
cài file `CNTT_Remote.exe` từ GitHub Releases để Velopack
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
installer lên GitHub Releases. Bản đã cài sẽ kiểm tra release mới khi khởi động.
Khi phát hiện bản mới, ứng dụng hiển thị version hiện tại, version mới và cho
phép cập nhật ngay hoặc để sau. Quá trình tải có thanh tiến trình; ứng dụng báo
trước khi khởi động lại và xác nhận khi cập nhật thành công.

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
