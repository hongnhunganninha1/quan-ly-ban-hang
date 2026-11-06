namespace QuanLyBanHang
{
    /// <summary>
    /// Cửa sổ thanh toán: chọn phương thức
    ///  - Tiền mặt: nhập số tiền khách đưa → tự tính tiền trả lại khách
    ///  - Chuyển khoản (QR): tự hiện mã VietQR có sẵn số tiền + nội dung (quét bằng app ngân hàng)
    ///  - Thẻ ngân hàng: quẹt thẻ trên máy POS rồi xác nhận
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal class PaymentDialog : AppForm
    {
        private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public string PaymentMethod { get; private set; } = "Tiền mặt";
        public decimal CashGiven { get; private set; }

        private readonly decimal total;
        private readonly string orderCode;
        private readonly Button tabCash, tabQr, tabCard;
        private readonly Panel pnlCash = new() { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly Panel pnlQr = new() { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly Panel pnlCard = new() { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly NumericUpDown numCash = Ui.Num(10_000_000_000, 260);
        private readonly Label lblChange = new();
        private readonly PictureBox picQr = new() { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private readonly Label lblQrStatus = new();
        private readonly Button btnConfirm;
        private bool qrLoaded;

        public PaymentDialog(decimal total, string orderCode) : base("Thanh toán", 620, 640)
        {
            this.total = total;
            this.orderCode = orderCode;
            Text = "Thanh toán hóa đơn " + orderCode;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.White;

            // ----- tổng tiền -----
            var totalPanel = new Panel { Height = 92, BackColor = Theme.PrimaryLight };
            totalPanel.Controls.Add(new Label
            {
                Text = "Số tiền khách cần thanh toán", AutoSize = false, Size = new Size(620, 26), Location = new Point(0, 10),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted,
            });
            totalPanel.Controls.Add(new Label
            {
                Text = Fmt.Money(total), AutoSize = false, Size = new Size(620, 50), Location = new Point(0, 36),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Primary, Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            });

            // ----- chọn phương thức -----
            var tabs = new FlowLayoutPanel { Height = 66, Padding = new Padding(16, 12, 0, 0), BackColor = Color.White };
            tabCash = Tab("💵  Tiền mặt");
            tabQr = Tab("📱  Chuyển khoản (QR)");
            tabCard = Tab("💳  Thẻ ngân hàng");
            tabs.Controls.AddRange(new Control[] { tabCash, tabQr, tabCard });

            BuildCashPanel();
            BuildQrPanel();
            BuildCardPanel();
            var content = new Panel { BackColor = Color.White };
            content.Controls.AddRange(new Control[] { pnlCash, pnlQr, pnlCard });

            // ----- nút -----
            var bottom = new FlowLayoutPanel { Height = 64, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(14, 10, 14, 10), BackColor = Theme.Back };
            var btnClose = Ui.Btn("Quay lại", BtnKind.Secondary, 120, 42);
            btnClose.DialogResult = DialogResult.Cancel;
            btnConfirm = Ui.Btn("✔ XÁC NHẬN THANH TOÁN", BtnKind.Success, 260, 42);
            bottom.Controls.AddRange(new Control[] { btnClose, btnConfirm });
            CancelButton = btnClose;
            AcceptButton = btnConfirm;

            Ui.Stack(this, content, new Control[] { Ui.Header("THANH TOÁN", orderCode, 50), totalPanel, tabs }, new Control[] { bottom });

            tabCash.Click += (s, e) => ShowTab(0);
            tabQr.Click += (s, e) => ShowTab(1);
            tabCard.Click += (s, e) => ShowTab(2);
            btnConfirm.Click += (s, e) => Confirm();
            Shown += (s, e) => ShowTab(0);
            EndInit();
        }

        private Button Tab(string text)
        {
            var b = Ui.Btn(text, BtnKind.Secondary, 186, 44);
            b.Margin = new Padding(0, 0, 8, 0);
            return b;
        }

        private void ShowTab(int index)
        {
            var tabsArr = new[] { tabCash, tabQr, tabCard };
            var pnls = new[] { pnlCash, pnlQr, pnlCard };
            for (int i = 0; i < 3; i++)
            {
                bool on = i == index;
                tabsArr[i].BackColor = on ? Theme.Primary : Color.White;
                tabsArr[i].ForeColor = on ? Color.White : Theme.Primary;
                pnls[i].Visible = on;
            }
            PaymentMethod = index switch { 1 => "Chuyển khoản", 2 => "Thẻ ngân hàng", _ => "Tiền mặt" };
            if (index == 0) { numCash.Focus(); numCash.Select(0, numCash.Text.Length); UpdateChange(); }
            else btnConfirm.Enabled = true;
            if (index == 1 && !qrLoaded) _ = LoadQrAsync();
            btnConfirm.Text = index == 1 ? "✔ ĐÃ NHẬN ĐƯỢC TIỀN" : "✔ XÁC NHẬN THANH TOÁN";
        }

        // ================= TIỀN MẶT =================
        private void BuildCashPanel()
        {
            pnlCash.Controls.Add(new Label { Text = "Số tiền khách đưa (đ):", AutoSize = true, Font = Theme.Bold, Location = new Point(24, 14) });
            numCash.Location = new Point(24, 42);
            numCash.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            numCash.Increment = 10000;
            numCash.ValueChanged += (s, e) => UpdateChange();
            numCash.KeyUp += (s, e) => UpdateChange();
            pnlCash.Controls.Add(numCash);

            // các mức tiền gợi ý để bấm nhanh
            var quick = new FlowLayoutPanel { Location = new Point(24, 92), Size = new Size(570, 92) };
            foreach (var amount in SuggestCash(total))
            {
                var b = Ui.Btn(amount == total ? "Đủ tiền" : Fmt.Num(amount), BtnKind.Secondary, 132, 38);
                b.Margin = new Padding(0, 0, 8, 8);
                b.Click += (s, e) => { numCash.Value = Math.Min(numCash.Maximum, amount); UpdateChange(); };
                quick.Controls.Add(b);
            }
            pnlCash.Controls.Add(quick);

            pnlCash.Controls.Add(new Label { Text = "Tiền trả lại khách:", AutoSize = true, Font = Theme.Bold, Location = new Point(24, 196) });
            lblChange.AutoSize = false;
            lblChange.Size = new Size(570, 56);
            lblChange.Location = new Point(24, 222);
            lblChange.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblChange.TextAlign = ContentAlignment.MiddleLeft;
            pnlCash.Controls.Add(lblChange);
        }

        /// <summary>Gợi ý: đủ tiền, làm tròn lên 10k / 50k / 100k, các tờ 200k, 500k...</summary>
        private static List<decimal> SuggestCash(decimal total)
        {
            var list = new List<decimal> { total };
            foreach (var step in new decimal[] { 10_000, 50_000, 100_000, 200_000, 500_000, 1_000_000 })
            {
                var v = Math.Ceiling(total / step) * step;
                if (v > 0 && !list.Contains(v)) list.Add(v);
            }
            return list.Take(8).ToList();
        }

        private void UpdateChange()
        {
            var change = numCash.Value - total;
            if (numCash.Value == 0)
            {
                lblChange.Text = "Nhập số tiền khách đưa";
                lblChange.ForeColor = Theme.Muted;
                btnConfirm.Enabled = false;
            }
            else if (change >= 0)
            {
                lblChange.Text = Fmt.Money(change);
                lblChange.ForeColor = Theme.Success;
                btnConfirm.Enabled = true;
            }
            else
            {
                lblChange.Text = "Còn thiếu " + Fmt.Money(-change);
                lblChange.ForeColor = Theme.Danger;
                btnConfirm.Enabled = false;
            }
        }

        // ================= CHUYỂN KHOẢN QR =================
        private void BuildQrPanel()
        {
            picQr.Size = new Size(260, 300);
            picQr.Location = new Point(20, 4);
            pnlQr.Controls.Add(picQr);
            int x = 300;
            void Info(string label, string value, int y, bool big = false)
            {
                pnlQr.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted, Location = new Point(x, y) });
                pnlQr.Controls.Add(new Label
                {
                    Text = value, AutoSize = false, Size = new Size(300, big ? 34 : 24), Location = new Point(x, y + 20),
                    Font = big ? new Font("Segoe UI", 15F, FontStyle.Bold) : Theme.Bold, ForeColor = big ? Theme.Primary : Theme.Text,
                });
            }
            Info("Ngân hàng", ShopInfo.BankId, 10);
            Info("Số tài khoản", ShopInfo.AccountNo, 60);
            Info("Chủ tài khoản", ShopInfo.AccountName, 110);
            Info("Số tiền", Fmt.Money(total), 160, true);
            Info("Nội dung chuyển khoản", orderCode, 220);
            lblQrStatus.AutoSize = false;
            lblQrStatus.Size = new Size(300, 44);
            lblQrStatus.Location = new Point(x, 268);
            lblQrStatus.ForeColor = Theme.Muted;
            lblQrStatus.Font = Theme.Small;
            pnlQr.Controls.Add(lblQrStatus);
        }

        private async Task LoadQrAsync()
        {
            lblQrStatus.Text = "Đang tạo mã QR...";
            string url = $"https://img.vietqr.io/image/{ShopInfo.BankId}-{ShopInfo.AccountNo}-compact2.png" +
                         $"?amount={decimal.ToInt64(total)}&addInfo={Uri.EscapeDataString(orderCode)}" +
                         $"&accountName={Uri.EscapeDataString(ShopInfo.AccountName)}";
            try
            {
                var bytes = await http.GetByteArrayAsync(url);
                if (IsDisposed) return;
                using var ms = new MemoryStream(bytes);
                using var img = Image.FromStream(ms);
                picQr.Image = new Bitmap(img);
                qrLoaded = true;
                lblQrStatus.Text = "Khách mở app ngân hàng, quét mã để chuyển khoản. Kiểm tra đã nhận tiền rồi bấm xác nhận.";
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                lblQrStatus.ForeColor = Theme.Danger;
                lblQrStatus.Text = "Không tạo được mã QR (máy cần có Internet). Có thể đọc số tài khoản cho khách. " + ex.Message;
            }
        }

        // ================= THẺ =================
        private void BuildCardPanel()
        {
            pnlCard.Controls.Add(new Label
            {
                Text = "💳",
                Font = new Font("Segoe UI Emoji", 48F),
                AutoSize = false, Size = new Size(620, 100), Location = new Point(0, 20),
                TextAlign = ContentAlignment.MiddleCenter,
            });
            pnlCard.Controls.Add(new Label
            {
                Text = "Quẹt / chạm thẻ của khách trên máy POS với số tiền trên.\nKhi máy POS báo giao dịch thành công, bấm \"Xác nhận thanh toán\".",
                AutoSize = false, Size = new Size(580, 70), Location = new Point(20, 130),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Text,
            });
        }

        private void Confirm()
        {
            if (PaymentMethod == "Tiền mặt")
            {
                if (numCash.Value < total) { Ui.Warn("Số tiền khách đưa chưa đủ."); return; }
                CashGiven = numCash.Value;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) picQr.Image?.Dispose();
            base.Dispose(disposing);
        }
    }
}
