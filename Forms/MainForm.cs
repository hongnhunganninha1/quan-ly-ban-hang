using System.Data;
using System.Drawing.Drawing2D;

namespace QuanLyBanHang
{
    /// <summary>
    /// Màn hình chính kiểu dashboard: menu dọc bên trái + thanh tiêu đề trên (thông báo, tài khoản)
    /// + nội dung từng chức năng mở ngay ở khung bên phải.
    /// Quản lý (Admin) vào trang Tổng quan; Nhân viên vào thẳng màn Lập hóa đơn với menu rút gọn.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class MainForm : AppForm
    {
        public bool LoggedOut { get; private set; }

        private readonly Panel sidebar = new() { Dock = DockStyle.Left, Width = 236, BackColor = Theme.Sidebar };
        private readonly Panel content = new() { Dock = DockStyle.Fill, BackColor = Theme.Back };
        private readonly Label lblPage = new();
        private readonly Label lblPageSub = new();
        private readonly List<NavButton> navButtons = new();
        private readonly BellButton bell = new();
        private readonly UserBadge user = new();
        private Control? current;
        private string currentKey = "";

        public MainForm() : base("Màn hình chính", 1280, 780)
        {
            Text = "Twenty Five Shop - Hệ thống quản lý bán hàng";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1100, 680);
            BuildSidebar();

            // ----- thanh tiêu đề trên -----
            var topbar = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.White };
            topbar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, topbar.Height - 1, topbar.Width, topbar.Height - 1);
            };
            var titleBox = new Panel { Dock = DockStyle.Fill, Padding = new Padding(26, 10, 0, 6) };
            lblPage.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPage.ForeColor = Theme.Text;
            lblPage.Dock = DockStyle.Top;
            lblPage.Height = 36;
            lblPage.UseMnemonic = false;
            lblPage.AutoEllipsis = true;
            lblPageSub.Font = Theme.Small;
            lblPageSub.ForeColor = Theme.Muted;
            lblPageSub.Dock = DockStyle.Top;
            lblPageSub.Height = 24;
            lblPageSub.UseMnemonic = false;
            titleBox.Controls.Add(lblPageSub);
            titleBox.Controls.Add(lblPage);

            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 360, FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
                Padding = new Padding(0, 13, 18, 0),
            };
            user.Size = new Size(250, 50);
            bell.Size = new Size(50, 50);
            bell.Margin = new Padding(0, 0, 8, 0);
            right.Controls.Add(user);
            right.Controls.Add(bell);
            topbar.Controls.Add(titleBox);
            topbar.Controls.Add(right);
            bell.Click += (s, e) => ShowNotifications();
            user.Click += (s, e) => ShowUserMenu();

            Controls.Add(content);
            Controls.Add(topbar);
            Controls.Add(sidebar);

            KeyDown += (s, e) => { if (e.KeyCode == Keys.F2) { Navigate("sales"); e.Handled = true; } };
            Load += (s, e) => Navigate(Session.IsAdmin ? "home" : "sales");
            EndInit();
        }

        // ================= MENU DỌC =================
        private void BuildSidebar()
        {
            bool admin = Session.IsAdmin;
            var logo = new LogoPanel { Dock = DockStyle.Top, Height = 84 };

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(11, 10, 11, 0),
            };
            void Item(string key, string icon, string text)
            {
                var b = new NavButton(key, icon, text);
                b.Click += (s, e) => Navigate(key);
                navButtons.Add(b);
                nav.Controls.Add(b);
            }
            Item("home", "◉", "Tổng quan");
            Item("sales", "🧾", "Lập hóa đơn");
            Item("orders", "📋", "Đơn hàng");
            Item("customers", "👥", "Khách hàng");
            if (admin) Item("products", "👗", "Sản phẩm");
            if (admin) Item("categories", "🗂", "Danh mục");
            Item("inventory", "📦", "Nhập hàng - Tồn kho");
            if (admin) Item("reports", "📊", "Báo cáo thống kê");
            if (admin) Item("users", "🔐", "Người dùng");

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(11, 8, 11, 8) };
            var logout = new NavButton("", "⎋", "Đăng xuất") { Dock = DockStyle.Fill };
            logout.Click += (s, e) => Logout();
            bottom.Controls.Add(logout);

            sidebar.Controls.Add(nav);
            sidebar.Controls.Add(bottom);
            sidebar.Controls.Add(logo);
        }

        // ================= CHUYỂN TRANG =================
        public void Navigate(string key)
        {
            Control page;
            string title, sub;
            try
            {
                switch (key)
                {
                    case "sales": page = new SalesForm(); title = "Lập hóa đơn"; sub = "Bán hàng, thanh toán và in hóa đơn"; break;
                    case "orders": page = new OrderListForm(); title = "Đơn hàng"; sub = "Tra cứu, xem chi tiết, in lại và hủy đơn"; break;
                    case "customers": page = new CustomerForm(); title = "Khách hàng"; sub = "Thông tin và lịch sử mua hàng"; break;
                    case "products": page = new ProductForm(); title = "Sản phẩm"; sub = "Thêm, sửa, xóa sản phẩm"; break;
                    case "categories": page = new CategoryForm(); title = "Danh mục sản phẩm"; sub = "Phân loại sản phẩm theo nhóm"; break;
                    case "inventory": page = new InventoryForm(); title = "Nhập hàng - Tồn kho"; sub = "Cập nhật số lượng, cảnh báo tồn thấp"; break;
                    case "inventory-low": page = new InventoryForm(true); key = "inventory"; title = "Nhập hàng - Tồn kho"; sub = "Các sản phẩm đang dưới ngưỡng tồn tối thiểu"; break;
                    case "reports": page = new ReportForm(0); title = "Báo cáo thống kê"; sub = "Doanh thu, lợi nhuận, tồn kho, khách hàng"; break;
                    case "users": page = new UserForm(); title = "Người dùng"; sub = "Tài khoản và phân quyền"; break;
                    default: page = new DashboardPage(Navigate); key = "home"; title = $"Xin chào, {Session.FullName}!"; sub = "Tổng quan hoạt động kinh doanh"; break;
                }
            }
            catch (Exception ex) { Ui.Error("Không mở được trang: " + ex.Message); return; }

            var old = current;
            if (page is Form f)
            {
                f.WindowState = FormWindowState.Normal;
                f.TopLevel = false;
                f.FormBorderStyle = FormBorderStyle.None;
                f.BackColor = Theme.Back;
                foreach (Control c in f.Controls)
                    if (Equals(c.Tag, "header")) c.Visible = false;   // tiêu đề đã có ở thanh trên
                // trang tự đóng (VD: nút Đóng, Esc) → quay về trang mặc định
                f.FormClosed += (s, e) => { if (current == f) BeginInvoke(() => Navigate(Session.IsAdmin ? "home" : "sales")); };
            }
            page.Dock = DockStyle.Fill;
            content.SuspendLayout();
            content.Controls.Add(page);
            current = page;
            currentKey = key;
            if (page is Form pf) pf.Show();
            page.BringToFront();
            content.ResumeLayout();

            if (old != null)
            {
                content.Controls.Remove(old);
                old.Dispose();
            }
            lblPage.Text = title;
            lblPageSub.Text = sub + "   •   " + DateTime.Now.ToString("dddd, dd/MM/yyyy", Fmt.VN);
            foreach (var b in navButtons) b.Active = b.Key == currentKey;
            RefreshBell();
            page.Focus();
        }

        // ================= THÔNG BÁO =================
        private DataTable lowStock = new();

        private void RefreshBell()
        {
            try
            {
                lowStock = Database.Query(
                    @"SELECT p.ProductCode, p.Name, i.QuantityInStock, i.MinStock
                      FROM Inventory i JOIN Products p ON p.Id = i.ProductId
                      WHERE p.IsActive = 1 AND i.QuantityInStock <= i.MinStock
                      ORDER BY i.QuantityInStock, p.ProductCode");
                bell.Count = lowStock.Rows.Count;
            }
            catch { bell.Count = 0; }
        }

        private void ShowNotifications()
        {
            RefreshBell();
            var menu = new ContextMenuStrip { Font = Theme.Base, ShowImageMargin = false };
            menu.Items.Add(new ToolStripLabel("THÔNG BÁO") { Font = Theme.Bold, ForeColor = Theme.Primary });
            menu.Items.Add(new ToolStripSeparator());
            if (lowStock.Rows.Count == 0)
                menu.Items.Add(new ToolStripLabel("✔  Không có thông báo mới. Tồn kho ổn định.") { ForeColor = Theme.Success });
            foreach (DataRow r in lowStock.Rows.Cast<DataRow>().Take(8))
            {
                int qty = Convert.ToInt32(r["QuantityInStock"]);
                string text = qty == 0
                    ? $"⛔  Hết hàng: {r["Name"]} ({r["ProductCode"]})"
                    : $"⚠  Sắp hết: {r["Name"]} - còn {qty} (tối thiểu {r["MinStock"]})";
                var it = new ToolStripMenuItem(text) { ForeColor = qty == 0 ? Theme.Danger : Theme.Warning };
                it.Click += (s, e) => Navigate("inventory-low");
                menu.Items.Add(it);
            }
            if (lowStock.Rows.Count > 8)
                menu.Items.Add(new ToolStripLabel($"... và {lowStock.Rows.Count - 8} sản phẩm khác") { ForeColor = Theme.Muted });
            if (lowStock.Rows.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
                var all = new ToolStripMenuItem("Xem tất cả và nhập hàng  →") { Font = Theme.Bold, ForeColor = Theme.Primary };
                all.Click += (s, e) => Navigate("inventory-low");
                menu.Items.Add(all);
            }
            menu.Closed += (s, e) => BeginInvoke(() => menu.Dispose());
            menu.Show(bell, new Point(bell.Width - menu.PreferredSize.Width, bell.Height + 4));
        }

        private void ShowUserMenu()
        {
            var menu = new ContextMenuStrip { Font = Theme.Base, ShowImageMargin = false };
            menu.Items.Add(new ToolStripLabel($"{Session.FullName}  ({Session.Username})") { Font = Theme.Bold });
            menu.Items.Add(new ToolStripLabel(Session.RoleText) { ForeColor = Theme.Muted });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("🔑  Đổi mật khẩu", null, (s, e) => { using var d = new ChangePasswordDialog(Session.UserId, true); d.ShowDialog(this); });
            menu.Items.Add("⎋  Đăng xuất", null, (s, e) => Logout());
            menu.Closed += (s, e) => BeginInvoke(() => menu.Dispose());
            menu.Show(user, new Point(user.Width - menu.PreferredSize.Width, user.Height + 4));
        }

        private void Logout()
        {
            if (!Ui.Confirm("Bạn có chắc muốn đăng xuất?")) return;
            LoggedOut = true;
            Close();
        }
    }

    /// <summary>Hỗ trợ vẽ theo tỉ lệ màn hình (100%, 125%, 150%...) để chữ không bị che.</summary>
    internal static class Dpi
    {
        public static int S(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96f);
        public static float F(Control c, float v) => v * c.DeviceDpi / 96f;
    }

    /// <summary>Logo ở đầu menu dọc.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class LogoPanel : Control
    {
        private static readonly Font f25 = new("Georgia", 16F, FontStyle.Bold);
        private static readonly Font fName = new("Georgia", 13F, FontStyle.Bold);

        public LogoPanel() { DoubleBuffered = true; BackColor = Theme.Sidebar; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int S(int v) => Dpi.S(this, v);
            int box = S(44), x = S(20), y = (Height - box) / 2;
            using (var path = Ui.RoundRect(new Rectangle(x, y, box, box), S(10))) g.FillPath(Brushes.White, path);
            TextRenderer.DrawText(g, "25", f25, new Rectangle(x, y, box, box), Theme.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            int tx = x + box + S(12);
            TextRenderer.DrawText(g, "TWENTY FIVE", fName, new Rectangle(tx, y - S(2), Width - tx, box / 2 + S(6)), Color.White,
                TextFormatFlags.Bottom | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Fashion Store", Theme.Small, new Rectangle(tx, y + box / 2 + S(4), Width - tx, box / 2), Color.FromArgb(230, 200, 212),
                TextFormatFlags.Top | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            using var line = new Pen(Theme.SidebarHover);
            g.DrawLine(line, S(16), Height - 1, Width - S(16), Height - 1);
        }
    }

    /// <summary>Nút menu dọc: icon + chữ, nền sáng hơn và vạch vàng khi đang chọn.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class NavButton : Control
    {
        private static readonly Font fIcon = new("Segoe UI Emoji", 12F);
        public string Key { get; }
        private readonly string icon, text;
        private bool hover, active;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Active { get => active; set { active = value; Invalidate(); } }

        public NavButton(string key, string icon, string text)
        {
            Key = key; this.icon = icon; this.text = text;
            Size = new Size(214, 44);
            Margin = new Padding(0, 2, 0, 2);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = Theme.Sidebar;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Sidebar);
            int S(int v) => Dpi.S(this, v);
            if (active || hover)
            {
                using var path = Ui.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), S(10));
                using var b = new SolidBrush(active ? Theme.SidebarActive : Theme.SidebarHover);
                g.FillPath(b, path);
            }
            if (active)
            {
                using var gold = new SolidBrush(Theme.Accent);
                g.FillRectangle(gold, 0, S(10), S(4), Height - S(20));
            }
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(g, icon, fIcon, new Rectangle(S(12), 0, S(30), Height), Color.White, flags | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, text, active ? Theme.Bold : Theme.Base, new Rectangle(S(52), 0, Width - S(56), Height),
                active ? Color.White : Color.FromArgb(242, 224, 231), flags | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Nút chuông thông báo, có chấm đỏ số lượng.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class BellButton : Control
    {
        private static readonly Font fBell = new("Segoe UI Emoji", 14F);
        private static readonly Font fBadge = new("Segoe UI", 7.5F, FontStyle.Bold);
        private int count;
        private bool hover;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Count { get => count; set { count = value; Invalidate(); } }

        public BellButton() { DoubleBuffered = true; BackColor = Color.White; Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int S(int v) => Dpi.S(this, v);
            int d = Math.Min(Width, Height) - S(4);
            var circle = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d);
            using (var b = new SolidBrush(hover ? Theme.PrimaryLight : Color.FromArgb(248, 243, 245))) g.FillEllipse(b, circle);
            TextRenderer.DrawText(g, "🔔", fBell, circle, Theme.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (count > 0)
            {
                string t = count > 9 ? "9+" : count.ToString();
                int bw = S(t.Length > 1 ? 24 : 19), bh = S(19);
                var badge = new Rectangle(Width - bw, 0, bw, bh);
                using var path = Ui.RoundRect(badge, bh / 2);
                using (var red = new SolidBrush(Theme.Danger)) g.FillPath(red, path);
                using var white = new Pen(Color.White, 2);
                g.DrawPath(white, path);
                TextRenderer.DrawText(g, t, fBadge, badge, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }

    /// <summary>Ảnh đại diện chữ cái + tên, chức vụ ở góc phải thanh trên (bấm để mở menu tài khoản).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class UserBadge : Control
    {
        private static readonly Font fInitial = new("Segoe UI", 13F, FontStyle.Bold);
        private bool hover;
        public UserBadge() { DoubleBuffered = true; BackColor = Color.White; Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int S(int v) => Dpi.S(this, v);
            if (hover)
            {
                using var path = Ui.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), S(10));
                using var hb = new SolidBrush(Color.FromArgb(248, 243, 245));
                g.FillPath(hb, path);
            }
            var parts = Session.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = parts.Length == 0 ? "?" : parts[^1][..1].ToUpper();
            int d = Height - S(8), x = Width - d - S(6), y = S(4);
            using (var b = new SolidBrush(Theme.Primary)) g.FillEllipse(b, x, y, d, d);
            TextRenderer.DrawText(g, initials, fInitial, new Rectangle(x, y, d, d), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            var flags = TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            int textW = x - S(10);
            TextRenderer.DrawText(g, Session.FullName + "  ▾", Theme.Bold, new Rectangle(0, 0, textW, Height / 2), Theme.Text, flags | TextFormatFlags.Bottom);
            TextRenderer.DrawText(g, Session.RoleText, Theme.Small, new Rectangle(0, Height / 2 + S(2), textW, Height / 2 - S(2)), Theme.Muted, flags | TextFormatFlags.Top);
        }
    }
}
