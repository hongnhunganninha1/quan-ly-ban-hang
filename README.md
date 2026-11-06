# Hệ thống quản lý bán hàng – Shop thời trang

Ứng dụng Windows Forms (C#, .NET 10) quản lý bán hàng cho shop thời trang, dùng cơ sở dữ liệu MySQL.

## Chức năng
- Đăng nhập, quản lý người dùng
- Quản lý danh mục, sản phẩm, khách hàng
- Bán hàng, thanh toán, in hóa đơn
- Quản lý kho, danh sách đơn hàng
- Dashboard và báo cáo doanh thu

## Cấu trúc
| Thư mục / file | Nội dung |
|---|---|
| `Forms/` | Các màn hình (Login, Main, Sales, Product, Report...) |
| `Core/` | Giao diện dùng chung (Theme, Ui) |
| `Database.cs` | Kết nối và truy vấn MySQL |
| `SQL/` | Script tạo database `shopthoitrang` |

## Cách chạy
1. Cài **MySQL** và **Visual Studio** (có .NET 10, workload *.NET desktop development*).
2. Chạy file `SQL/TAO_DATABASE_MOI.sql` trong MySQL để tạo database `shopthoitrang`.
3. Mở `Database.cs`, sửa ` Password=YOUR_PASSWORD`.
4. Mở `QuanLyBanHang.slnx` bằng Visual Studio và nhấn **F5**.
