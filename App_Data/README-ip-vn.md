# ip-vn.csv.gz

Bảng tra dải IP Việt Nam → tỉnh/thành (34 đơn vị hành chính sau sáp nhập 2025), dùng cho màn hình
**Thống kê truy cập** (`IpViTriService`). Chỉ gồm các dải IP của Việt Nam; IP không thuộc bảng được ghi là "Nước ngoài".

- Nguồn: **DB-IP Lite City** — https://db-ip.com — giấy phép **CC BY 4.0** (bắt buộc ghi nguồn, đã ghi ở chân trang Thống kê).
- Phiên bản: 2026-11. DB-IP cập nhật hằng tháng; độ chính xác cấp tỉnh của IP di động (3G/4G/5G) thường thấp hơn IP cáp quang.
- Định dạng (sau khi giải nén): `phien_ban_ip,bat_dau(hex),ket_thuc(hex),tinh_thanh`, sắp xếp tăng dần.

Cập nhật: tải `dbip-city-lite-YYYY-MM.csv.gz`, lọc các dòng mã quốc gia `VN`, đổi tên tỉnh sang tiếng Việt, ghi lại đúng định dạng trên.
