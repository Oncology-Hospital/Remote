# Phát hành lên GitHub và mạng nội bộ

GitHub là nguồn lưu mã và tạo bản phát hành chính. Thư mục
`\\10.100.100.4\Website\App_IT\Remote` là bản sao các file phát hành để máy con
cài và cập nhật nhanh trong LAN; không nên chép source hoặc thư mục `publish`
lên đây.

Ứng dụng kiểm tra cả hai nguồn khi khởi động:

1. Thư mục mạng nội bộ (ưu tiên tốc độ LAN).
2. GitHub Releases (dự phòng khi máy ở ngoài LAN hoặc thư mục mạng lỗi).

Nếu hai nguồn có phiên bản khác nhau, ứng dụng chọn phiên bản mới hơn.

## Chuẩn bị một lần

- Git đã đăng nhập và có quyền push repository `Oncology-Hospital/Remote`.
- Tài khoản Windows phải đọc/ghi được thư mục mạng.
- Máy con phải có quyền đọc thư mục mạng. Nếu không có, cập nhật vẫn chạy qua
  GitHub.

## Phát hành phiên bản mới

Ví dụ phát hành `3.5.1`:

1. Đổi `<Version>` trong `Directory.Build.props` thành `3.5.1`.
2. Commit toàn bộ thay đổi cần phát hành.
3. Chạy:

```powershell
.\scripts\Publish-Release.ps1 -Version 3.5.1
```

Script yêu cầu working tree sạch, sau đó:

- push commit hiện tại và tag `v3.5.1` lên GitHub;
- chờ GitHub Actions tạo Velopack release;
- tải đúng các asset của release đó;
- chép installer, feed cập nhật và package vào thư mục mạng.

Máy mới chỉ cần chạy:

```text
\\10.100.100.4\Website\App_IT\Remote\CNTT_Remote.exe
```

Không đổi tên hoặc chỉ chép riêng file `.exe` của ứng dụng trong thư mục
`publish`: bản đó không có đầy đủ metadata để Velopack quản lý cập nhật.

## Khi GitHub release đã tồn tại nhưng chưa chép được sang LAN

Chạy lại cùng lệnh và cùng version. Script chấp nhận tag đang trỏ đúng commit,
tải lại release và thay các file trùng tên trên thư mục mạng. Các package phiên
bản cũ được giữ lại để không làm gián đoạn máy đang cập nhật.
