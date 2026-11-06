namespace QuanLyBanHang
{
    /// <summary>Quản lý danh mục sản phẩm: phân loại sản phẩm theo nhóm, ngành hàng.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class CategoryForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();
        private readonly TextBox txtName = Ui.Txt(260);
        private readonly TextBox txtDesc = Ui.Txt(260);
        private readonly Label lblMode = Ui.Lbl("Đang thêm danh mục mới", false, Theme.Muted);
        private int? editingId;

        public CategoryForm() : base("Danh mục sản phẩm", 900, 560)
        {
            txtDesc.Multiline = true;
            txtDesc.Height = 90;

            // Khung nhập bên phải
            var side = new Panel { Width = 330, BackColor = Color.White };
            var title = Ui.Lbl("THÔNG TIN DANH MỤC", true, Theme.Primary); title.Location = new Point(18, 14);
            lblMode.Location = new Point(18, 40);
            var l1 = Ui.Lbl("Tên danh mục *"); l1.Location = new Point(18, 74);
            txtName.Location = new Point(18, 98); txtName.Width = 290;
            var l2 = Ui.Lbl("Mô tả"); l2.Location = new Point(18, 136);
            txtDesc.Location = new Point(18, 160); txtDesc.Width = 290;
            var btnNew = Ui.Btn("+ Thêm mới", BtnKind.Secondary, 140); btnNew.Location = new Point(18, 266);
            var btnSave = Ui.Btn("💾 Lưu", BtnKind.Primary, 140); btnSave.Location = new Point(168, 266);
            var btnDel = Ui.Btn("Xóa", BtnKind.Danger, 140); btnDel.Location = new Point(18, 310);
            var btnClose = Ui.Btn("Đóng", BtnKind.Secondary, 140); btnClose.Location = new Point(168, 310);
            side.Controls.AddRange(new Control[] { title, lblMode, l1, txtName, l2, txtDesc, btnNew, btnSave, btnDel, btnClose });

            btnNew.Click += (s, e) => NewMode();
            btnSave.Click += (s, e) => Save();
            btnDel.Click += (s, e) => Delete();
            btnClose.Click += (s, e) => Close();
            grid.SelectionChanged += (s, e) => FillFromGrid();

            var gridWrap = Ui.Card();
            gridWrap.Controls.Add(grid);
            side.Dock = DockStyle.Right;
            Controls.Add(gridWrap);
            gridWrap.Dock = DockStyle.Fill;
            Controls.Add(side);
            Controls.Add(Ui.Header("DANH MỤC SẢN PHẨM", "Phân loại sản phẩm theo nhóm"));
            Load += (s, e) => { LoadData(); NewMode(); };
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                grid.DataSource = Database.Query(
                    @"SELECT c.Id, c.CategoryName AS `Tên danh mục`, c.Description AS `Mô tả`,
                             (SELECT COUNT(*) FROM Products p WHERE p.CategoryId = c.Id AND p.IsActive = 1) AS `Số sản phẩm`
                      FROM Categories c WHERE c.IsActive = 1 ORDER BY c.CategoryName");
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void NewMode()
        {
            editingId = null;
            txtName.Clear(); txtDesc.Clear();
            lblMode.Text = "Đang thêm danh mục mới";
            grid.ClearSelection();
            txtName.Focus();
        }

        private void FillFromGrid()
        {
            if (!grid.Focused || Ui.SelectedId(grid) is not int id) return;
            editingId = id;
            txtName.Text = grid.CurrentRow!.Cells["Tên danh mục"].Value?.ToString();
            txtDesc.Text = grid.CurrentRow.Cells["Mô tả"].Value?.ToString();
            lblMode.Text = "Đang sửa: " + txtName.Text;
        }

        private void Save()
        {
            string name = txtName.Text.Trim();
            if (name == "") { Ui.Warn("Vui lòng nhập tên danh mục."); txtName.Focus(); return; }
            try
            {
                var dup = Database.ToInt(Database.Scalar(
                    "SELECT COUNT(*) FROM Categories WHERE CategoryName = @n AND Id <> @id AND IsActive = 1",
                    ("@n", name), ("@id", editingId ?? 0)));
                if (dup > 0) { Ui.Warn("Tên danh mục đã tồn tại."); return; }

                if (editingId == null)
                {
                    // nếu trước đó đã xóa danh mục cùng tên thì khôi phục lại
                    int restored = Database.Execute(
                        "UPDATE Categories SET IsActive = 1, Description = @d WHERE CategoryName = @n AND IsActive = 0",
                        ("@n", name), ("@d", txtDesc.Text.Trim()));
                    if (restored == 0)
                        Database.Execute("INSERT INTO Categories (CategoryName, Description) VALUES (@n, @d)",
                            ("@n", name), ("@d", txtDesc.Text.Trim()));
                }
                else
                    Database.Execute("UPDATE Categories SET CategoryName = @n, Description = @d WHERE Id = @id",
                        ("@n", name), ("@d", txtDesc.Text.Trim()), ("@id", editingId));
                Ui.Info("Đã lưu danh mục.");
                LoadData();
                NewMode();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void Delete()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn danh mục cần xóa."); return; }
            try
            {
                int count = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Products WHERE CategoryId = @id AND IsActive = 1", ("@id", id)));
                if (count > 0) { Ui.Warn($"Danh mục đang có {count} sản phẩm, không thể xóa.\nHãy chuyển các sản phẩm sang danh mục khác trước."); return; }
                if (!Ui.Confirm("Xóa danh mục đã chọn?")) return;
                Database.Execute("UPDATE Categories SET IsActive = 0 WHERE Id = @id", ("@id", id));
                LoadData();
                NewMode();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }
}
