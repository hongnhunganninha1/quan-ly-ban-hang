using System.Data;
using System.Drawing.Drawing2D;

namespace QuanLyBanHang
{
    /// <summary>Trang Tổng quan: thẻ thống kê, biểu đồ doanh thu 7 ngày, top bán chạy, đơn hàng gần đây.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class DashboardPage : UserControl
    {
        private readonly Action<string> navigate;
        private readonly StatCard cRevenue, cOrders, cProducts, cCustomers, cLow;
        private readonly BarChart chart = new() { Dock = DockStyle.Fill };
        private readonly DataGridView gridTop = Ui.Grid();
        private readonly DataGridView gridRecent = Ui.Grid();

        public DashboardPage(Action<string> navigate)
        {
            this.navigate = navigate;
            BackColor = Theme.Back;
            Font = Theme.Base;
            Padding = new Padding(16, 10, 16, 8);
            bool admin = Session.IsAdmin;

            // ----- hàng thẻ thống kê -----
            var stats = new TableLayoutPanel { Dock = DockStyle.Top, Height = 124, ColumnCount = 5, RowCount = 1 };
            for (int i = 0; i < 5; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            stats.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cRevenue = new StatCard("Doanh thu tháng này", "💰", Theme.Primary);
            cOrders = new StatCard("Đơn hàng hôm nay", "🧾", Color.FromArgb(52, 120, 200));
            cProducts = new StatCard("Sản phẩm đang bán", "👗", Theme.Accent);
            cCustomers = new StatCard("Khách hàng", "👥", Theme.Success);
            cLow = new StatCard("Cảnh báo tồn thấp", "⚠", Theme.Danger);
            if (admin) cRevenue.Click += (s, e) => navigate("reports");
            cOrders.Click += (s, e) => navigate("orders");
            if (admin) cProducts.Click += (s, e) => navigate("products");
            cCustomers.Click += (s, e) => navigate("customers");
            cLow.Click += (s, e) => navigate("inventory-low");
            var cards = new[] { cRevenue, cOrders, cProducts, cCustomers, cLow };
            for (int i = 0; i < 5; i++) { cards[i].Dock = DockStyle.Fill; stats.Controls.Add(cards[i], i, 0); }

            // ----- biểu đồ + top bán chạy -----
            var middle = new TableLayoutPanel { Dock = DockStyle.Top, Height = 320, ColumnCount = 2, RowCount = 1 };
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            middle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            middle.Controls.Add(CardWith("Doanh thu & lợi nhuận 7 ngày gần nhất", chart), 0, 0);
            middle.Controls.Add(CardWith("🔥 Top sản phẩm bán chạy tháng này", gridTop), 1, 0);

            // ----- đơn hàng gần đây -----
            var recent = CardWith("Đơn hàng gần đây  (nhấp đúp để xem chi tiết)", gridRecent);
            recent.Dock = DockStyle.Fill;
            gridRecent.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || Ui.SelectedId(gridRecent) is not int id) return;
                using var dlg = new OrderDetailDialog(id);
                dlg.ShowDialog(Ui.Owner(this));
            };

            // ----- nút bán hàng nhanh -----
            var quick = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 4, 0, 0) };
            var bSale = Ui.Btn("🧾  Lập hóa đơn mới (F2)", BtnKind.Primary, 230, 40);
            var bStock = Ui.Btn("📦  Nhập hàng", BtnKind.Secondary, 150, 40);
            var bCus = Ui.Btn("👥  Thêm khách hàng", BtnKind.Secondary, 190, 40);
            bSale.Click += (s, e) => navigate("sales");
            bStock.Click += (s, e) => navigate("inventory");
            bCus.Click += (s, e) => { using var d = new CustomerEditDialog(null); if (d.ShowDialog(Ui.Owner(this)) == DialogResult.OK) LoadData(); };
            quick.Controls.AddRange(new Control[] { bSale, bStock, bCus });

            Controls.Add(recent);
            Controls.Add(middle);
            Controls.Add(stats);
            Controls.Add(quick);
            Load += (s, e) => LoadData();
        }

        private static Control CardWith(string title, Control inner)
        {
            var card = Ui.Card(22);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);
            var lbl = new Label
            {
                Text = title, Dock = DockStyle.Top, Height = 32, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.Text, BackColor = Color.White,
            };
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner);
            card.Controls.Add(lbl);
            return card;
        }

        private void LoadData()
        {
            try
            {
                var monthRev = Database.ToDecimal(Database.Scalar(
                    @"SELECT SUM(SubTotal - DiscountAmount) FROM Orders
                      WHERE Status = 'Paid' AND YEAR(OrderDate) = YEAR(CURDATE()) AND MONTH(OrderDate) = MONTH(CURDATE())"));
                var lastRev = Database.ToDecimal(Database.Scalar(
                    @"SELECT SUM(SubTotal - DiscountAmount) FROM Orders
                      WHERE Status = 'Paid' AND OrderDate >= DATE_FORMAT(CURDATE() - INTERVAL 1 MONTH, '%Y-%m-01')
                        AND OrderDate < DATE_FORMAT(CURDATE() - INTERVAL 1 MONTH, '%Y-%m-01') + INTERVAL DAY(CURDATE()) DAY"));
                cRevenue.Value = Fmt.Money(monthRev);
                cRevenue.Note = lastRev > 0
                    ? (monthRev >= lastRev ? "▲ " : "▼ ") + Math.Abs(Math.Round((monthRev - lastRev) * 100 / lastRev, 1)) + "% so với cùng kỳ tháng trước"
                    : "Tháng trước chưa có dữ liệu";

                var today = Database.Query(
                    "SELECT COUNT(*) AS N, IFNULL(SUM(TotalAmount), 0) AS T FROM Orders WHERE Status = 'Paid' AND DATE(OrderDate) = CURDATE()");
                cOrders.Value = Database.ToInt(today.Rows[0]["N"]) + " đơn";
                cOrders.Note = "Thu " + Fmt.Money(Database.ToDecimal(today.Rows[0]["T"])) + " hôm nay";

                cProducts.Value = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Products WHERE IsActive = 1")).ToString();
                cProducts.Note = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Categories WHERE IsActive = 1")) + " danh mục";
                cCustomers.Value = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Customers WHERE IsActive = 1")).ToString();
                cCustomers.Note = Database.ToInt(Database.Scalar(
                    "SELECT COUNT(DISTINCT CustomerId) FROM Orders WHERE Status = 'Paid' AND OrderDate >= CURDATE() - INTERVAL 30 DAY")) + " khách mua trong 30 ngày";
                int low = Database.ToInt(Database.Scalar(
                    @"SELECT COUNT(*) FROM Inventory i JOIN Products p ON p.Id = i.ProductId
                      WHERE p.IsActive = 1 AND i.QuantityInStock <= i.MinStock"));
                cLow.Value = low + " SP";
                cLow.Note = low > 0 ? "Bấm để xem và nhập thêm" : "Tồn kho ổn định";

                // biểu đồ 7 ngày (kể cả ngày không bán được)
                var dt = Database.Query(
                    @"SELECT DATE(o.OrderDate) AS D, SUM(o.SubTotal - o.DiscountAmount) AS Rev,
                             SUM(o.SubTotal - o.DiscountAmount - (SELECT IFNULL(SUM(oi.CostPrice * oi.Quantity), 0) FROM OrderItems oi WHERE oi.OrderId = o.Id)) AS Profit
                      FROM Orders o WHERE o.Status = 'Paid' AND o.OrderDate >= CURDATE() - INTERVAL 6 DAY
                      GROUP BY DATE(o.OrderDate)");
                var points = new List<(string, decimal, decimal)>();
                for (int i = 6; i >= 0; i--)
                {
                    var day = DateTime.Today.AddDays(-i);
                    var row = dt.AsEnumerable().FirstOrDefault(r => Convert.ToDateTime(r["D"]).Date == day);
                    points.Add((day.ToString("dd/MM"), row == null ? 0 : Database.ToDecimal(row["Rev"]), row == null ? 0 : Database.ToDecimal(row["Profit"])));
                }
                chart.SetData(points, "");

                gridTop.DataSource = Database.Query(
                    @"SELECT p.Name AS `Sản phẩm`, SUM(oi.Quantity) AS `Đã bán`, SUM(oi.LineTotal) AS `Doanh thu`
                      FROM OrderItems oi JOIN Orders o ON o.Id = oi.OrderId JOIN Products p ON p.Id = oi.ProductId
                      WHERE o.Status = 'Paid' AND YEAR(o.OrderDate) = YEAR(CURDATE()) AND MONTH(o.OrderDate) = MONTH(CURDATE())
                      GROUP BY p.Id, p.Name ORDER BY SUM(oi.Quantity) DESC LIMIT 6");

                gridRecent.DataSource = Database.Query(
                    @"SELECT o.Id, o.OrderCode AS `Mã hóa đơn`, o.OrderDate AS `Ngày lập`,
                             IFNULL(c.CustomerName, 'Khách lẻ') AS `Khách hàng`, u.FullName AS `Nhân viên`,
                             o.TotalAmount AS `Tổng tiền`, o.PaymentMethod AS `Thanh toán`,
                             IF(o.Status = 'Paid', 'Đã thanh toán', 'Đã hủy') AS `Trạng thái`
                      FROM Orders o LEFT JOIN Customers c ON c.Id = o.CustomerId JOIN Users u ON u.Id = o.UserId
                      ORDER BY o.OrderDate DESC LIMIT 20");
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }

    /// <summary>Thẻ thống kê trắng bo góc: icon tròn màu, tiêu đề, số liệu, ghi chú.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class StatCard : Control
    {
        private static readonly Font fTitle = new("Segoe UI", 9.5F);
        private static readonly Font fValue = new("Segoe UI", 17F, FontStyle.Bold);
        private static readonly Font fNote = new("Segoe UI", 8.5F);
        private static readonly Font fIcon = new("Segoe UI Emoji", 15F);
        private readonly string title, icon;
        private readonly Color color;
        private string value = "...", note = "";
        private bool hover;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Value { get => value; set { this.value = value; Invalidate(); } }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Note { get => note; set { note = value; Invalidate(); } }

        public StatCard(string title, string icon, Color color)
        {
            this.title = title; this.icon = icon; this.color = color;
            DoubleBuffered = true;
            Margin = new Padding(0);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            var r = new Rectangle(8, 8, Width - 17, Height - 17);
            if (r.Width < 20 || r.Height < 20) return;
            using (var path = Ui.RoundRect(r, 14))
            {
                using var sh = new Pen(Color.FromArgb(hover ? 40 : 18, 80, 20, 40), hover ? 4 : 3);
                g.DrawPath(sh, path);
                g.FillPath(Brushes.White, path);
                using var border = new Pen(hover ? color : Theme.Border);
                g.DrawPath(border, path);
            }
            // vạch màu bên trái
            using (var b = new SolidBrush(color)) g.FillRectangle(b, r.X + 1, r.Y + 16, 4, r.Height - 32);

            // icon tròn
            int d = 42, ix = r.Right - d - 14, iy = r.Y + 14;
            using (var light = new SolidBrush(Color.FromArgb(32, color))) g.FillEllipse(light, ix, iy, d, d);
            TextRenderer.DrawText(g, icon, fIcon, new Rectangle(ix, iy, d, d), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int tx = r.X + 18, tw = ix - tx - 6;
            TextRenderer.DrawText(g, title, fTitle, new Rectangle(tx, r.Y + 14, tw, 20), Theme.Muted, TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, value, fValue, new Rectangle(tx, r.Y + 36, r.Right - tx - 8, 36), Theme.Text, TextFormatFlags.EndEllipsis);
            var noteColor = note.StartsWith("▼") ? Theme.Danger : note.StartsWith("▲") ? Theme.Success : Theme.Muted;
            TextRenderer.DrawText(g, note, fNote, new Rectangle(tx, r.Bottom - 26, r.Right - tx - 8, 18), noteColor, TextFormatFlags.EndEllipsis);
        }
    }
}
