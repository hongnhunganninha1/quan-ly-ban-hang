using System.Data;

namespace QuanLyBanHang
{
    /// <summary>
    /// Lập hóa đơn bán hàng (theo sơ đồ sequence "Lập hóa đơn"):
    /// nhập sản phẩm + số lượng → kiểm tra tồn kho → tính tổng → xác nhận thanh toán
    /// → lưu hóa đơn + trừ tồn kho → in hóa đơn.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class SalesForm : AppForm
    {
        private DataTable products = new();
        private readonly DataTable cart = new();
        private readonly DataGridView grid = Ui.Grid(false);

        private readonly TextBox txtCode = Ui.Txt(170);
        private readonly DateTimePicker dtDate = Ui.Date(150);
        private readonly ComboBox cboCustomer = Ui.Combo(260);
        private readonly TextBox txtProduct = Ui.Txt(360, "Nhập mã / tên sản phẩm rồi Enter...");
        private readonly NumericUpDown numQty = Ui.Num(10_000, 70);
        private readonly NumericUpDown numDiscPct = Ui.Num(100, 90, 1);
        private readonly NumericUpDown numDiscVnd = Ui.Num(1_000_000_000, 140);
        private readonly NumericUpDown numVat = Ui.Num(100, 90, 1);
        private readonly Label lblSub = new(), lblDisc = new(), lblVat = new(), lblItems = new();
        private readonly Label lblTotal = new();

        private decimal subTotal, discount, vat, total;

        public SalesForm() : base("Lập hóa đơn", 1200, 720)
        {
            WindowState = FormWindowState.Maximized;
            txtCode.ReadOnly = true;
            txtCode.BackColor = Theme.PrimaryLight;
            dtDate.CustomFormat = "dd/MM/yyyy HH:mm";
            dtDate.Enabled = false;
            numQty.Minimum = 1; numQty.Value = 1;
            numDiscVnd.Increment = 1000;

            // ----- dòng thông tin hóa đơn -----
            var info = Ui.Toolbar();
            var btnNewCus = Ui.Btn("+ KH mới", BtnKind.Secondary, 100, 32);
            info.Controls.AddRange(new Control[] { Ui.Lbl("Số HĐ:", true), txtCode, Ui.Lbl("Ngày lập:", true), dtDate,
                Ui.Lbl("Khách hàng:", true), cboCustomer, btnNewCus });

            // ----- dòng nhập sản phẩm -----
            var add = Ui.Toolbar();
            var btnAdd = Ui.Btn("+ Thêm", BtnKind.Primary, 100, 32);
            var btnPick = Ui.Btn("Chọn từ danh sách", BtnKind.Secondary, 160, 32);
            var btnRemove = Ui.Btn("Xóa dòng", BtnKind.Danger, 100, 32);
            add.Controls.AddRange(new Control[] { Ui.Lbl("Sản phẩm:", true), txtProduct, Ui.Lbl("SL:", true), numQty, btnAdd, btnPick, btnRemove });

            // ----- bảng giỏ hàng -----
            cart.Columns.Add("ProductId", typeof(int));
            cart.Columns.Add("Mã SP", typeof(string));
            cart.Columns.Add("Tên sản phẩm", typeof(string));
            cart.Columns.Add("Số lượng", typeof(int));
            cart.Columns.Add("Đơn giá", typeof(decimal));
            cart.Columns.Add("Thành tiền", typeof(decimal));
            cart.Columns.Add("Tồn kho", typeof(int));
            cart.Columns.Add("CostPrice", typeof(decimal));
            grid.DataSource = cart;
            grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn c in grid.Columns) c.ReadOnly = c.Name != "Số lượng";
                if (grid.Columns.Contains("CostPrice")) grid.Columns["CostPrice"]!.Visible = false;
                if (grid.Columns.Contains("Số lượng"))
                {
                    grid.Columns["Số lượng"]!.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 230);
                    grid.Columns["Số lượng"]!.HeaderText = "Số lượng ✎";
                }
            };
            grid.CellEndEdit += (s, e) => OnQtyEdited(e.RowIndex);
            grid.DataError += (s, e) => { Ui.Warn("Số lượng phải là số nguyên."); e.Cancel = true; };
            var gridWrap = Ui.Card();
            gridWrap.Controls.Add(grid);

            // ----- phần thanh toán phía dưới -----
            // ----- phần tính tiền phía dưới (đặt vị trí cố định cho chắc chắn hiển thị đủ) -----
            var bottom = new Panel { Height = 190, BackColor = Color.White };
            var line = new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Theme.Border };

            var pLeft = new Panel { Dock = DockStyle.Left, Width = 420 };
            pLeft.Controls.Add(new Label { Text = "GIẢM GIÁ & THUẾ", Font = Theme.Bold, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(20, 14) });
            Place(pLeft, "Giảm giá (%)", numDiscPct, 46);
            Place(pLeft, "Giảm thêm (đ)", numDiscVnd, 86);
            Place(pLeft, "Thuế VAT (%)", numVat, 126);
            numDiscPct.Width = 120; numDiscVnd.Width = 180; numVat.Width = 120;

            var pRight = new Panel { Dock = DockStyle.Right, Width = 390 };
            lblTotal.AutoSize = false; lblTotal.Size = new Size(360, 58); lblTotal.Location = new Point(10, 14);
            lblTotal.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTotal.ForeColor = Color.White; lblTotal.BackColor = Theme.Primary;
            lblTotal.TextAlign = ContentAlignment.MiddleCenter;
            var btnPay = Ui.Btn("💳 THANH TOÁN (F12)", BtnKind.Success, 360, 50);
            btnPay.Location = new Point(10, 80);
            btnPay.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            var btnCancel = Ui.Btn("✖ Hủy hóa đơn (Esc)", BtnKind.Secondary, 360, 40);
            btnCancel.Location = new Point(10, 138);
            pRight.Controls.AddRange(new Control[] { lblTotal, btnPay, btnCancel });

            var pMid = new Panel { Dock = DockStyle.Fill };
            pMid.Controls.Add(new Label { Text = "TỔNG KẾT", Font = Theme.Bold, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(20, 14) });
            Place(pMid, "Số sản phẩm:", lblItems, 46);
            Place(pMid, "Tạm tính:", lblSub, 76);
            Place(pMid, "Giảm giá:", lblDisc, 106);
            Place(pMid, "Thuế VAT:", lblVat, 136);
            foreach (var l in new[] { lblItems, lblSub, lblDisc, lblVat })
            {
                l.AutoSize = true; l.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                l.Location = new Point(l.Left, l.Top + 4);
            }

            bottom.Controls.Add(pMid);
            bottom.Controls.Add(pLeft);
            bottom.Controls.Add(pRight);
            bottom.Controls.Add(line);

            Ui.Stack(this, gridWrap, new Control[] { Ui.Header("🧾 LẬP HÓA ĐƠN BÁN HÀNG", $"Nhân viên: {Session.FullName}"), info, add }, new Control[] { bottom });

            // ----- sự kiện -----
            txtProduct.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AddProduct(); e.SuppressKeyPress = true; } };
            btnAdd.Click += (s, e) => AddProduct();
            btnPick.Click += (s, e) => PickProduct("");
            btnRemove.Click += (s, e) =>
            {
                if (grid.CurrentRow == null) { Ui.Warn("Chọn dòng cần xóa."); return; }
                cart.Rows.RemoveAt(grid.CurrentRow.Index);
                Recalc();
            };
            btnNewCus.Click += (s, e) => QuickAddCustomer();
            foreach (var n in new[] { numDiscPct, numDiscVnd, numVat }) n.ValueChanged += (s, e) => Recalc();
            btnPay.Click += (s, e) => Pay();
            btnCancel.Click += (s, e) => CancelInvoice();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F12) { Pay(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { CancelInvoice(); e.Handled = true; }
            };

            Load += (s, e) => { LoadLookups(); NewInvoice(); };
            EndInit();
        }

        private static void Place(Panel p, string label, Control c, int y)
        {
            var l = Ui.Lbl(label);
            l.Location = new Point(20, y + 4);
            c.Location = new Point(150, y);
            p.Controls.Add(l);
            p.Controls.Add(c);
        }

        private void LoadLookups()
        {
            try
            {
                products = Database.Query(
                    @"SELECT p.Id, p.ProductCode, p.Name, p.UnitPrice, p.CostPrice, IFNULL(i.QuantityInStock, 0) AS Stock
                      FROM Products p LEFT JOIN Inventory i ON i.ProductId = p.Id
                      WHERE p.IsActive = 1 ORDER BY p.ProductCode");
                var auto = new AutoCompleteStringCollection();
                foreach (DataRow r in products.Rows) auto.Add($"{r["ProductCode"]} - {r["Name"]}");
                txtProduct.AutoCompleteCustomSource = auto;
                txtProduct.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtProduct.AutoCompleteSource = AutoCompleteSource.CustomSource;
                LoadCustomers(null);
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void LoadCustomers(int? select)
        {
            var dt = Database.Query(
                @"SELECT Id, CONCAT(CustomerName, IFNULL(CONCAT(' - ', Phone), '')) AS Display
                  FROM Customers WHERE IsActive = 1 ORDER BY CustomerName");
            var walkIn = dt.NewRow();
            walkIn["Id"] = 0;
            walkIn["Display"] = "-- Khách lẻ --";
            dt.Rows.InsertAt(walkIn, 0);
            cboCustomer.DisplayMember = "Display";
            cboCustomer.ValueMember = "Id";
            cboCustomer.DataSource = dt;
            cboCustomer.DropDownStyle = ComboBoxStyle.DropDown;
            cboCustomer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cboCustomer.AutoCompleteSource = AutoCompleteSource.ListItems;
            cboCustomer.SelectedValue = select ?? 0;
        }

        private void NewInvoice()
        {
            cart.Rows.Clear();
            txtCode.Text = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");
            dtDate.Value = DateTime.Now;
            if (cboCustomer.Items.Count > 0) cboCustomer.SelectedValue = 0;
            numDiscPct.Value = 0; numDiscVnd.Value = 0; numVat.Value = 0;
            txtProduct.Clear(); numQty.Value = 1;
            Recalc();
            txtProduct.Focus();
        }

        // ---------- thêm sản phẩm vào giỏ ----------
        private void AddProduct()
        {
            string kw = txtProduct.Text.Trim();
            if (kw == "") { PickProduct(""); return; }
            if (kw.Contains(" - ")) kw = kw.Substring(0, kw.IndexOf(" - ", StringComparison.Ordinal)).Trim();

            var exact = products.Select($"ProductCode = '{kw.Replace("'", "''")}'");
            if (exact.Length == 1) { AddToCart(exact[0], (int)numQty.Value); return; }

            string like = kw.Replace("'", "''").Replace("[", "[[]").Replace("*", "[*]").Replace("%", "[%]");
            var found = products.Select($"ProductCode LIKE '%{like}%' OR Name LIKE '%{like}%'");
            if (found.Length == 1) AddToCart(found[0], (int)numQty.Value);
            else PickProduct(kw);
        }

        private void PickProduct(string keyword)
        {
            using var dlg = new ProductPickerDialog(products, keyword);
            if (dlg.ShowDialog(Ui.Owner(this)) == DialogResult.OK && dlg.Selected != null)
                AddToCart(dlg.Selected, (int)numQty.Value);
        }

        private void AddToCart(DataRow p, int qty)
        {
            int pid = Convert.ToInt32(p["Id"]);
            int stock = Convert.ToInt32(p["Stock"]);
            var existing = cart.Select("ProductId = " + pid);
            int already = existing.Length > 0 ? (int)existing[0]["Số lượng"] : 0;
            if (already + qty > stock)
            {
                Ui.Warn($"Sản phẩm \"{p["Name"]}\" chỉ còn {stock} trong kho" + (already > 0 ? $" (đã thêm {already})." : "."), "Không đủ tồn kho");
                return;
            }
            decimal price = Convert.ToDecimal(p["UnitPrice"]);
            if (existing.Length > 0)
            {
                existing[0]["Số lượng"] = already + qty;
                existing[0]["Thành tiền"] = (already + qty) * price;
            }
            else
            {
                cart.Rows.Add(pid, p["ProductCode"], p["Name"], qty, price, qty * price, stock, p["CostPrice"]);
            }
            txtProduct.Clear();
            numQty.Value = 1;
            Recalc();
            txtProduct.Focus();
        }

        private void OnQtyEdited(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= cart.Rows.Count) return;
            var r = cart.Rows[rowIndex];
            int qty = r["Số lượng"] == DBNull.Value ? 1 : (int)r["Số lượng"];
            int stock = (int)r["Tồn kho"];
            if (qty <= 0) { qty = 1; Ui.Warn("Số lượng phải lớn hơn 0."); }
            if (qty > stock) { qty = stock; Ui.Warn($"Chỉ còn {stock} sản phẩm trong kho."); }
            r["Số lượng"] = qty;
            r["Thành tiền"] = qty * (decimal)r["Đơn giá"];
            Recalc();
        }

        // ---------- tính tiền ----------
        private void Recalc()
        {
            subTotal = 0;
            foreach (DataRow r in cart.Rows) subTotal += (decimal)r["Thành tiền"];
            discount = Math.Round(subTotal * numDiscPct.Value / 100, 0) + numDiscVnd.Value;
            if (discount > subTotal) discount = subTotal;
            vat = Math.Round((subTotal - discount) * numVat.Value / 100, 0);
            total = subTotal - discount + vat;

            lblSub.Text = Fmt.Money(subTotal);
            lblDisc.Text = "- " + Fmt.Money(discount);
            lblVat.Text = "+ " + Fmt.Money(vat);
            lblTotal.Text = "TỔNG: " + Fmt.Money(total);
            int count = 0;
            foreach (DataRow r in cart.Rows) count += (int)r["Số lượng"];
            lblItems.Text = count.ToString();
        }

        private void QuickAddCustomer()
        {
            using var dlg = new CustomerEditDialog(null);
            if (dlg.ShowDialog(Ui.Owner(this)) == DialogResult.OK) LoadCustomers(dlg.SavedId);
        }

        private void CancelInvoice()
        {
            if (cart.Rows.Count == 0) { Close(); return; }
            if (Ui.Confirm("Hủy hóa đơn đang lập? Các sản phẩm đã thêm sẽ bị xóa.")) NewInvoice();
        }

        // ---------- xác nhận thanh toán ----------
        private void Pay()
        {
            grid.EndEdit();
            if (cart.Rows.Count == 0) { Ui.Warn("Hóa đơn chưa có sản phẩm nào."); txtProduct.Focus(); return; }
            int customerId = cboCustomer.SelectedValue == null ? 0 : Convert.ToInt32(cboCustomer.SelectedValue);
            if (cboCustomer.SelectedIndex < 0 && cboCustomer.Text.Trim() != "")
            {
                Ui.Warn("Không tìm thấy khách hàng đã nhập. Chọn trong danh sách hoặc bấm \"+ KH mới\".");
                return;
            }
            // chọn phương thức thanh toán: tiền mặt (nhập tiền khách đưa) / chuyển khoản (mã QR) / thẻ
            string payMethod;
            decimal cashGiven;
            using (var dlg = new PaymentDialog(total, txtCode.Text))
            {
                if (dlg.ShowDialog(Ui.Owner(this)) != DialogResult.OK) return;
                payMethod = dlg.PaymentMethod;
                cashGiven = dlg.CashGiven;
            }

            int orderId;
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    Database.Command(conn, tx,
                        @"INSERT INTO Orders (OrderCode, OrderDate, CustomerId, UserId, SubTotal, DiscountPercent, DiscountAmount,
                                              VatPercent, VatAmount, TotalAmount, PaymentMethod, Status)
                          VALUES (@code, NOW(), @cus, @user, @sub, @dp, @disc, @vp, @vat, @total, @pay, 'Paid')",
                        ("@code", txtCode.Text), ("@cus", customerId == 0 ? (object?)null : customerId), ("@user", Session.UserId),
                        ("@sub", subTotal), ("@dp", numDiscPct.Value), ("@disc", discount), ("@vp", numVat.Value),
                        ("@vat", vat), ("@total", total), ("@pay", payMethod)).ExecuteNonQuery();
                    orderId = Convert.ToInt32(Database.Command(conn, tx, "SELECT LAST_INSERT_ID()").ExecuteScalar());

                    foreach (DataRow r in cart.Rows)
                    {
                        int pid = (int)r["ProductId"], qty = (int)r["Số lượng"];
                        Database.Command(conn, tx,
                            @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, CostPrice, LineTotal)
                              VALUES (@o, @p, @q, @price, @cost, @line)",
                            ("@o", orderId), ("@p", pid), ("@q", qty), ("@price", r["Đơn giá"]), ("@cost", r["CostPrice"]),
                            ("@line", r["Thành tiền"])).ExecuteNonQuery();

                        // trừ tồn kho - chỉ trừ khi còn đủ hàng
                        int ok = Database.Command(conn, tx,
                            @"UPDATE Inventory SET QuantityInStock = QuantityInStock - @q, LastUpdated = NOW()
                              WHERE ProductId = @p AND QuantityInStock >= @q",
                            ("@q", qty), ("@p", pid)).ExecuteNonQuery();
                        if (ok == 0) throw new InvalidOperationException($"Sản phẩm \"{r["Tên sản phẩm"]}\" không còn đủ hàng trong kho.");

                        Database.Command(conn, tx,
                            @"INSERT INTO InventoryLogs (ProductId, ChangeQty, StockAfter, Note, UserId)
                              SELECT @p, -@q, QuantityInStock, @note, @u FROM Inventory WHERE ProductId = @p",
                            ("@p", pid), ("@q", qty), ("@note", "Bán hàng - " + txtCode.Text), ("@u", Session.UserId)).ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Ui.Error("Không lưu được hóa đơn:\n" + ex.Message);
                    LoadLookups();
                    return;
                }
            }

            string done = $"Thanh toán thành công ({payMethod})!";
            if (payMethod == "Tiền mặt")
                done += $"\nKhách đưa: {Fmt.Money(cashGiven)}\nTiền trả lại khách: {Fmt.Money(cashGiven - total)}";
            if (Ui.Confirm(done + "\n\nBạn có muốn in hóa đơn không?", "Hoàn tất"))
                InvoicePrinter.Preview(orderId, Ui.Owner(this));
            LoadLookups();
            NewInvoice();
        }
    }

    /// <summary>Hộp thoại chọn sản phẩm khi tìm thấy nhiều kết quả.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class ProductPickerDialog : AppForm
    {
        public DataRow? Selected { get; private set; }
        private readonly DataView view;
        private readonly DataGridView grid = Ui.Grid();

        public ProductPickerDialog(DataTable products, string keyword) : base("Chọn sản phẩm", 760, 480)
        {
            view = new DataView(products);
            var txt = Ui.Txt(300, "Lọc theo mã / tên...");
            txt.Text = keyword;
            var bar = Ui.Toolbar();
            var btnOk = Ui.Btn("Chọn", BtnKind.Primary, 100);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Tìm:"), txt, btnOk });
            grid.DataSource = view;
            grid.DataBindingComplete += (s, e) =>
            {
                var names = new Dictionary<string, string>
                {
                    ["ProductCode"] = "Mã SP", ["Name"] = "Tên sản phẩm", ["UnitPrice"] = "Giá bán", ["Stock"] = "Tồn kho",
                };
                foreach (DataGridViewColumn c in grid.Columns)
                {
                    if (names.TryGetValue(c.Name, out var h)) c.HeaderText = h;
                    if (c.Name == "CostPrice") c.Visible = false;
                }
            };
            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("CHỌN SẢN PHẨM", "Nhấp đúp hoặc Enter để chọn", 50), bar });

            void Filter()
            {
                string k = txt.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("*", "[*]").Replace("%", "[%]");
                view.RowFilter = k == "" ? "" : $"ProductCode LIKE '%{k}%' OR Name LIKE '%{k}%'";
            }
            void Choose()
            {
                if (grid.CurrentRow?.DataBoundItem is DataRowView drv)
                {
                    if (Convert.ToInt32(drv["Stock"]) <= 0) { Ui.Warn("Sản phẩm này đã hết hàng."); return; }
                    Selected = drv.Row; DialogResult = DialogResult.OK; Close();
                }
            }
            txt.TextChanged += (s, e) => Filter();
            txt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down) { grid.Focus(); e.Handled = true; }
                if (e.KeyCode == Keys.Enter) { Choose(); e.SuppressKeyPress = true; }
            };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Choose(); e.SuppressKeyPress = true; } };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Choose(); };
            btnOk.Click += (s, e) => Choose();
            Filter();
            EndInit();
        }
    }
}
