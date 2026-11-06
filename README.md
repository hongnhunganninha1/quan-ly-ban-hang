# Hệ thống quản lý bán hàng – Shop thời trang Twenty Five

Ứng dụng desktop (Windows) hỗ trợ cửa hàng thời trang quản lý toàn bộ hoạt động kinh doanh: từ danh mục, sản phẩm, khách hàng, nhập hàng – tồn kho, lập hóa đơn – thanh toán cho đến báo cáo thống kê doanh thu, lợi nhuận.

## Chức năng chính

### Người dùng & phân quyền
- Đăng nhập (hiện/ẩn mật khẩu), đăng xuất, đổi mật khẩu; mật khẩu được mã hóa SHA-256.
- Hai vai trò: **Quản lý (Admin)** và **Nhân viên bán hàng (Staff)**; menu và trang được hiển thị theo quyền.
- Quản lý tài khoản: thêm / sửa, khóa / mở khóa, đặt lại mật khẩu, phân chức vụ.

### Danh mục
- Quản lý danh mục sản phẩm (phân loại theo nhóm).
- Quản lý khách hàng kèm **lịch sử mua hàng**, tìm theo tên, số điện thoại, email.

### Sản phẩm
- Thêm / sửa / xóa sản phẩm: mã, tên, danh mục, đơn vị tính, giá nhập, giá bán, ảnh sản phẩm.
- Cảnh báo khi giá bán thấp hơn giá nhập (bán lỗ).
- Xóa mềm: sản phẩm bị ẩn nhưng lịch sử hóa đơn cũ vẫn được giữ.

### Nhập hàng – Tồn kho
- Nhập thêm hàng có ghi chú, lưu lịch sử nhập / xuất kho.
- Đặt ngưỡng tồn tối thiểu, lọc sản phẩm sắp hết / hết hàng.
- Điều chỉnh tồn kho khi kiểm kê (chỉ Quản lý).
- Thông báo sản phẩm sắp hết hàng ngay trên màn hình chính.

### Bán hàng
- Lập hóa đơn nhanh: tìm sản phẩm theo mã / tên, kiểm tra tồn kho khi thêm, chọn khách hàng hoặc khách lẻ, thêm khách mới ngay khi bán.
- Áp dụng giảm giá (%), giảm thêm (đ) và thuế VAT.
- Thanh toán bằng **tiền mặt** (tính tiền thừa trả khách), **thẻ ngân hàng** hoặc **chuyển khoản qua mã VietQR** tự điền sẵn số tiền và nội dung.
- Xem trước và **in hóa đơn** theo mẫu có thông tin cửa hàng.
- Phím tắt: **F2** lập hóa đơn mới, **F12** thanh toán, **Esc** hủy hóa đơn đang lập.

### Đơn hàng
- Tra cứu đơn theo mã hóa đơn, tên / SĐT khách, khoảng thời gian, trạng thái, hình thức thanh toán.
- Xem chi tiết, in lại hóa đơn.
- Hủy đơn và tự động hoàn lại số lượng vào kho.

### Báo cáo – Thống kê
- Màn hình tổng quan: doanh thu tháng (so với tháng trước), đơn hàng hôm nay, biểu đồ doanh thu & lợi nhuận 7 ngày, top sản phẩm bán chạy, đơn hàng gần đây.
- Báo cáo doanh thu – giá vốn – lợi nhuận gộp theo ngày / tháng / quý.
- Báo cáo tồn kho (giá trị tồn, sắp hết, hết hàng).
- Top khách hàng có tổng giá trị mua cao nhất.
- Biểu đồ trực quan, xuất dữ liệu ra file CSV (mở bằng Excel).

### Khác
- Giao diện tiếng Việt, định dạng tiền tệ và ngày tháng theo chuẩn Việt Nam.

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Ngôn ngữ / Nền tảng | C#, .NET 10 |
| Giao diện | Windows Forms |
| Cơ sở dữ liệu | MySQL |
| Kết nối CSDL | MySqlConnector |
| Thanh toán QR | VietQR (img.vietqr.io) |
| In hóa đơn | System.Drawing.Printing (PrintDocument, PrintPreviewDialog) |

## Cài đặt và chạy

1. Cài **MySQL** và **Visual Studio** (workload *.NET desktop development*, có .NET 10).
2. Chạy file `SQL/TAO_DATABASE_MOI.sql` trong MySQL để tạo database `shopthoitrang` và dữ liệu mẫu.
3. Mở `Database.cs`, sửa `Password=` cho đúng mật khẩu MySQL trên máy bạn.
4. Mở `QuanLyBanHang.slnx` bằng Visual Studio và nhấn **F5**.

### Tài khoản dùng thử

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản lý | `admin` | `123456` |
| Nhân viên | `staff` | `staff123` |
