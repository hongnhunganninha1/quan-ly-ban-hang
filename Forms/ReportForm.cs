using System.Data;
using System.Drawing.Drawing2D;

namespace QuanLyBanHang
{
    /// <summary>
    /// Báo cáo thống kê (theo sơ đồ sequence "Báo cáo thống kê"):
    /// 1. Doanh thu, lợi nhuận theo ngày / tháng / quý
    /// 2. Tồn kho: cảnh báo sản phẩm dưới ngưỡng
    /// 3. Khách hàng: top khách hàng có hóa đơn cao nhất
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class ReportForm : AppForm
    {
        private readonly TabControl tabs = new()
        {
            Font = Theme.Bold, DrawMode = TabDrawMode.OwnerDrawFixed, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(210, 40),
        };

        // tab doanh thu
        private readonly DateTimePicker rFrom = Ui.Date(), rTo = Ui.Date();
        private readonly ComboBox rGroup = Ui.Combo(110);
        private readonly DataGridView rGrid = Ui.Grid();
        private readonly BarChart chart = new();
        private readonly Label rRevenue = new(), rCost = new(), rProfit = new(), rOrders = new();

        // tab tồn kho
        private readonly DataGridView sGrid = Ui.Grid();
        private readonly CheckBox sAll = new() { Text = "Hiện tất cả sản phẩm", AutoSize = true, Margin = new Padding(8, 10, 8, 4) };
        private readonly Label sOut = new(), sLow = new(), sValue = new(), sCount = new();

        // tab khách hàng
        private readonly DateTimePicker cFrom = Ui.Date(), cTo = Ui.Date();
        private readonly NumericUpDown cTop = Ui.Num(100, 70);
        private readonly DataGridView cGrid = Ui.Grid();

        public ReportForm(int tabIndex = 0) : base("Báo cáo thống kê", 1200, 720)
        {
            WindowState = FormWindowState.Maximized;
            tabs.TabPages.Add(BuildRevenueTab());
            tabs.TabPages.Add(BuildStockTab());
            tabs.TabPages.Add(BuildCustomerTab());
            tabs.SelectedIndex = tabIndex;
            tabs.DrawItem += (s, e) =>
            {
                bool sel = e.Index == tabs.SelectedIndex;
                using var b = new SolidBrush(sel ? Theme.Primary : Color.White);
                e.Graphics.FillRectangle(b, e.Bounds);
                TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, Theme.Bold, e.Bounds,
                    sel ? Color.White : Theme.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            var wrap = new Panel { Padding = new Padding(12, 10, 12, 10) };
            wrap.Controls.Add(tabs);
            tabs.Dock = DockStyle.Fill;
            Ui.Stack(this, wrap, new Control[] { Ui.Header("📊 BÁO CÁO THỐNG KÊ", "Doanh thu - Lợi nhuận - Tồn kho - Khách hàng") });
            Load += (s, e) => { LoadRevenue(); LoadStock(); LoadCustomers(); };
            EndInit();
        }

        // =============== 1. DOANH THU - LỢI NHUẬN ===============
        private TabPage BuildRevenueTab()
        {
            var page = new TabPage("Doanh thu - Lợi nhuận") { BackColor = Theme.Back };
            rFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
            rTo.Value = DateTime.Today;
            rGroup.Items.AddRange(new object[] { "Ngày", "Tháng", "Quý" });
            rGroup.SelectedIndex = 1;

            var bar = Ui.Toolbar();
            var btnView = Ui.Btn("Xem báo cáo", BtnKind.Primary, 130);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            var qToday = Ui.Btn("Hôm nay", BtnKind.Secondary, 90, 32);
            var qMonth = Ui.Btn("Tháng này", BtnKind.Secondary, 100, 32);
            var qQuarter = Ui.Btn("Quý này", BtnKind.Secondary, 90, 32);
            var qYear = Ui.Btn("Năm nay", BtnKind.Secondary, 90, 32);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Từ:"), rFrom, Ui.Lbl("Đến:"), rTo, Ui.Lbl("Thống kê theo:"), rGroup, btnView, btnCsv,
                qToday, qMonth, qQuarter, qYear });

            var cards = new TableLayoutPanel { Height = 92, ColumnCount = 4, Padding = new Padding(8, 4, 8, 4) };
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards.Controls.Add(Card("Doanh thu (sau giảm giá)", rRevenue, Theme.Primary), 0, 0);
            cards.Controls.Add(Card("Giá vốn hàng bán", rCost, Color.FromArgb(109, 84, 94)), 1, 0);
            cards.Controls.Add(Card("Lợi nhuận gộp", rProfit, Theme.Success), 2, 0);
            cards.Controls.Add(Card("Số hóa đơn", rOrders, Theme.Accent), 3, 0);

            var split = new SplitContainer { Orientation = Orientation.Horizontal, SplitterWidth = 6, BackColor = Theme.Back };
            var gWrap = Ui.Card(); gWrap.Dock = DockStyle.Fill;
            gWrap.Controls.Add(rGrid);
            split.Panel1.Controls.Add(gWrap);
            chart.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(chart);
            split.Panel2.Padding = new Padding(8, 0, 8, 8);

            Ui.Stack(page, split, new Control[] { bar, cards });
            bool splitSet = false;
            page.Layout += (s, e) =>
            {
                if (!splitSet && split.Height > 300) { split.SplitterDistance = split.Height / 2; splitSet = true; }
            };

            btnView.Click += (s, e) => LoadRevenue();
            btnCsv.Click += (s, e) => Ui.ExportCsv(rGrid, "BaoCaoDoanhThu");
            var today = DateTime.Today;
            qToday.Click += (s, e) => SetRange(today, today, 0);
            qMonth.Click += (s, e) => SetRange(new DateTime(today.Year, today.Month, 1), today, 0);
            qQuarter.Click += (s, e) => SetRange(new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1), today, 1);
            qYear.Click += (s, e) => SetRange(new DateTime(today.Year, 1, 1), today, 1);
            return page;
        }

        private void SetRange(DateTime from, DateTime to, int group)
        {
            rFrom.Value = from; rTo.Value = to; rGroup.SelectedIndex = group;
            LoadRevenue();
        }

        private void LoadRevenue()
        {
            if (rFrom.Value.Date > rTo.Value.Date) { Ui.Warn("Ngày bắt đầu phải trước ngày kết thúc."); return; }
            string key, sort;
            switch (rGroup.SelectedIndex)
            {
                case 0: key = "DATE_FORMAT(o.OrderDate, '%d/%m/%Y')"; sort = "DATE(o.OrderDate)"; break;
                case 2: key = "CONCAT('Quý ', QUARTER(o.OrderDate), '/', YEAR(o.OrderDate))"; sort = "YEAR(o.OrderDate) * 10 + QUARTER(o.OrderDate)"; break;
                default: key = "DATE_FORMAT(o.OrderDate, '%m/%Y')"; sort = "YEAR(o.OrderDate) * 100 + MONTH(o.OrderDate)"; break;
            }
            try
            {
                var dt = Database.Query($@"
                    SELECT t.Ky AS `Kỳ`, COUNT(*) AS `Số hóa đơn`,
                           SUM(t.SubTotal) AS `Tiền hàng`, SUM(t.DiscountAmount) AS `Giảm giá`,
                           SUM(t.SubTotal - t.DiscountAmount) AS `Doanh thu`,
                           SUM(t.Cost) AS `Giá vốn`,
                           SUM(t.SubTotal - t.DiscountAmount - t.Cost) AS `Lợi nhuận`,
                           ROUND(SUM(t.SubTotal - t.DiscountAmount - t.Cost) * 100 / NULLIF(SUM(t.SubTotal - t.DiscountAmount), 0), 1) AS `Tỷ suất LN (%)`
                    FROM (
                        SELECT {key} AS Ky, {sort} AS SortKey, o.SubTotal, o.DiscountAmount,
                               (SELECT IFNULL(SUM(oi.CostPrice * oi.Quantity), 0) FROM OrderItems oi WHERE oi.OrderId = o.Id) AS Cost
                        FROM Orders o
                        WHERE o.Status = 'Paid' AND o.OrderDate >= @from AND o.OrderDate < @to
                    ) t
                    GROUP BY t.Ky, t.SortKey
                    ORDER BY t.SortKey",
                    ("@from", rFrom.Value.Date), ("@to", rTo.Value.Date.AddDays(1)));
                rGrid.DataSource = dt;

                decimal rev = 0, cost = 0, profit = 0; int orders = 0;
                var points = new List<(string, decimal, decimal)>();
                foreach (DataRow r in dt.Rows)
                {
                    decimal dRev = Database.ToDecimal(r["Doanh thu"]), dProfit = Database.ToDecimal(r["Lợi nhuận"]);
                    rev += dRev; profit += dProfit; cost += Database.ToDecimal(r["Giá vốn"]);
                    orders += Database.ToInt(r["Số hóa đơn"]);
                    points.Add((r["Kỳ"].ToString()!, dRev, dProfit));
                }
                rRevenue.Text = Fmt.Money(rev);
                rCost.Text = Fmt.Money(cost);
                rProfit.Text = Fmt.Money(profit);
                rOrders.Text = orders + " đơn";
                chart.SetData(points, $"Doanh thu và lợi nhuận theo {rGroup.Text.ToLower()}");
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        // =============== 2. TỒN KHO ===============
        private TabPage BuildStockTab()
        {
            var page = new TabPage("Tồn kho") { BackColor = Theme.Back };
            var bar = Ui.Toolbar();
            var btnLoad = Ui.Btn("Tải lại", BtnKind.Primary, 100);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            bar.Controls.AddRange(new Control[] { sAll, btnLoad, btnCsv });

            var cards = new TableLayoutPanel { Height = 92, ColumnCount = 4, Padding = new Padding(8, 4, 8, 4) };
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards.Controls.Add(Card("Sản phẩm hết hàng", sOut, Theme.Danger), 0, 0);
            cards.Controls.Add(Card("Sắp hết (≤ tồn tối thiểu)", sLow, Theme.Warning), 1, 0);
            cards.Controls.Add(Card("Tổng số lượng tồn", sCount, Theme.Primary), 2, 0);
            cards.Controls.Add(Card("Giá trị tồn kho (giá nhập)", sValue, Color.FromArgb(109, 84, 94)), 3, 0);

            var wrap = Ui.Card();
            wrap.Controls.Add(sGrid);
            Ui.Stack(page, wrap, new Control[] { bar, cards });

            sAll.CheckedChanged += (s, e) => LoadStock();
            btnLoad.Click += (s, e) => LoadStock();
            btnCsv.Click += (s, e) => Ui.ExportCsv(sGrid, "BaoCaoTonKho");
            sGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var st = sGrid.Rows[e.RowIndex].Cells["Trạng thái"].Value?.ToString();
                if (st == "Hết hàng") { e.CellStyle!.BackColor = Theme.DangerLight; e.CellStyle.ForeColor = Theme.Danger; }
                else if (st == "Sắp hết") { e.CellStyle!.BackColor = Color.FromArgb(255, 244, 225); e.CellStyle.ForeColor = Theme.Warning; }
            };
            return page;
        }

        private void LoadStock()
        {
            try
            {
                var sum = Database.Query(
                    @"SELECT SUM(i.QuantityInStock = 0) AS OutCnt,
                             SUM(i.QuantityInStock > 0 AND i.QuantityInStock <= i.MinStock) AS LowCnt,
                             SUM(i.QuantityInStock) AS Qty, SUM(i.QuantityInStock * p.CostPrice) AS Val
                      FROM Inventory i JOIN Products p ON p.Id = i.ProductId WHERE p.IsActive = 1");
                if (sum.Rows.Count > 0)
                {
                    var r = sum.Rows[0];
                    sOut.Text = Database.ToInt(r["OutCnt"]) + " sản phẩm";
                    sLow.Text = Database.ToInt(r["LowCnt"]) + " sản phẩm";
                    sCount.Text = Fmt.Num(Database.ToDecimal(r["Qty"]));
                    sValue.Text = Fmt.Money(Database.ToDecimal(r["Val"]));
                }
                sGrid.DataSource = Database.Query(
                    @"SELECT p.ProductCode AS `Mã SP`, p.Name AS `Tên sản phẩm`, c.CategoryName AS `Danh mục`,
                             i.QuantityInStock AS `Tồn kho`, i.MinStock AS `Tồn tối thiểu`,
                             GREATEST(i.MinStock * 2 - i.QuantityInStock, 0) AS `Đề xuất nhập thêm`,
                             i.QuantityInStock * p.CostPrice AS `Giá trị tồn`,
                             (SELECT IFNULL(SUM(oi.Quantity), 0) FROM OrderItems oi JOIN Orders o ON o.Id = oi.OrderId
                               WHERE oi.ProductId = p.Id AND o.Status = 'Paid' AND o.OrderDate >= CURDATE() - INTERVAL 30 DAY) AS `Đã bán 30 ngày`,
                             CASE WHEN i.QuantityInStock = 0 THEN 'Hết hàng'
                                  WHEN i.QuantityInStock <= i.MinStock THEN 'Sắp hết' ELSE 'Đủ hàng' END AS `Trạng thái`
                      FROM Inventory i JOIN Products p ON p.Id = i.ProductId JOIN Categories c ON c.Id = p.CategoryId
                      WHERE p.IsActive = 1 AND (@all = 1 OR i.QuantityInStock <= i.MinStock)
                      ORDER BY i.QuantityInStock - i.MinStock, p.ProductCode",
                    ("@all", sAll.Checked ? 1 : 0));
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        // =============== 3. KHÁCH HÀNG ===============
        private TabPage BuildCustomerTab()
        {
            var page = new TabPage("Khách hàng") { BackColor = Theme.Back };
            cFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
            cTo.Value = DateTime.Today;
            cTop.Minimum = 1; cTop.Value = 10;
            var bar = Ui.Toolbar();
            var btnView = Ui.Btn("Xem", BtnKind.Primary, 90);
            var btnCsv = Ui.Btn("Xuất Excel", BtnKind.Accent, 110);
            bar.Controls.AddRange(new Control[] { Ui.Lbl("Từ:"), cFrom, Ui.Lbl("Đến:"), cTo, Ui.Lbl("Top:"), cTop, btnView, btnCsv });
            var wrap = Ui.Card();
            wrap.Controls.Add(cGrid);
            Ui.Stack(page, wrap, new Control[] { bar, Ui.SectionTitle("🏆 Top khách hàng có tổng giá trị hóa đơn cao nhất") });
            btnView.Click += (s, e) => LoadCustomers();
            btnCsv.Click += (s, e) => Ui.ExportCsv(cGrid, "TopKhachHang");
            cGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex is >= 0 and < 3) e.CellStyle!.Font = Theme.Bold;
            };
            return page;
        }

        private void LoadCustomers()
        {
            if (cFrom.Value.Date > cTo.Value.Date) { Ui.Warn("Ngày bắt đầu phải trước ngày kết thúc."); return; }
            try
            {
                cGrid.DataSource = Database.Query(
                    @"SELECT ROW_NUMBER() OVER (ORDER BY SUM(o.TotalAmount) DESC) AS `Hạng`,
                             c.CustomerName AS `Khách hàng`, c.Phone AS `Số điện thoại`,
                             COUNT(o.Id) AS `Số hóa đơn`, SUM(o.TotalAmount) AS `Tổng chi tiêu`,
                             MAX(o.TotalAmount) AS `Hóa đơn cao nhất`, ROUND(AVG(o.TotalAmount), 0) AS `Trung bình / đơn`,
                             MAX(o.OrderDate) AS `Mua gần nhất`
                      FROM Orders o JOIN Customers c ON c.Id = o.CustomerId
                      WHERE o.Status = 'Paid' AND o.OrderDate >= @from AND o.OrderDate < @to
                      GROUP BY c.Id, c.CustomerName, c.Phone
                      ORDER BY SUM(o.TotalAmount) DESC
                      LIMIT @top",
                    ("@from", cFrom.Value.Date), ("@to", cTo.Value.Date.AddDays(1)), ("@top", (int)cTop.Value));
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private static Control Card(string title, Label value, Color color)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = color, Margin = new Padding(5), Padding = new Padding(12, 8, 12, 6) };
            var t = new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Color.FromArgb(250, 238, 242), Font = Theme.Small };
            value.Dock = DockStyle.Fill;
            value.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            value.ForeColor = Color.White;
            value.TextAlign = ContentAlignment.MiddleLeft;
            p.Controls.Add(value);
            p.Controls.Add(t);
            return p;
        }
    }

    /// <summary>Biểu đồ cột đơn giản (doanh thu + lợi nhuận), tự vẽ bằng GDI+ - không cần thư viện.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class BarChart : Panel
    {
        private List<(string Label, decimal Revenue, decimal Profit)> data = new();
        private string title = "";

        public BarChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            ResizeRedraw = true;
        }

        public void SetData(List<(string, decimal, decimal)> points, string chartTitle)
        {
            data = points;
            title = chartTitle;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fTitle = new Font("Segoe UI", 11, FontStyle.Bold);
            using var fSmall = new Font("Segoe UI", 8);
            using var bRev = new SolidBrush(Theme.Primary);
            using var bProfit = new SolidBrush(Theme.Accent);
            using var grid = new Pen(Theme.Border);

            g.DrawString(title, fTitle, bRev, 12, 8);
            // chú thích
            int lx = Width - 260;
            g.FillRectangle(bRev, lx, 12, 14, 14); g.DrawString("Doanh thu", Font, Brushes.Black, lx + 18, 9);
            g.FillRectangle(bProfit, lx + 120, 12, 14, 14); g.DrawString("Lợi nhuận", Font, Brushes.Black, lx + 138, 9);

            if (data.Count == 0)
            {
                g.DrawString("Không có dữ liệu trong khoảng thời gian đã chọn.", Font, Brushes.Gray, 12, 40);
                return;
            }

            var area = new Rectangle(80, 42, Width - 100, Height - 82);
            if (area.Width < 50 || area.Height < 50) return;
            decimal max = data.Max(d => Math.Max(d.Revenue, d.Profit));
            if (max <= 0) max = 1;
            decimal step = NiceStep(max / 5);
            decimal top = Math.Ceiling(max / step) * step;

            for (decimal v = 0; v <= top; v += step)
            {
                float y = area.Bottom - (float)(v / top) * area.Height;
                g.DrawLine(grid, area.Left, y, area.Right, y);
                var label = v >= 1_000_000 ? (v / 1_000_000).ToString("0.#", Fmt.VN) + " tr" : Fmt.Num(v);
                var sz = g.MeasureString(label, fSmall);
                g.DrawString(label, fSmall, Brushes.DimGray, area.Left - sz.Width - 6, y - sz.Height / 2);
            }

            float slot = (float)area.Width / data.Count;
            float barW = Math.Min(40, slot * 0.35f);
            int labelEvery = Math.Max(1, (int)Math.Ceiling(data.Count / (area.Width / 70f)));
            for (int i = 0; i < data.Count; i++)
            {
                float cx = area.Left + slot * i + slot / 2;
                float hRev = (float)(Math.Max(0, data[i].Revenue) / top) * area.Height;
                float hPro = (float)(Math.Max(0, data[i].Profit) / top) * area.Height;
                g.FillRectangle(bRev, cx - barW, area.Bottom - hRev, barW, hRev);
                g.FillRectangle(bProfit, cx, area.Bottom - hPro, barW, hPro);
                if (i % labelEvery == 0)
                {
                    var sz = g.MeasureString(data[i].Label, fSmall);
                    g.DrawString(data[i].Label, fSmall, Brushes.Black, cx - sz.Width / 2, area.Bottom + 4);
                }
            }
            g.DrawLine(Pens.Gray, area.Left, area.Bottom, area.Right, area.Bottom);
        }

        private static decimal NiceStep(decimal raw)
        {
            if (raw <= 0) return 1;
            double exp = Math.Pow(10, Math.Floor(Math.Log10((double)raw)));
            double f = (double)raw / exp;
            double nice = f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10;
            return (decimal)(nice * exp);
        }
    }
}
