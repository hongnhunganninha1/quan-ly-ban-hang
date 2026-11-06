namespace QuanLyBanHang
{
    /// <summary>Đăng nhập: nhập tài khoản, mật khẩu, chọn chức vụ (theo sơ đồ sequence "Đăng nhập").</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class LoginForm : AppForm
    {
        private readonly TextBox txtUser;
        private readonly TextBox txtPass;
        private readonly CheckBox chkShow;

        public LoginForm() : base("Đăng nhập", 880, 540)
        {
            Text = "Đăng nhập - Twenty Five Shop";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            // ----- nửa trái: thương hiệu -----
            var brand = new Panel { Dock = DockStyle.Left, Width = 390, BackColor = Theme.Primary };
            brand.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // họa tiết vòng tròn trang trí
                using (var c1 = new SolidBrush(Color.FromArgb(28, 255, 255, 255))) g.FillEllipse(c1, -120, 300, 360, 360);
                using (var c2 = new SolidBrush(Color.FromArgb(22, 193, 154, 62))) g.FillEllipse(c2, 230, -90, 260, 260);
                using (var path = Ui.RoundRect(new Rectangle(48, 120, 76, 76), 16)) g.FillPath(Brushes.White, path);
                using var f25 = new Font("Georgia", 28F, FontStyle.Bold);
                TextRenderer.DrawText(g, "25", f25, new Rectangle(48, 120, 76, 76), Theme.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                using var fName = new Font("Georgia", 26F, FontStyle.Bold);
                TextRenderer.DrawText(g, "TWENTY FIVE", fName, new Point(44, 214), Color.White);
                using var fSub = new Font("Segoe UI", 11F);
                TextRenderer.DrawText(g, "Hệ thống quản lý bán hàng\ncửa hàng thời trang", fSub, new Rectangle(48, 266, 300, 60), Color.FromArgb(244, 222, 230), TextFormatFlags.WordBreak);
                using var gold = new SolidBrush(Theme.Accent);
                g.FillRectangle(gold, 48, 336, 56, 4);
                TextRenderer.DrawText(g, "© 2026 Twenty Five Fashion", Theme.Small, new Point(48, 490), Color.FromArgb(220, 190, 202));
            };

            // ----- nửa phải: form đăng nhập -----
            int x = 450, w = 380;
            var title = new Label { Text = "Đăng nhập", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(x - 4, 50) };
            var sub = new Label { Text = "Chào mừng trở lại! Vui lòng nhập thông tin tài khoản.", ForeColor = Theme.Muted, AutoSize = true, Location = new Point(x, 98) };

            var lblUser = Ui.Lbl("Tên đăng nhập", true); lblUser.Location = new Point(x, 142);
            txtUser = Ui.Txt(w, "Nhập tên đăng nhập"); txtUser.Location = new Point(x, 168);
            var lblPass = Ui.Lbl("Mật khẩu", true); lblPass.Location = new Point(x, 212);
            txtPass = Ui.Txt(w, "Nhập mật khẩu"); txtPass.Location = new Point(x, 238);
            txtPass.UseSystemPasswordChar = true;
            chkShow = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true, Location = new Point(x, 272), ForeColor = Theme.Muted };
            chkShow.CheckedChanged += (s, e) => txtPass.UseSystemPasswordChar = !chkShow.Checked;

            foreach (var c in new Control[] { txtUser, txtPass }) c.Font = new Font("Segoe UI", 11F);
            var hint = new Label
            {
                Text = "Hệ thống tự nhận chức vụ (Quản lý / Nhân viên) theo tài khoản.",
                ForeColor = Theme.Muted, Font = Theme.Small, AutoSize = true, Location = new Point(x, 306),
            };

            var btnLogin = Ui.Btn("ĐĂNG NHẬP", BtnKind.Primary, w, 46);
            btnLogin.Location = new Point(x, 346);
            btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnLogin.Click += (s, e) => DoLogin();
            var btnExit = Ui.Btn("Thoát", BtnKind.Secondary, w, 40);
            btnExit.Location = new Point(x, 402);
            btnExit.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { title, sub, lblUser, txtUser, lblPass, txtPass, chkShow, hint, btnLogin, btnExit });
            Controls.Add(brand);
            AcceptButton = btnLogin;
            CancelButton = btnExit;
            Shown += (s, e) => txtUser.Focus();
            EndInit();
        }

        private void DoLogin()
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;
            if (user == "") { Ui.Warn("Vui lòng nhập tên đăng nhập!"); txtUser.Focus(); return; }
            if (pass == "") { Ui.Warn("Vui lòng nhập mật khẩu!"); txtPass.Focus(); return; }

            try
            {
                var dt = Database.Query(
                    "SELECT Id, Username, Password, FullName, Role, IsActive FROM Users WHERE Username = @u",
                    ("@u", user));

                if (dt.Rows.Count == 0 || dt.Rows[0]["Password"].ToString() != Security.Hash(pass))
                {
                    Ui.Error("Tên đăng nhập hoặc mật khẩu không đúng!", "Đăng nhập thất bại");
                    txtPass.Clear(); txtPass.Focus();
                    return;
                }
                var row = dt.Rows[0];
                if (!Convert.ToBoolean(row["IsActive"]))
                {
                    Ui.Warn("Tài khoản đã bị khóa. Vui lòng liên hệ quản lý.");
                    return;
                }
                string role = row["Role"].ToString() ?? "Staff";

                Session.UserId = Convert.ToInt32(row["Id"]);
                Session.Username = user;
                Session.FullName = row["FullName"].ToString() ?? user;
                Session.Role = role;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Ui.Error("Không kết nối được CSDL. Kiểm tra MySQL đã chạy và mật khẩu trong file Database.cs.\n\nChi tiết: " + ex.Message);
            }
        }
    }
}
