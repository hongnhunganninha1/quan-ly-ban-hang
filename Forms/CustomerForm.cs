using System.Text.RegularExpressions;

namespace QuanLyBanHang
{
    /// <summary>Quản lý khách hàng: thêm, sửa, xóa, lưu thông tin và lịch sử mua hàng.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class CustomerForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();
        private readonly TextBox txtSearch = Ui.Txt(260, "Tìm theo tên, số điện thoại, email...");

        public CustomerForm() : base("Quản lý khách hàng", 1100, 620)
        {
            var bar = Ui.Toolbar();
            var btnFind = Ui.Btn("🔍 Tìm", BtnKind.Secondary, 90);
            var btnAdd = Ui.Btn("+ Thêm", BtnKind.Primary, 100);
            var btnEdit = Ui.Btn("✎ Sửa", BtnKind.Primary, 100);
            var btnDel = Ui.Btn("Xóa", BtnKind.Danger, 90);
            var btnHist = Ui.Btn("Lịch sử mua hàng", BtnKind.Accent, 160);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Secondary, 110);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Tìm kiếm:"), txtSearch, btnFind, btnAdd, btnEdit, btnDel, btnHist, btnCsv });

            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("QUẢN LÝ KHÁCH HÀNG", "Thông tin và lịch sử mua hàng"), bar });

            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadData(); e.SuppressKeyPress = true; } };
            btnFind.Click += (s, e) => LoadData();
            btnAdd.Click += (s, e) => Edit(null);
            btnEdit.Click += (s, e) => { if (Ui.SelectedId(grid) is int id) Edit(id); else Ui.Warn("Chọn khách hàng cần sửa."); };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && Ui.SelectedId(grid) is int id) Edit(id); };
            btnDel.Click += (s, e) => Delete();
            btnHist.Click += (s, e) =>
            {
                if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn khách hàng để xem lịch sử."); return; }
                using var dlg = new CustomerHistoryDialog(id);
                dlg.ShowDialog(Ui.Owner(this));
            };
            btnCsv.Click += (s, e) => Ui.ExportCsv(grid, "DanhSachKhachHang");
            Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                string kw = txtSearch.Text.Trim();
                grid.DataSource = Database.Query(
                    @"SELECT c.Id, c.CustomerName AS `Tên khách hàng`, c.Phone AS `Số điện thoại`, c.Email AS `Email`,
                             c.Address AS `Địa chỉ`,
                             COUNT(o.Id) AS `Số đơn`, IFNULL(SUM(o.TotalAmount), 0) AS `Tổng chi tiêu`,
                             MAX(o.OrderDate) AS `Mua gần nhất`
                      FROM Customers c
                      LEFT JOIN Orders o ON o.CustomerId = c.Id AND o.Status = 'Paid'
                      WHERE c.IsActive = 1
                        AND (@kw = '' OR c.CustomerName LIKE @like OR c.Phone LIKE @like OR c.Email LIKE @like)
                      GROUP BY c.Id, c.CustomerName, c.Phone, c.Email, c.Address
                      ORDER BY c.CustomerName",
                    ("@kw", kw), ("@like", "%" + kw + "%"));
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void Edit(int? id)
        {
            using var dlg = new CustomerEditDialog(id);
            if (dlg.ShowDialog(Ui.Owner(this)) == DialogResult.OK) LoadData();
        }

        private void Delete()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn khách hàng cần xóa."); return; }
            if (!Ui.Confirm($"Xóa khách hàng \"{grid.CurrentRow!.Cells["Tên khách hàng"].Value}\"?")) return;
            try
            {
                int orders = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Orders WHERE CustomerId = @id", ("@id", id)));
                if (orders > 0)
                    // đã có hóa đơn: chỉ ẩn để giữ lịch sử; bỏ SĐT để có thể dùng lại số này
                    Database.Execute("UPDATE Customers SET IsActive = 0, Phone = NULL WHERE Id = @id", ("@id", id));
                else
                    Database.Execute("DELETE FROM Customers WHERE Id = @id", ("@id", id));
                LoadData();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }

    /// <summary>Thêm / sửa khách hàng. Dùng cả ở màn hình Lập hóa đơn (thêm nhanh).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class CustomerEditDialog : DialogBase
    {
        private readonly int? customerId;
        private readonly TextBox txtName = Ui.Txt();
        private readonly TextBox txtPhone = Ui.Txt();
        private readonly TextBox txtEmail = Ui.Txt();
        private readonly TextBox txtAddress = Ui.Txt();
        public int SavedId { get; private set; }

        public CustomerEditDialog(int? id, string? phone = null) : base(id == null ? "Thêm khách hàng" : "Sửa khách hàng")
        {
            customerId = id;
            txtAddress.Multiline = true;
            txtAddress.Height = 60;
            AddField("Tên khách hàng *", txtName);
            AddField("Số điện thoại", txtPhone);
            AddField("Email", txtEmail);
            AddField("Địa chỉ", txtAddress, 72);
            if (phone != null) txtPhone.Text = phone;
            if (id != null) Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                var dt = Database.Query("SELECT * FROM Customers WHERE Id = @id", ("@id", customerId));
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                txtName.Text = r["CustomerName"].ToString();
                txtPhone.Text = r["Phone"].ToString();
                txtEmail.Text = r["Email"].ToString();
                txtAddress.Text = r["Address"].ToString();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        protected override bool OnSave()
        {
            string name = txtName.Text.Trim(), phone = txtPhone.Text.Trim(), email = txtEmail.Text.Trim();
            if (name == "") { Ui.Warn("Vui lòng nhập tên khách hàng."); txtName.Focus(); return false; }
            if (phone != "" && !Regex.IsMatch(phone, @"^0\d{9,10}$")) { Ui.Warn("Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0)."); txtPhone.Focus(); return false; }
            if (email != "" && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) { Ui.Warn("Email không hợp lệ."); txtEmail.Focus(); return false; }
            if (phone != "")
            {
                int dup = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Customers WHERE Phone = @p AND Id <> @id",
                    ("@p", phone), ("@id", customerId ?? 0)));
                if (dup > 0) { Ui.Warn("Số điện thoại đã thuộc về khách hàng khác."); return false; }
            }
            object? phoneVal = phone == "" ? null : phone;
            if (customerId == null)
            {
                Database.Execute("INSERT INTO Customers (CustomerName, Phone, Email, Address) VALUES (@n, @p, @e, @a)",
                    ("@n", name), ("@p", phoneVal), ("@e", email), ("@a", txtAddress.Text.Trim()));
                SavedId = Database.ToInt(Database.Scalar("SELECT MAX(Id) FROM Customers"));
            }
            else
            {
                Database.Execute("UPDATE Customers SET CustomerName = @n, Phone = @p, Email = @e, Address = @a WHERE Id = @id",
                    ("@n", name), ("@p", phoneVal), ("@e", email), ("@a", txtAddress.Text.Trim()), ("@id", customerId));
                SavedId = customerId.Value;
            }
            return true;
        }
    }

    /// <summary>Lịch sử mua hàng của 1 khách hàng.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class CustomerHistoryDialog : AppForm
    {
        public CustomerHistoryDialog(int customerId) : base("Lịch sử mua hàng", 900, 520)
        {
            var grid = Ui.Grid();
            var lblSum = new Label { Height = 40, Font = Theme.Bold, ForeColor = Theme.Primary, Padding = new Padding(14, 10, 0, 0), BackColor = Color.White };
            string name = "";
            try
            {
                name = Database.Scalar("SELECT CustomerName FROM Customers WHERE Id = @id", ("@id", customerId))?.ToString() ?? "";
                var dt = Database.Query(
                    @"SELECT o.Id, o.OrderCode AS `Mã hóa đơn`, o.OrderDate AS `Ngày mua`,
                             (SELECT SUM(Quantity) FROM OrderItems WHERE OrderId = o.Id) AS `Số SP`,
                             o.TotalAmount AS `Tổng tiền`, o.PaymentMethod AS `Thanh toán`,
                             IF(o.Status = 'Paid', 'Đã thanh toán', 'Đã hủy') AS `Trạng thái`
                      FROM Orders o WHERE o.CustomerId = @id ORDER BY o.OrderDate DESC", ("@id", customerId));
                grid.DataSource = dt;
                decimal total = 0; int count = 0;
                foreach (System.Data.DataRow r in dt.Rows)
                    if (r["Trạng thái"].ToString() == "Đã thanh toán") { total += Database.ToDecimal(r["Tổng tiền"]); count++; }
                lblSum.Text = $"Tổng cộng: {count} đơn đã thanh toán  |  Tổng chi tiêu: {Fmt.Money(total)}";
            }
            catch (Exception ex) { Ui.DbError(ex); }

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || Ui.SelectedId(grid) is not int id) return;
                using var dlg = new OrderDetailDialog(id);
                dlg.ShowDialog(Ui.Owner(this));
            };
            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("LỊCH SỬ MUA HÀNG", name) }, new Control[] { lblSum });
            EndInit();
        }
    }
}
