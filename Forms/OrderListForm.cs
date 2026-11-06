using System.Data;

namespace QuanLyBanHang
{
    /// <summary>Quản lý đơn hàng: tra cứu, xem chi tiết, in lại, hủy đơn.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class OrderListForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();
        private readonly DateTimePicker dtFrom = Ui.Date();
        private readonly DateTimePicker dtTo = Ui.Date();
        private readonly TextBox txtSearch = Ui.Txt(220, "Mã HĐ, tên / SĐT khách...");
        private readonly ComboBox cboStatus = Ui.Combo(150);
        private readonly ComboBox cboPay = Ui.Combo(150);
        private readonly Label lblSum = new() { Height = 40, Font = Theme.Bold, ForeColor = Theme.Primary, Padding = new Padding(14, 10, 0, 0), BackColor = Color.White };

        public OrderListForm() : base("Quản lý đơn hàng", 1200, 660)
        {
            dtFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtTo.Value = DateTime.Today;
            cboStatus.Items.AddRange(new object[] { "Tất cả", "Đã thanh toán", "Đã hủy" });
            cboStatus.SelectedIndex = 0;
            cboPay.Items.AddRange(new object[] { "Mọi hình thức", "Tiền mặt", "Thẻ ngân hàng", "Chuyển khoản" });
            cboPay.SelectedIndex = 0;

            var bar = Ui.Toolbar();
            var btnFind = Ui.Btn("🔍 Lọc", BtnKind.Primary, 90);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Từ:"), dtFrom, Ui.Lbl("Đến:"), dtTo, txtSearch, cboStatus, cboPay, btnFind });
            var bar2 = Ui.Toolbar();
            var btnView = Ui.Btn("Xem chi tiết", BtnKind.Primary, 130);
            var btnPrint = Ui.Btn("🖨 In hóa đơn", BtnKind.Secondary, 130);
            var btnCancel = Ui.Btn("Hủy đơn", BtnKind.Danger, 110);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            bar2.Controls.AddRange(new Control[] { btnView, btnPrint, btnCancel, btnCsv });
            btnCancel.Visible = Session.IsAdmin;

            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("QUẢN LÝ ĐƠN HÀNG", "Lưu trữ thông tin chi tiết đơn hàng"), bar, bar2 }, new Control[] { lblSum });

            btnFind.Click += (s, e) => LoadData();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadData(); e.SuppressKeyPress = true; } };
            btnView.Click += (s, e) => ViewDetail();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ViewDetail(); };
            btnPrint.Click += (s, e) => { if (Ui.SelectedId(grid) is int id) InvoicePrinter.Preview(id, Ui.Owner(this)); else Ui.Warn("Chọn đơn hàng cần in."); };
            btnCancel.Click += (s, e) => CancelOrder();
            btnCsv.Click += (s, e) => Ui.ExportCsv(grid, "DanhSachDonHang");
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "Trạng thái" && e.Value?.ToString() == "Đã hủy")
                    e.CellStyle!.ForeColor = Theme.Danger;
            };
            Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            if (dtFrom.Value.Date > dtTo.Value.Date) { Ui.Warn("Ngày bắt đầu phải trước ngày kết thúc."); return; }
            try
            {
                string kw = txtSearch.Text.Trim();
                string status = cboStatus.SelectedIndex switch { 1 => "Paid", 2 => "Cancelled", _ => "" };
                string pay = cboPay.SelectedIndex == 0 ? "" : cboPay.Text;
                var dt = Database.Query(
                    @"SELECT o.Id, o.OrderCode AS `Mã hóa đơn`, o.OrderDate AS `Ngày lập`,
                             IFNULL(c.CustomerName, 'Khách lẻ') AS `Khách hàng`, c.Phone AS `SĐT`,
                             u.FullName AS `Nhân viên`, o.SubTotal AS `Tiền hàng`, o.DiscountAmount AS `Giảm giá`,
                             o.VatAmount AS `VAT`, o.TotalAmount AS `Tổng tiền`, o.PaymentMethod AS `Thanh toán`,
                             IF(o.Status = 'Paid', 'Đã thanh toán', 'Đã hủy') AS `Trạng thái`
                      FROM Orders o
                      LEFT JOIN Customers c ON c.Id = o.CustomerId
                      JOIN Users u ON u.Id = o.UserId
                      WHERE o.OrderDate >= @from AND o.OrderDate < @to
                        AND (@kw = '' OR o.OrderCode LIKE @like OR c.CustomerName LIKE @like OR c.Phone LIKE @like)
                        AND (@st = '' OR o.Status = @st)
                        AND (@pay = '' OR o.PaymentMethod = @pay)
                      ORDER BY o.OrderDate DESC",
                    ("@from", dtFrom.Value.Date), ("@to", dtTo.Value.Date.AddDays(1)),
                    ("@kw", kw), ("@like", "%" + kw + "%"), ("@st", status), ("@pay", pay));
                grid.DataSource = dt;

                int paid = 0; decimal sum = 0;
                foreach (DataRow r in dt.Rows)
                    if (r["Trạng thái"].ToString() == "Đã thanh toán") { paid++; sum += Database.ToDecimal(r["Tổng tiền"]); }
                lblSum.Text = $"Tổng: {dt.Rows.Count} đơn  |  Đã thanh toán: {paid} đơn  |  Tổng tiền thu: {Fmt.Money(sum)}";
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void ViewDetail()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn đơn hàng cần xem."); return; }
            using var dlg = new OrderDetailDialog(id);
            dlg.ShowDialog(Ui.Owner(this));
        }

        /// <summary>Hủy đơn: đổi trạng thái và cộng trả lại tồn kho.</summary>
        private void CancelOrder()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn đơn hàng cần hủy."); return; }
            if (grid.CurrentRow!.Cells["Trạng thái"].Value?.ToString() == "Đã hủy") { Ui.Warn("Đơn hàng này đã bị hủy trước đó."); return; }
            string code = grid.CurrentRow.Cells["Mã hóa đơn"].Value?.ToString() ?? "";
            if (!Ui.Confirm($"Hủy đơn hàng {code}?\nSố lượng sản phẩm sẽ được cộng trả lại vào kho.")) return;

            using var conn = Database.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                int ok = Database.Command(conn, tx, "UPDATE Orders SET Status = 'Cancelled' WHERE Id = @id AND Status = 'Paid'", ("@id", id)).ExecuteNonQuery();
                if (ok == 0) throw new InvalidOperationException("Đơn hàng không ở trạng thái Đã thanh toán.");
                Database.Command(conn, tx,
                    @"UPDATE Inventory i JOIN OrderItems oi ON oi.ProductId = i.ProductId
                      SET i.QuantityInStock = i.QuantityInStock + oi.Quantity, i.LastUpdated = NOW()
                      WHERE oi.OrderId = @id", ("@id", id)).ExecuteNonQuery();
                Database.Command(conn, tx,
                    @"INSERT INTO InventoryLogs (ProductId, ChangeQty, StockAfter, Note, UserId)
                      SELECT oi.ProductId, oi.Quantity, i.QuantityInStock, @note, @u
                      FROM OrderItems oi JOIN Inventory i ON i.ProductId = oi.ProductId WHERE oi.OrderId = @id",
                    ("@id", id), ("@note", "Hủy đơn - " + code), ("@u", Session.UserId)).ExecuteNonQuery();
                tx.Commit();
                Ui.Info("Đã hủy đơn hàng và hoàn lại tồn kho.");
                LoadData();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Ui.DbError(ex);
            }
        }
    }

    /// <summary>Chi tiết 1 đơn hàng.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class OrderDetailDialog : AppForm
    {
        public OrderDetailDialog(int orderId) : base("Chi tiết đơn hàng", 860, 560)
        {
            var info = new TableLayoutPanel { Height = 124, ColumnCount = 4, RowCount = 3, Padding = new Padding(14, 8, 14, 4), BackColor = Color.White };
            for (int i = 0; i < 4; i++) info.ColumnStyles.Add(new ColumnStyle(i % 2 == 0 ? SizeType.Absolute : SizeType.Percent, i % 2 == 0 ? 110 : 50));
            var grid = Ui.Grid();
            var totals = new Label { Height = 70, Font = Theme.Bold, ForeColor = Theme.Text, Padding = new Padding(14, 6, 14, 0), BackColor = Color.White, TextAlign = ContentAlignment.TopRight };
            var bar = new FlowLayoutPanel { Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8), BackColor = Theme.Back };
            var btnClose = Ui.Btn("Đóng", BtnKind.Secondary, 100);
            var btnPrint = Ui.Btn("🖨 In hóa đơn", BtnKind.Primary, 140);
            bar.Controls.AddRange(new Control[] { btnClose, btnPrint });
            btnClose.Click += (s, e) => Close();
            btnPrint.Click += (s, e) => InvoicePrinter.Preview(orderId, Ui.Owner(this));
            CancelButton = btnClose;

            string code = "";
            try
            {
                var h = Database.Query(
                    @"SELECT o.*, IFNULL(c.CustomerName, 'Khách lẻ') AS CustomerName, IFNULL(c.Phone, '-') AS Phone, u.FullName AS StaffName
                      FROM Orders o LEFT JOIN Customers c ON c.Id = o.CustomerId JOIN Users u ON u.Id = o.UserId WHERE o.Id = @id",
                    ("@id", orderId));
                if (h.Rows.Count > 0)
                {
                    var r = h.Rows[0];
                    code = r["OrderCode"].ToString()!;
                    void Add(string l, string v, int col, int row)
                    {
                        info.Controls.Add(Ui.Lbl(l, true), col, row);
                        info.Controls.Add(Ui.Lbl(v), col + 1, row);
                    }
                    Add("Mã hóa đơn:", code, 0, 0);
                    Add("Ngày lập:", Fmt.Date(Convert.ToDateTime(r["OrderDate"])), 2, 0);
                    Add("Khách hàng:", r["CustomerName"] + " (" + r["Phone"] + ")", 0, 1);
                    Add("Nhân viên:", r["StaffName"].ToString()!, 2, 1);
                    Add("Thanh toán:", r["PaymentMethod"].ToString()!, 0, 2);
                    Add("Trạng thái:", r["Status"].ToString() == "Paid" ? "Đã thanh toán" : "ĐÃ HỦY", 2, 2);
                    totals.Text =
                        $"Tiền hàng: {Fmt.Money(Convert.ToDecimal(r["SubTotal"]))}    Giảm giá: -{Fmt.Money(Convert.ToDecimal(r["DiscountAmount"]))}    " +
                        $"VAT: +{Fmt.Money(Convert.ToDecimal(r["VatAmount"]))}\n" +
                        $"TỔNG THANH TOÁN: {Fmt.Money(Convert.ToDecimal(r["TotalAmount"]))}";
                }
                grid.DataSource = Database.Query(
                    @"SELECT p.ProductCode AS `Mã SP`, p.Name AS `Tên sản phẩm`, oi.Quantity AS `Số lượng`,
                             oi.UnitPrice AS `Đơn giá`, oi.LineTotal AS `Thành tiền`
                      FROM OrderItems oi JOIN Products p ON p.Id = oi.ProductId WHERE oi.OrderId = @id ORDER BY oi.Id",
                    ("@id", orderId));
            }
            catch (Exception ex) { Ui.DbError(ex); }

            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("CHI TIẾT ĐƠN HÀNG", code, 50), info }, new Control[] { totals, bar });
            EndInit();
        }
    }
}
