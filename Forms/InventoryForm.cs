namespace QuanLyBanHang
{
    /// <summary>Nhập hàng - Cập nhật tồn kho, có cảnh báo sản phẩm dưới ngưỡng tồn tối thiểu.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class InventoryForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();
        private readonly DataGridView gridLog = Ui.Grid();
        private readonly TextBox txtSearch = Ui.Txt(240, "Tìm mã / tên sản phẩm...");
        private readonly CheckBox chkLow = new() { Text = "Chỉ hiện SP tồn thấp", AutoSize = true, Margin = new Padding(8, 10, 8, 4) };
        private readonly Label lblSelected = Ui.Lbl("Chưa chọn sản phẩm", true, Theme.Primary);
        private readonly NumericUpDown numQty = Ui.Num(100_000, 100);
        private readonly NumericUpDown numMin = Ui.Num(100_000, 90);
        private readonly TextBox txtNote = Ui.Txt(220, "Ghi chú (VD: nhập từ xưởng...)");

        public InventoryForm(bool lowOnly = false) : base("Nhập hàng - Tồn kho", 1200, 700)
        {
            chkLow.Checked = lowOnly;
            numQty.Minimum = 0; numQty.Value = 1;

            var bar = Ui.Toolbar();
            var btnFind = Ui.Btn("🔍 Tìm", BtnKind.Secondary, 90);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Tìm kiếm:"), txtSearch, chkLow, btnFind, btnCsv });

            var input = Ui.Toolbar();
            input.BackColor = Color.White;
            var btnImport = Ui.Btn("+ Nhập thêm", BtnKind.Success, 120, 34);
            var btnSet = Ui.Btn("Điều chỉnh tồn = SL", BtnKind.Secondary, 170, 34);
            var btnMin = Ui.Btn("Lưu ngưỡng", BtnKind.Secondary, 110, 34);
            lblSelected.MinimumSize = new Size(230, 0);
            input.Controls.AddRange(new Control[] { Ui.Lbl("Sản phẩm:"), lblSelected, Ui.Lbl("Số lượng:"), numQty, txtNote, btnImport, btnSet,
                Ui.Lbl("  Tồn tối thiểu:"), numMin, btnMin });

            var gridWrap = Ui.Card();
            gridWrap.Controls.Add(grid);

            var logPanel = new Panel { Height = 210, Padding = new Padding(12, 0, 12, 10), BackColor = Theme.Back };
            var logWrap = new Panel { Dock = DockStyle.Fill };
            logWrap.Controls.Add(gridLog);
            logPanel.Controls.Add(logWrap);
            logPanel.Controls.Add(Ui.SectionTitle("Lịch sử nhập / xuất kho gần đây"));

            Ui.Stack(this, gridWrap,
                new Control[] { Ui.Header("📦 NHẬP HÀNG - TỒN KHO", "Cập nhật số lượng, cảnh báo tồn thấp"), bar, input },
                new Control[] { logPanel });

            btnFind.Click += (s, e) => LoadData();
            chkLow.CheckedChanged += (s, e) => LoadData();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadData(); e.SuppressKeyPress = true; } };
            btnCsv.Click += (s, e) => Ui.ExportCsv(grid, "TonKho");
            grid.SelectionChanged += (s, e) => OnSelect();
            btnImport.Click += (s, e) => ChangeStock(false);
            btnSet.Click += (s, e) => ChangeStock(true);
            btnMin.Click += (s, e) => SaveMin();
            btnSet.Visible = Session.IsAdmin;  // chỉ quản lý được điều chỉnh tồn tuyệt đối (kiểm kê)
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var st = grid.Rows[e.RowIndex].Cells["Trạng thái"].Value?.ToString();
                if (st == "Hết hàng") { e.CellStyle!.BackColor = Theme.DangerLight; e.CellStyle.ForeColor = Theme.Danger; }
                else if (st == "Sắp hết") { e.CellStyle!.BackColor = Color.FromArgb(255, 244, 225); e.CellStyle.ForeColor = Theme.Warning; }
            };
            Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                string kw = txtSearch.Text.Trim();
                grid.DataSource = Database.Query(
                    @"SELECT p.Id, p.ProductCode AS `Mã SP`, p.Name AS `Tên sản phẩm`, c.CategoryName AS `Danh mục`,
                             p.CostPrice AS `Giá nhập`, p.UnitPrice AS `Giá bán`, p.Unit AS `Đơn vị`,
                             i.QuantityInStock AS `Tồn kho`, i.MinStock AS `Tồn tối thiểu`,
                             CASE WHEN i.QuantityInStock = 0 THEN 'Hết hàng'
                                  WHEN i.QuantityInStock <= i.MinStock THEN 'Sắp hết'
                                  ELSE 'Đủ hàng' END AS `Trạng thái`,
                             i.LastUpdated AS `Cập nhật`
                      FROM Products p
                      JOIN Inventory i ON i.ProductId = p.Id
                      JOIN Categories c ON c.Id = p.CategoryId
                      WHERE p.IsActive = 1
                        AND (@kw = '' OR p.ProductCode LIKE @like OR p.Name LIKE @like)
                        AND (@low = 0 OR i.QuantityInStock <= i.MinStock)
                      ORDER BY (i.QuantityInStock <= i.MinStock) DESC, p.ProductCode",
                    ("@kw", kw), ("@like", "%" + kw + "%"), ("@low", chkLow.Checked ? 1 : 0));

                gridLog.DataSource = Database.Query(
                    @"SELECT l.CreatedAt AS `Thời gian`, p.ProductCode AS `Mã SP`, p.Name AS `Tên sản phẩm`,
                             l.ChangeQty AS `Thay đổi`, l.StockAfter AS `Tồn sau`, l.Note AS `Ghi chú`, u.FullName AS `Người thực hiện`
                      FROM InventoryLogs l
                      JOIN Products p ON p.Id = l.ProductId
                      LEFT JOIN Users u ON u.Id = l.UserId
                      ORDER BY l.Id DESC LIMIT 100");
                OnSelect();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void OnSelect()
        {
            if (grid.CurrentRow == null) { lblSelected.Text = "Chưa chọn sản phẩm"; return; }
            var r = grid.CurrentRow;
            lblSelected.Text = $"{r.Cells["Mã SP"].Value} - {r.Cells["Tên sản phẩm"].Value} (tồn {r.Cells["Tồn kho"].Value})";
            numMin.Value = Math.Min(numMin.Maximum, Database.ToInt(r.Cells["Tồn tối thiểu"].Value));
        }

        /// <summary>setAbsolute = false: cộng thêm SL (nhập hàng). true: đặt tồn = SL (kiểm kê).</summary>
        private void ChangeStock(bool setAbsolute)
        {
            if (Ui.SelectedId(grid) is not int pid) { Ui.Warn("Chọn sản phẩm trong bảng trước."); return; }
            int qty = (int)numQty.Value;
            int current = Database.ToInt(grid.CurrentRow!.Cells["Tồn kho"].Value);
            if (!setAbsolute && qty <= 0) { Ui.Warn("Số lượng nhập phải lớn hơn 0."); return; }
            int change = setAbsolute ? qty - current : qty;
            if (setAbsolute && change == 0) { Ui.Info("Tồn kho không thay đổi."); return; }

            string name = grid.CurrentRow.Cells["Tên sản phẩm"].Value?.ToString() ?? "";
            string msg = setAbsolute
                ? $"Điều chỉnh tồn kho \"{name}\" từ {current} thành {qty}?"
                : $"Nhập thêm {qty} sản phẩm \"{name}\"?\nTồn kho sau nhập: {current + qty}";
            if (!Ui.Confirm(msg)) return;

            string note = txtNote.Text.Trim();
            if (note == "") note = setAbsolute ? "Điều chỉnh kiểm kê" : "Nhập hàng";
            using var conn = Database.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                Database.Command(conn, tx,
                    "UPDATE Inventory SET QuantityInStock = QuantityInStock + @c, LastUpdated = NOW() WHERE ProductId = @p",
                    ("@c", change), ("@p", pid)).ExecuteNonQuery();
                Database.Command(conn, tx,
                    @"INSERT INTO InventoryLogs (ProductId, ChangeQty, StockAfter, Note, UserId)
                      SELECT @p, @c, QuantityInStock, @n, @u FROM Inventory WHERE ProductId = @p",
                    ("@p", pid), ("@c", change), ("@n", note), ("@u", Session.UserId)).ExecuteNonQuery();
                tx.Commit();
                txtNote.Clear();
                numQty.Value = 1;
                int keep = grid.CurrentRow.Index;
                LoadData();
                if (keep < grid.Rows.Count) grid.CurrentCell = grid.Rows[keep].Cells["Mã SP"];
                Ui.Info("Đã cập nhật tồn kho.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Ui.DbError(ex);
            }
        }

        private void SaveMin()
        {
            if (Ui.SelectedId(grid) is not int pid) { Ui.Warn("Chọn sản phẩm trong bảng trước."); return; }
            try
            {
                Database.Execute("UPDATE Inventory SET MinStock = @m WHERE ProductId = @p", ("@m", (int)numMin.Value), ("@p", pid));
                LoadData();
                Ui.Info("Đã lưu ngưỡng tồn tối thiểu.");
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }
}
