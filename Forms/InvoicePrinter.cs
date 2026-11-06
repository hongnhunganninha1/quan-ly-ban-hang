using System.Data;
using System.Drawing.Printing;

namespace QuanLyBanHang
{
    /// <summary>Vẽ và in hóa đơn (xem trước trước khi in). Không cần thư viện ngoài.</summary>
    internal static class InvoicePrinter
    {
        private const string ShopName = ShopInfo.Name;
        private const string ShopAddress = "Địa chỉ: " + ShopInfo.Address;
        private const string ShopPhone = "Hotline: " + ShopInfo.Phone;

        public static void Preview(int orderId, IWin32Window? owner = null)
        {
            DataRow header;
            DataTable items;
            try
            {
                var h = Database.Query(
                    @"SELECT o.*, IFNULL(c.CustomerName, 'Khách lẻ') AS CustomerName, c.Phone, u.FullName AS StaffName
                      FROM Orders o LEFT JOIN Customers c ON c.Id = o.CustomerId JOIN Users u ON u.Id = o.UserId
                      WHERE o.Id = @id", ("@id", orderId));
                if (h.Rows.Count == 0) { Ui.Warn("Không tìm thấy hóa đơn."); return; }
                header = h.Rows[0];
                items = Database.Query(
                    @"SELECT p.ProductCode, p.Name, oi.Quantity, oi.UnitPrice, oi.LineTotal
                      FROM OrderItems oi JOIN Products p ON p.Id = oi.ProductId WHERE oi.OrderId = @id ORDER BY oi.Id",
                    ("@id", orderId));
            }
            catch (Exception ex) { Ui.DbError(ex); return; }

            int rowIndex = 0;
            using var doc = new PrintDocument { DocumentName = "HoaDon_" + header["OrderCode"] };
            doc.BeginPrint += (s, e) => rowIndex = 0;
            doc.PrintPage += (s, e) => rowIndex = DrawPage(e, header, items, rowIndex);
            doc.EndPrint += (s, e) =>
            {
                if (e.PrintAction == PrintAction.PrintToPrinter)
                    try { Database.Execute("UPDATE Orders SET PrintedAt = NOW() WHERE Id = @id", ("@id", orderId)); } catch { }
            };

            using var dlg = new PrintPreviewDialog
            {
                Document = doc,
                Width = 900,
                Height = 900,
                UseAntiAlias = true,
                Text = "Xem trước hóa đơn " + header["OrderCode"],
                StartPosition = FormStartPosition.CenterScreen,
            };
            dlg.ShowDialog(owner);
        }

        /// <summary>Vẽ 1 trang, trả về chỉ số dòng sản phẩm tiếp theo (để in sang trang sau nếu dài).</summary>
        private static int DrawPage(PrintPageEventArgs e, DataRow h, DataTable items, int start)
        {
            var g = e.Graphics!;
            var m = e.MarginBounds;
            float x = m.Left, y = m.Top, w = m.Width;
            using var fShop = new Font("Georgia", 20, FontStyle.Bold);
            using var fTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            using var fNormal = new Font("Segoe UI", 10);
            using var fBold = new Font("Segoe UI", 10, FontStyle.Bold);
            using var fTotal = new Font("Segoe UI", 14, FontStyle.Bold);
            using var brand = new SolidBrush(Theme.Primary);
            using var pen = new Pen(Theme.Primary, 1.5f);
            var center = new StringFormat { Alignment = StringAlignment.Center };
            var right = new StringFormat { Alignment = StringAlignment.Far };

            if (start == 0)
            {
                // Tiêu đề cửa hàng
                g.DrawString(ShopName, fShop, brand, new RectangleF(x, y, w, 36), center); y += 36;
                g.DrawString(ShopAddress + "   |   " + ShopPhone, fNormal, Brushes.DimGray, new RectangleF(x, y, w, 20), center); y += 30;
                g.DrawLine(pen, x, y, x + w, y); y += 12;
                g.DrawString("HÓA ĐƠN BÁN HÀNG", fTitle, brand, new RectangleF(x, y, w, 30), center); y += 40;

                // Thông tin hóa đơn (2 cột)
                float half = w / 2;
                void Info(string l1, string v1, string l2, string v2)
                {
                    g.DrawString(l1, fBold, Brushes.Black, x, y); g.DrawString(v1, fNormal, Brushes.Black, x + 110, y);
                    g.DrawString(l2, fBold, Brushes.Black, x + half, y); g.DrawString(v2, fNormal, Brushes.Black, x + half + 110, y);
                    y += 22;
                }
                var date = Convert.ToDateTime(h["OrderDate"]);
                Info("Mã hóa đơn:", h["OrderCode"].ToString()!, "Ngày lập:", Fmt.Date(date));
                Info("Khách hàng:", h["CustomerName"].ToString()!, "Nhân viên:", h["StaffName"].ToString()!);
                Info("Điện thoại:", h["Phone"] == DBNull.Value ? "-" : h["Phone"].ToString()!, "Thanh toán:", h["PaymentMethod"].ToString()!);
                if (h["Status"].ToString() == "Cancelled")
                {
                    g.DrawString("(HÓA ĐƠN ĐÃ HỦY)", fBold, Brushes.Red, x, y); y += 22;
                }
                y += 10;
            }

            // Bảng sản phẩm
            float[] cols = { 0.06f, 0.14f, 0.38f, 0.10f, 0.15f, 0.17f };
            string[] heads = { "STT", "Mã SP", "Tên sản phẩm", "SL", "Đơn giá", "Thành tiền" };
            float cx = x;
            g.FillRectangle(brand, x, y, w, 26);
            for (int i = 0; i < heads.Length; i++)
            {
                float cw = cols[i] * w;
                g.DrawString(heads[i], fBold, Brushes.White, new RectangleF(cx + 4, y + 4, cw - 8, 20), i >= 3 ? right : StringFormat.GenericDefault);
                cx += cw;
            }
            y += 30;

            int idx = start;
            for (; idx < items.Rows.Count; idx++)
            {
                if (y > m.Bottom - 40) { e.HasMorePages = true; return idx; }
                var r = items.Rows[idx];
                string[] vals =
                {
                    (idx + 1).ToString(), r["ProductCode"].ToString()!, r["Name"].ToString()!,
                    r["Quantity"].ToString()!, Fmt.Num(Convert.ToDecimal(r["UnitPrice"])), Fmt.Num(Convert.ToDecimal(r["LineTotal"])),
                };
                cx = x;
                for (int i = 0; i < vals.Length; i++)
                {
                    float cw = cols[i] * w;
                    g.DrawString(vals[i], fNormal, Brushes.Black, new RectangleF(cx + 4, y, cw - 8, 20), i >= 3 ? right : StringFormat.GenericDefault);
                    cx += cw;
                }
                y += 22;
                g.DrawLine(Pens.Gainsboro, x, y - 2, x + w, y - 2);
            }

            // Tổng tiền (cần ~170px, nếu không đủ thì sang trang mới)
            if (y > m.Bottom - 170) { e.HasMorePages = true; return idx; }
            y += 10;
            float lx = x + w * 0.5f, lw = w * 0.5f;
            void Sum(string label, string value, Font f, Brush b)
            {
                g.DrawString(label, f, b, lx, y);
                g.DrawString(value, f, b, new RectangleF(lx, y, lw - 4, 26), right);
                y += f.Height + 6;
            }
            Sum("Tổng tiền hàng:", Fmt.Money(Convert.ToDecimal(h["SubTotal"])), fNormal, Brushes.Black);
            Sum($"Giảm giá ({Convert.ToDecimal(h["DiscountPercent"]):0.#}% + VNĐ):", "- " + Fmt.Money(Convert.ToDecimal(h["DiscountAmount"])), fNormal, Brushes.Black);
            Sum($"Thuế VAT ({Convert.ToDecimal(h["VatPercent"]):0.#}%):", "+ " + Fmt.Money(Convert.ToDecimal(h["VatAmount"])), fNormal, Brushes.Black);
            g.DrawLine(pen, lx, y, x + w, y); y += 6;
            Sum("TỔNG THANH TOÁN:", Fmt.Money(Convert.ToDecimal(h["TotalAmount"])), fTotal, brand);

            y += 20;
            g.DrawString("Cảm ơn quý khách đã mua hàng tại Twenty Five!", fBold, brand, new RectangleF(x, y, w, 22), center); y += 22;
            g.DrawString("Hàng đã mua được đổi trong vòng 7 ngày kèm hóa đơn.", fNormal, Brushes.DimGray, new RectangleF(x, y, w, 20), center);
            e.HasMorePages = false;
            return idx;
        }
    }
}
