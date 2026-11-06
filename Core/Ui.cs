using System.Data;
using System.Text;

namespace QuanLyBanHang
{
    internal enum BtnKind { Primary, Secondary, Danger, Success, Accent }

    /// <summary>Các hàm tạo control có sẵn style tone đỏ đô, giúp mọi form nhìn đồng bộ.</summary>
    internal static class Ui
    {
        // ---------------- Thông báo ----------------
        public static void Info(string msg, string title = "Thông báo") =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void Warn(string msg, string title = "Cảnh báo") =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static void Error(string msg, string title = "Lỗi") =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static bool Confirm(string msg, string title = "Xác nhận") =>
            MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public static void DbError(Exception ex) =>
            Error("Có lỗi khi làm việc với cơ sở dữ liệu:\n" + ex.Message);

        // ---------------- Control cơ bản ----------------
        public static Button Btn(string text, BtnKind kind = BtnKind.Primary, int width = 120, int height = 36)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.Bold,
                Cursor = Cursors.Hand,
                Margin = new Padding(4),
                UseVisualStyleBackColor = false,
            };
            Color back, fore = Color.White, hover;
            switch (kind)
            {
                case BtnKind.Secondary:
                    back = Color.White; fore = Theme.Primary; hover = Theme.PrimaryLight;
                    b.FlatAppearance.BorderColor = Theme.Primary; b.FlatAppearance.BorderSize = 1;
                    break;
                case BtnKind.Danger:
                    back = Theme.Danger; hover = Color.FromArgb(150, 35, 35); b.FlatAppearance.BorderSize = 0;
                    break;
                case BtnKind.Success:
                    back = Theme.Success; hover = Color.FromArgb(32, 100, 66); b.FlatAppearance.BorderSize = 0;
                    break;
                case BtnKind.Accent:
                    back = Theme.Accent; hover = Color.FromArgb(165, 128, 45); b.FlatAppearance.BorderSize = 0;
                    break;
                default:
                    back = Theme.Primary; hover = Theme.PrimaryHover; b.FlatAppearance.BorderSize = 0;
                    break;
            }
            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = hover;
            return b;
        }

        public static Label Lbl(string text, bool bold = false, Color? color = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = bold ? Theme.Bold : Theme.Base,
                ForeColor = color ?? Theme.Text,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(4, 10, 4, 4),
            };
        }

        public static TextBox Txt(int width = 200, string placeholder = "")
        {
            return new TextBox
            {
                Width = width,
                Font = Theme.Base,
                PlaceholderText = placeholder,
                Margin = new Padding(4, 6, 4, 4),
                BorderStyle = BorderStyle.FixedSingle,
            };
        }

        public static NumericUpDown Num(decimal max = 1_000_000_000, int width = 140, int decimals = 0, decimal min = 0)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                ThousandsSeparator = true,
                Width = width,
                Font = Theme.Base,
                Margin = new Padding(4, 6, 4, 4),
                TextAlign = HorizontalAlignment.Right,
            };
        }

        public static ComboBox Combo(int width = 200)
        {
            return new ComboBox
            {
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.Base,
                Margin = new Padding(4, 6, 4, 4),
                FlatStyle = FlatStyle.Flat,
            };
        }

        public static DateTimePicker Date(int width = 130)
        {
            return new DateTimePicker
            {
                Width = width,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Font = Theme.Base,
                Margin = new Padding(4, 6, 4, 4),
            };
        }

        /// <summary>Thanh tiêu đề màu đỏ đô ở đầu mỗi form.</summary>
        public static Panel Header(string title, string? subtitle = null, int height = 60)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Theme.Primary, Padding = new Padding(18, 0, 18, 0), Tag = "header" };
            var lbl = new Label
            {
                Text = title,
                Font = Theme.Title,
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            p.Controls.Add(lbl);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = new Label
                {
                    Text = subtitle,
                    Font = Theme.Base,
                    ForeColor = Color.FromArgb(240, 220, 228),
                    AutoSize = false,
                    Width = 420,
                    Dock = DockStyle.Right,
                    TextAlign = ContentAlignment.MiddleRight,
                };
                p.Controls.Add(sub);
            }
            // đường viền vàng đồng mảnh bên dưới
            var line = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Theme.Accent };
            p.Controls.Add(line);
            return p;
        }

        /// <summary>Thanh công cụ (chứa ô tìm kiếm, nút...) xếp ngang, tự xuống dòng.</summary>
        public static FlowLayoutPanel Toolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 8, 10, 6),
                BackColor = Theme.Back,
                WrapContents = true,
            };
        }

        public static Label SectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(12, 8, 0, 0),
                BackColor = Theme.Back,
            };
        }

        /// <summary>Khung thẻ trắng bo góc chứa bảng / nội dung.</summary>
        public static CardPanel Card(int padding = 20) => new CardPanel { Padding = new Padding(padding) };

        /// <summary>Cửa sổ chủ (top-level) để mở hộp thoại, kể cả khi form đang nằm trong màn hình chính.</summary>
        public static IWin32Window Owner(Control c) => (IWin32Window?)c.TopLevelControl ?? c;

        /// <summary>Bảng dữ liệu đã style sẵn: header đỏ đô, dòng xen kẽ, tự định dạng tiền.</summary>
        public static DataGridView Grid(bool readOnly = true)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = readOnly,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                Font = Theme.Base,
            };
            g.RowTemplate.Height = 36;
            g.ColumnHeadersHeight = 42;
            g.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeader;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Primary;
            g.ColumnHeadersDefaultCellStyle.Font = Theme.Bold;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.GridHeader;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Primary;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            g.DefaultCellStyle.SelectionBackColor = Theme.PrimaryLight;
            g.DefaultCellStyle.SelectionForeColor = Theme.Primary;
            g.DefaultCellStyle.ForeColor = Theme.Text;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(253, 251, 252);
            // đường kẻ mảnh dưới tiêu đề cột
            g.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 2);
                e.Graphics.DrawLine(pen, 0, g.ColumnHeadersHeight - 1, g.Width, g.ColumnHeadersHeight - 1);
            };
            g.DataBindingComplete += (s, e) => FormatColumns(g);
            return g;
        }

        /// <summary>Ẩn cột Id, định dạng cột tiền và ngày.</summary>
        public static void FormatColumns(DataGridView g)
        {
            foreach (DataGridViewColumn c in g.Columns)
            {
                if (c.Name == "Id" || c.Name.EndsWith("Id")) { c.Visible = false; continue; }
                var t = c.ValueType;
                if (t == typeof(decimal) || t == typeof(double) || t == typeof(long) || t == typeof(int))
                {
                    if (t == typeof(decimal) || t == typeof(double))
                        c.DefaultCellStyle.Format = c.HeaderText.Contains('%') ? "N1" : "N0";
                    c.DefaultCellStyle.FormatProvider = Fmt.VN;
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (t == typeof(DateTime))
                {
                    c.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
            }
        }

        /// <summary>
        /// Sắp xếp control trong form theo thứ tự: các thanh trên cùng (tops), phần chính (fill), các thanh dưới/bên cạnh (bottoms,
        /// mặc định Dock Bottom, hoặc đặt sẵn Dock Right/Left).
        /// Tránh lỗi control bị che/lệch do thứ tự Dock của WinForms.
        /// </summary>
        public static void Stack(Control parent, Control fill, Control[] tops, Control[]? bottoms = null)
        {
            fill.Dock = DockStyle.Fill;
            parent.Controls.Add(fill);
            if (bottoms != null)
                foreach (var b in bottoms) { if (b.Dock == DockStyle.None) b.Dock = DockStyle.Bottom; parent.Controls.Add(b); }
            for (int i = tops.Length - 1; i >= 0; i--)
            {
                if (tops[i].Dock == DockStyle.None) tops[i].Dock = DockStyle.Top;
                parent.Controls.Add(tops[i]);
            }
        }

        /// <summary>Xuất dữ liệu đang hiển thị trên bảng ra file CSV (mở được bằng Excel).</summary>
        public static void ExportCsv(DataGridView g, string fileName)
        {
            if (g.Rows.Count == 0) { Warn("Không có dữ liệu để xuất."); return; }
            using var dlg = new SaveFileDialog
            {
                Filter = "File CSV (Excel)|*.csv",
                FileName = fileName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var cols = g.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", cols.Select(c => Csv(c.HeaderText))));
            foreach (DataGridViewRow r in g.Rows)
                sb.AppendLine(string.Join(",", cols.Select(c => Csv(r.Cells[c.Index].FormattedValue?.ToString() ?? ""))));
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            Info("Đã xuất file:\n" + dlg.FileName);
        }

        private static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

        /// <summary>Đọc ảnh mà không khóa file.</summary>
        public static Image? LoadImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var full = Path.IsPathRooted(path) ? path : Path.Combine(Application.StartupPath, path);
            if (!File.Exists(full)) return null;
            try
            {
                using var fs = new FileStream(full, FileMode.Open, FileAccess.Read);
                using var img = Image.FromStream(fs);
                return new Bitmap(img);
            }
            catch { return null; }
        }

        public static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static int? SelectedId(DataGridView g, string column = "Id")
        {
            if (g.CurrentRow == null || g.CurrentRow.Index < 0) return null;
            var v = g.CurrentRow.Cells[column].Value;
            return v == null || v == DBNull.Value ? null : Convert.ToInt32(v);
        }
    }

    /// <summary>Panel vẽ nền thẻ trắng bo góc (cách mép 8px), nội dung đặt bên trong Padding.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class CardPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 14;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Inset { get; set; } = 8;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color CardColor { get; set; } = Color.White;

        public CardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? Theme.Back);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int inset = (int)(Inset * DeviceDpi / 96f), radius = (int)(Radius * DeviceDpi / 96f);
            var r = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);
            if (r.Width <= 0 || r.Height <= 0) return;
            using var path = Ui.RoundRect(r, radius);
            using var shadow = new Pen(Color.FromArgb(18, 80, 20, 40), 3);
            e.Graphics.DrawPath(shadow, path);
            using var b = new SolidBrush(CardColor);
            e.Graphics.FillPath(b, path);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawPath(pen, path);
        }
    }

    /// <summary>Form gốc: font, màu nền, tự co giãn theo DPI màn hình.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class AppForm : Form
    {
        public AppForm(string title, int width, int height)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Base;
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Text = title + " - Twenty Five Shop";
            ClientSize = new Size(width, height);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            ShowIcon = false;
        }

        /// <summary>Gọi ở cuối constructor của form con sau khi đã thêm hết control.</summary>
        protected void EndInit()
        {
            ResumeLayout(false);
            PerformLayout();
        }
    }

    /// <summary>Hộp thoại nhập liệu dạng 2 cột (nhãn - ô nhập) với nút Lưu / Hủy.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class DialogBase : AppForm
    {
        protected readonly Panel Body;
        protected readonly Button BtnOk;
        protected readonly Button BtnCancel;
        private readonly int _width;
        private int _y = 16;

        public DialogBase(string title, int width = 480) : base(title, width, 300)
        {
            _width = width;
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            Body = new Panel { BackColor = Color.White };

            var bottom = new FlowLayoutPanel
            {
                Height = 58,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 10, 12, 8),
                BackColor = Theme.Back,
            };
            BtnCancel = Ui.Btn("Hủy", BtnKind.Secondary, 100);
            BtnCancel.DialogResult = DialogResult.Cancel;
            BtnOk = Ui.Btn("Lưu", BtnKind.Primary, 110);
            bottom.Controls.Add(BtnCancel);
            bottom.Controls.Add(BtnOk);
            CancelButton = BtnCancel;
            BtnOk.Click += (s, e) =>
            {
                try
                {
                    if (OnSave()) { DialogResult = DialogResult.OK; Close(); }
                }
                catch (Exception ex) { Ui.DbError(ex); }
            };

            Ui.Stack(this, Body, new Control[] { Ui.Header(title, null, 50) }, new Control[] { bottom });
        }

        /// <summary>Thêm 1 dòng "nhãn: ô nhập" (đặt vị trí cố định, không bị co mất ô nhập).</summary>
        protected void AddField(string label, Control control, int rowHeight = 42)
        {
            const int labelX = 20, fieldX = 165;
            var lbl = Ui.Lbl(label);
            lbl.Location = new Point(labelX, _y + 6);
            control.Location = new Point(fieldX, _y + 2);
            control.Width = _width - fieldX - 22;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            Body.Controls.Add(lbl);
            Body.Controls.Add(control);
            _y += rowHeight;
            ClientSize = new Size(_width, 50 + 58 + _y + 12);
        }

        /// <summary>Kiểm tra và lưu dữ liệu. Trả về true để đóng hộp thoại.</summary>
        protected virtual bool OnSave() => true;
    }
}
