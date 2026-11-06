using System.Data;

namespace QuanLyBanHang
{
    /// <summary>Quản lý sản phẩm: thêm, sửa, xóa, tìm kiếm, xem ảnh.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class ProductForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();
        private readonly TextBox txtSearch = Ui.Txt(240, "Tìm theo mã hoặc tên sản phẩm...");
        private readonly ComboBox cboCat = Ui.Combo(180);
        private readonly PictureBox pic = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Top, Height = 230, BackColor = Theme.PrimaryLight };
        private readonly Label lblInfo = new() { Dock = DockStyle.Fill, Padding = new Padding(4, 10, 4, 0), Font = Theme.Base };

        public ProductForm() : base("Quản lý sản phẩm", 1150, 640)
        {
            var bar = Ui.Toolbar();
            var btnFind = Ui.Btn("🔍 Tìm", BtnKind.Secondary, 90);
            var btnAdd = Ui.Btn("+ Thêm", BtnKind.Primary, 100);
            var btnEdit = Ui.Btn("✎ Sửa", BtnKind.Primary, 100);
            var btnDel = Ui.Btn("Xóa", BtnKind.Danger, 90);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Tìm kiếm:"), txtSearch, Ui.Lbl("Danh mục:"), cboCat, btnFind, btnAdd, btnEdit, btnDel, btnCsv });

            var side = new Panel { Dock = DockStyle.Right, Width = 270, Padding = new Padding(12), BackColor = Color.White };
            side.Controls.Add(lblInfo);
            side.Controls.Add(pic);

            var gridWrap = Ui.Card();
            gridWrap.Controls.Add(grid);

            Ui.Stack(this, gridWrap, new Control[] { Ui.Header("QUẢN LÝ SẢN PHẨM", "Thêm, sửa, xóa sản phẩm"), bar }, new Control[] { side });

            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadData(); e.SuppressKeyPress = true; } };
            btnFind.Click += (s, e) => LoadData();
            cboCat.SelectedIndexChanged += (s, e) => LoadData();
            btnAdd.Click += (s, e) => Edit(null);
            btnEdit.Click += (s, e) => { if (Ui.SelectedId(grid) is int id) Edit(id); else Ui.Warn("Chọn sản phẩm cần sửa."); };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && Ui.SelectedId(grid) is int id) Edit(id); };
            btnDel.Click += (s, e) => Delete();
            btnCsv.Click += (s, e) => Ui.ExportCsv(grid, "DanhSachSanPham");
            grid.SelectionChanged += (s, e) => ShowPreview();
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Tồn kho") return;
                var r = grid.Rows[e.RowIndex];
                if (Database.ToInt(r.Cells["Tồn kho"].Value) <= Database.ToInt(r.Cells["Tồn tối thiểu"].Value))
                {
                    e.CellStyle!.ForeColor = Theme.Danger;
                    e.CellStyle.Font = Theme.Bold;
                }
            };

            Load += (s, e) => { LoadCategories(); LoadData(); };
            EndInit();
        }

        private void LoadCategories()
        {
            try
            {
                var dt = Database.Query("SELECT Id, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");
                var all = dt.NewRow(); all["Id"] = 0; all["CategoryName"] = "-- Tất cả --";
                dt.Rows.InsertAt(all, 0);
                cboCat.DisplayMember = "CategoryName";
                cboCat.ValueMember = "Id";
                cboCat.DataSource = dt;
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void LoadData()
        {
            try
            {
                int catId = cboCat.SelectedValue is int v ? v : 0;
                grid.DataSource = Database.Query(
                    @"SELECT p.Id, p.ProductCode AS `Mã SP`, p.Name AS `Tên sản phẩm`, c.CategoryName AS `Danh mục`,
                             p.CostPrice AS `Giá nhập`, p.UnitPrice AS `Giá bán`, p.Unit AS `Đơn vị`,
                             IFNULL(i.QuantityInStock, 0) AS `Tồn kho`, IFNULL(i.MinStock, 0) AS `Tồn tối thiểu`,
                             p.ImagePath AS `Ảnh`
                      FROM Products p
                      JOIN Categories c ON c.Id = p.CategoryId
                      LEFT JOIN Inventory i ON i.ProductId = p.Id
                      WHERE p.IsActive = 1
                        AND (@kw = '' OR p.ProductCode LIKE @like OR p.Name LIKE @like)
                        AND (@cat = 0 OR p.CategoryId = @cat)
                      ORDER BY p.ProductCode",
                    ("@kw", txtSearch.Text.Trim()), ("@like", "%" + txtSearch.Text.Trim() + "%"), ("@cat", catId));
                if (grid.Columns.Contains("Ảnh")) grid.Columns["Ảnh"]!.Visible = false;
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void ShowPreview()
        {
            var old = pic.Image;
            pic.Image = null;
            old?.Dispose();
            if (grid.CurrentRow == null) { lblInfo.Text = ""; return; }
            var r = grid.CurrentRow;
            pic.Image = Ui.LoadImage(r.Cells["Ảnh"].Value?.ToString());
            lblInfo.Text =
                $"{r.Cells["Tên sản phẩm"].Value}\n\n" +
                $"Mã: {r.Cells["Mã SP"].Value}\n" +
                $"Danh mục: {r.Cells["Danh mục"].Value}\n" +
                $"Giá bán: {Fmt.Money(Database.ToDecimal(r.Cells["Giá bán"].Value))}\n" +
                $"Tồn kho: {r.Cells["Tồn kho"].Value} {r.Cells["Đơn vị"].Value}" +
                (pic.Image == null ? "\n\n(Chưa có ảnh)" : "");
        }

        private void Edit(int? id)
        {
            using var dlg = new ProductEditDialog(id);
            if (dlg.ShowDialog(Ui.Owner(this)) == DialogResult.OK) LoadData();
        }

        private void Delete()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn sản phẩm cần xóa."); return; }
            var name = grid.CurrentRow!.Cells["Tên sản phẩm"].Value;
            if (!Ui.Confirm($"Xóa sản phẩm \"{name}\"?\n(Sản phẩm sẽ bị ẩn, lịch sử hóa đơn cũ vẫn được giữ.)")) return;
            try
            {
                Database.Execute("UPDATE Products SET IsActive = 0, UpdatedAt = NOW() WHERE Id = @id", ("@id", id));
                LoadData();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }

    /// <summary>Hộp thoại thêm / sửa sản phẩm.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class ProductEditDialog : DialogBase
    {
        private readonly int? productId;
        private readonly TextBox txtCode = Ui.Txt();
        private readonly TextBox txtName = Ui.Txt();
        private readonly ComboBox cboCat = Ui.Combo();
        private readonly NumericUpDown numCost = Ui.Num();
        private readonly NumericUpDown numPrice = Ui.Num();
        private readonly ComboBox cboUnit = new() { DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.Base };
        private readonly NumericUpDown numStock = Ui.Num(1_000_000);
        private readonly NumericUpDown numMin = Ui.Num(1_000_000);
        private readonly PictureBox pic = new() { SizeMode = PictureBoxSizeMode.Zoom, Height = 120, BackColor = Theme.PrimaryLight, BorderStyle = BorderStyle.FixedSingle };
        private string? imagePath;      // đường dẫn đang lưu trong CSDL
        private string? newImageSource; // ảnh mới người dùng vừa chọn

        public ProductEditDialog(int? id) : base(id == null ? "Thêm sản phẩm" : "Sửa sản phẩm", 520)
        {
            productId = id;
            numCost.Increment = 1000; numPrice.Increment = 1000; numMin.Value = 5;
            cboUnit.Items.AddRange(new object[] { "chiếc", "cái", "bộ", "đôi", "hộp" });
            cboUnit.Text = "chiếc";

            var btnImg = Ui.Btn("Chọn ảnh...", BtnKind.Secondary, 120, 32);
            btnImg.Anchor = AnchorStyles.Left;
            btnImg.Click += (s, e) => PickImage();

            AddField("Mã sản phẩm *", txtCode);
            AddField("Tên sản phẩm *", txtName);
            AddField("Danh mục *", cboCat);
            AddField("Giá nhập (đ)", numCost);
            AddField("Giá bán (đ) *", numPrice);
            AddField("Đơn vị tính", cboUnit);
            AddField(id == null ? "Tồn kho ban đầu" : "Tồn kho hiện tại", numStock);
            AddField("Tồn tối thiểu", numMin);
            AddField("Hình ảnh", btnImg);
            AddField("", pic, 130);

            if (id != null) numStock.Enabled = false; // tồn kho thay đổi qua chức năng Nhập hàng
            Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                cboCat.DisplayMember = "CategoryName";
                cboCat.ValueMember = "Id";
                cboCat.DataSource = Database.Query("SELECT Id, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");

                if (productId == null)
                {
                    // gợi ý mã tiếp theo: SP001, SP002...
                    int next = Database.ToInt(Database.Scalar(
                        "SELECT IFNULL(MAX(CAST(SUBSTRING(ProductCode, 3) AS UNSIGNED)), 0) + 1 FROM Products WHERE ProductCode LIKE 'SP%'"));
                    txtCode.Text = "SP" + next.ToString("000");
                    return;
                }
                var dt = Database.Query(
                    @"SELECT p.*, IFNULL(i.QuantityInStock, 0) AS Stock, IFNULL(i.MinStock, 5) AS MinStock
                      FROM Products p LEFT JOIN Inventory i ON i.ProductId = p.Id WHERE p.Id = @id", ("@id", productId));
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                txtCode.Text = r["ProductCode"].ToString();
                txtName.Text = r["Name"].ToString();
                cboCat.SelectedValue = Convert.ToInt32(r["CategoryId"]);
                numCost.Value = Database.ToDecimal(r["CostPrice"]);
                numPrice.Value = Database.ToDecimal(r["UnitPrice"]);
                cboUnit.Text = r["Unit"].ToString();
                numStock.Value = Database.ToInt(r["Stock"]);
                numMin.Value = Database.ToInt(r["MinStock"]);
                imagePath = r["ImagePath"] == DBNull.Value ? null : r["ImagePath"].ToString();
                pic.Image = Ui.LoadImage(imagePath);
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void PickImage()
        {
            using var dlg = new OpenFileDialog { Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            newImageSource = dlg.FileName;
            var old = pic.Image;
            pic.Image = Ui.LoadImage(dlg.FileName);
            old?.Dispose();
        }

        protected override bool OnSave()
        {
            string code = txtCode.Text.Trim(), name = txtName.Text.Trim(), unit = cboUnit.Text.Trim();
            if (code == "") { Ui.Warn("Vui lòng nhập mã sản phẩm."); txtCode.Focus(); return false; }
            if (name == "") { Ui.Warn("Vui lòng nhập tên sản phẩm."); txtName.Focus(); return false; }
            if (cboCat.SelectedValue is not int catId) { Ui.Warn("Vui lòng chọn danh mục (tạo danh mục trước nếu chưa có)."); return false; }
            if (numPrice.Value <= 0) { Ui.Warn("Giá bán phải lớn hơn 0."); numPrice.Focus(); return false; }
            if (numPrice.Value < numCost.Value && !Ui.Confirm("Giá bán đang thấp hơn giá nhập (bán lỗ). Vẫn lưu?")) return false;
            if (unit == "") unit = "chiếc";

            int dup = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Products WHERE ProductCode = @c AND Id <> @id",
                ("@c", code), ("@id", productId ?? 0)));
            if (dup > 0) { Ui.Warn("Mã sản phẩm đã tồn tại (kể cả sản phẩm đã xóa). Hãy dùng mã khác."); return false; }

            // chép ảnh vào thư mục images cạnh file chạy để không bị mất khi xóa ảnh gốc
            string? savePath = imagePath;
            if (newImageSource != null)
            {
                var dir = Path.Combine(Application.StartupPath, "images");
                Directory.CreateDirectory(dir);
                var file = code + "_" + DateTime.Now.Ticks + Path.GetExtension(newImageSource);
                File.Copy(newImageSource, Path.Combine(dir, file), true);
                savePath = Path.Combine("images", file);
            }

            using var conn = Database.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                if (productId == null)
                {
                    Database.Command(conn, tx,
                        @"INSERT INTO Products (ProductCode, Name, CategoryId, CostPrice, UnitPrice, Unit, ImagePath)
                          VALUES (@c, @n, @cat, @cost, @price, @unit, @img)",
                        ("@c", code), ("@n", name), ("@cat", catId), ("@cost", numCost.Value), ("@price", numPrice.Value),
                        ("@unit", unit), ("@img", savePath)).ExecuteNonQuery();
                    long newId = Convert.ToInt64(Database.Command(conn, tx, "SELECT LAST_INSERT_ID()").ExecuteScalar());
                    int stock = (int)numStock.Value;
                    Database.Command(conn, tx,
                        "INSERT INTO Inventory (ProductId, QuantityInStock, MinStock) VALUES (@p, @q, @m)",
                        ("@p", newId), ("@q", stock), ("@m", (int)numMin.Value)).ExecuteNonQuery();
                    if (stock > 0)
                        Database.Command(conn, tx,
                            "INSERT INTO InventoryLogs (ProductId, ChangeQty, StockAfter, Note, UserId) VALUES (@p, @q, @q, 'Nhập kho ban đầu', @u)",
                            ("@p", newId), ("@q", stock), ("@u", Session.UserId)).ExecuteNonQuery();
                }
                else
                {
                    Database.Command(conn, tx,
                        @"UPDATE Products SET ProductCode = @c, Name = @n, CategoryId = @cat, CostPrice = @cost,
                              UnitPrice = @price, Unit = @unit, ImagePath = @img, UpdatedAt = NOW() WHERE Id = @id",
                        ("@c", code), ("@n", name), ("@cat", catId), ("@cost", numCost.Value), ("@price", numPrice.Value),
                        ("@unit", unit), ("@img", savePath), ("@id", productId)).ExecuteNonQuery();
                    Database.Command(conn, tx,
                        @"INSERT INTO Inventory (ProductId, QuantityInStock, MinStock) VALUES (@p, 0, @m)
                          ON DUPLICATE KEY UPDATE MinStock = @m",
                        ("@p", productId), ("@m", (int)numMin.Value)).ExecuteNonQuery();
                }
                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
