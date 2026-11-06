namespace QuanLyBanHang
{
    /// <summary>Quản lý người dùng (chỉ Quản lý): tạo tài khoản, phân quyền, khóa, đặt lại mật khẩu.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class UserForm : AppForm
    {
        private readonly DataGridView grid = Ui.Grid();

        public UserForm() : base("Quản lý người dùng", 980, 560)
        {
            var bar = Ui.Toolbar();
            var btnAdd = Ui.Btn("+ Thêm", BtnKind.Primary, 100);
            var btnEdit = Ui.Btn("✎ Sửa", BtnKind.Primary, 100);
            var btnReset = Ui.Btn("Đặt lại mật khẩu", BtnKind.Secondary, 160);
            var btnLock = Ui.Btn("Khóa / Mở khóa", BtnKind.Danger, 150);
            bar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnReset, btnLock });

            var wrap = Ui.Card();
            wrap.Controls.Add(grid);
            Ui.Stack(this, wrap, new Control[] { Ui.Header("QUẢN LÝ NGƯỜI DÙNG", "Tài khoản và phân quyền"), bar });

            btnAdd.Click += (s, e) => Edit(null);
            btnEdit.Click += (s, e) => { if (Ui.SelectedId(grid) is int id) Edit(id); else Ui.Warn("Chọn tài khoản cần sửa."); };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && Ui.SelectedId(grid) is int id) Edit(id); };
            btnReset.Click += (s, e) =>
            {
                if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn tài khoản."); return; }
                using var dlg = new ChangePasswordDialog(id, false);
                dlg.ShowDialog(Ui.Owner(this));
            };
            btnLock.Click += (s, e) => ToggleLock();
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "Trạng thái" && e.Value?.ToString() == "Đã khóa")
                    e.CellStyle!.ForeColor = Theme.Danger;
            };
            Load += (s, e) => LoadData();
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                grid.DataSource = Database.Query(
                    @"SELECT u.Id, u.Username AS `Tên đăng nhập`, u.FullName AS `Họ tên`,
                             IF(u.Role = 'Admin', 'Quản lý', 'Nhân viên bán hàng') AS `Chức vụ`,
                             IF(u.IsActive, 'Hoạt động', 'Đã khóa') AS `Trạng thái`,
                             (SELECT COUNT(*) FROM Orders o WHERE o.UserId = u.Id AND o.Status = 'Paid') AS `Số HĐ đã lập`,
                             u.CreatedAt AS `Ngày tạo`
                      FROM Users u ORDER BY u.Role, u.Username");
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        private void Edit(int? id)
        {
            using var dlg = new UserEditDialog(id);
            if (dlg.ShowDialog(Ui.Owner(this)) == DialogResult.OK) LoadData();
        }

        private void ToggleLock()
        {
            if (Ui.SelectedId(grid) is not int id) { Ui.Warn("Chọn tài khoản."); return; }
            if (id == Session.UserId) { Ui.Warn("Không thể tự khóa tài khoản đang đăng nhập."); return; }
            bool locked = grid.CurrentRow!.Cells["Trạng thái"].Value?.ToString() == "Đã khóa";
            if (!Ui.Confirm(locked ? "Mở khóa tài khoản này?" : "Khóa tài khoản này? Người dùng sẽ không đăng nhập được nữa.")) return;
            try
            {
                Database.Execute("UPDATE Users SET IsActive = @a WHERE Id = @id", ("@a", locked), ("@id", id));
                LoadData();
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }
    }

    [System.ComponentModel.DesignerCategory("Code")]

    internal class UserEditDialog : DialogBase
    {
        private readonly int? userId;
        private readonly TextBox txtUser = Ui.Txt();
        private readonly TextBox txtName = Ui.Txt();
        private readonly TextBox txtPass = Ui.Txt();
        private readonly TextBox txtPass2 = Ui.Txt();
        private readonly ComboBox cboRole = Ui.Combo();

        public UserEditDialog(int? id) : base(id == null ? "Thêm người dùng" : "Sửa người dùng")
        {
            userId = id;
            txtPass.UseSystemPasswordChar = txtPass2.UseSystemPasswordChar = true;
            cboRole.Items.AddRange(new object[] { "Quản lý", "Nhân viên bán hàng" });
            cboRole.SelectedIndex = 1;
            AddField("Tên đăng nhập *", txtUser);
            AddField("Họ tên *", txtName);
            AddField("Chức vụ", cboRole);
            if (id == null)
            {
                AddField("Mật khẩu *", txtPass);
                AddField("Nhập lại mật khẩu", txtPass2);
            }
            else
            {
                txtUser.ReadOnly = true;
                txtUser.BackColor = Theme.PrimaryLight;
                Load += (s, e) => LoadData();
            }
            EndInit();
        }

        private void LoadData()
        {
            try
            {
                var dt = Database.Query("SELECT Username, FullName, Role FROM Users WHERE Id = @id", ("@id", userId));
                if (dt.Rows.Count == 0) return;
                txtUser.Text = dt.Rows[0]["Username"].ToString();
                txtName.Text = dt.Rows[0]["FullName"].ToString();
                cboRole.SelectedIndex = dt.Rows[0]["Role"].ToString() == "Admin" ? 0 : 1;
            }
            catch (Exception ex) { Ui.DbError(ex); }
        }

        protected override bool OnSave()
        {
            string user = txtUser.Text.Trim(), name = txtName.Text.Trim();
            string role = cboRole.SelectedIndex == 0 ? "Admin" : "Staff";
            if (user == "" || name == "") { Ui.Warn("Vui lòng nhập tên đăng nhập và họ tên."); return false; }
            if (userId == Session.UserId && role != "Admin") { Ui.Warn("Không thể tự hạ quyền tài khoản đang đăng nhập."); return false; }
            if (userId == null)
            {
                if (user.Contains(' ')) { Ui.Warn("Tên đăng nhập không được có dấu cách."); return false; }
                if (txtPass.Text.Length < 6) { Ui.Warn("Mật khẩu phải có ít nhất 6 ký tự."); return false; }
                if (txtPass.Text != txtPass2.Text) { Ui.Warn("Mật khẩu nhập lại không khớp."); return false; }
                int dup = Database.ToInt(Database.Scalar("SELECT COUNT(*) FROM Users WHERE Username = @u", ("@u", user)));
                if (dup > 0) { Ui.Warn("Tên đăng nhập đã tồn tại."); return false; }
                Database.Execute("INSERT INTO Users (Username, Password, FullName, Role) VALUES (@u, @p, @n, @r)",
                    ("@u", user), ("@p", Security.Hash(txtPass.Text)), ("@n", name), ("@r", role));
            }
            else
            {
                Database.Execute("UPDATE Users SET FullName = @n, Role = @r WHERE Id = @id", ("@n", name), ("@r", role), ("@id", userId));
                if (userId == Session.UserId) Session.FullName = name;
            }
            return true;
        }
    }

    /// <summary>Đổi mật khẩu (requireOld = true) hoặc quản lý đặt lại mật khẩu (requireOld = false).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class ChangePasswordDialog : DialogBase
    {
        private readonly int userId;
        private readonly bool requireOld;
        private readonly TextBox txtOld = Ui.Txt();
        private readonly TextBox txtNew = Ui.Txt();
        private readonly TextBox txtNew2 = Ui.Txt();

        public ChangePasswordDialog(int userId, bool requireOld) : base(requireOld ? "Đổi mật khẩu" : "Đặt lại mật khẩu", 460)
        {
            this.userId = userId;
            this.requireOld = requireOld;
            txtOld.UseSystemPasswordChar = txtNew.UseSystemPasswordChar = txtNew2.UseSystemPasswordChar = true;
            if (requireOld) AddField("Mật khẩu hiện tại", txtOld);
            AddField("Mật khẩu mới", txtNew);
            AddField("Nhập lại mật khẩu", txtNew2);
            EndInit();
        }

        protected override bool OnSave()
        {
            if (requireOld)
            {
                var cur = Database.Scalar("SELECT Password FROM Users WHERE Id = @id", ("@id", userId))?.ToString();
                if (cur != Security.Hash(txtOld.Text)) { Ui.Warn("Mật khẩu hiện tại không đúng."); return false; }
            }
            if (txtNew.Text.Length < 6) { Ui.Warn("Mật khẩu mới phải có ít nhất 6 ký tự."); return false; }
            if (txtNew.Text != txtNew2.Text) { Ui.Warn("Mật khẩu nhập lại không khớp."); return false; }
            Database.Execute("UPDATE Users SET Password = @p WHERE Id = @id", ("@p", Security.Hash(txtNew.Text)), ("@id", userId));
            Ui.Info("Đã cập nhật mật khẩu.");
            return true;
        }
    }
}
