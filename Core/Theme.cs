using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyBanHang
{
    internal static class Theme
    {
        public static readonly Color Primary = Color.FromArgb(123, 30, 58);      
        public static readonly Color PrimaryDark = Color.FromArgb(88, 18, 40);
        public static readonly Color PrimaryHover = Color.FromArgb(146, 44, 76);
        public static readonly Color PrimaryLight = Color.FromArgb(248, 236, 240); 
        public static readonly Color Selection = Color.FromArgb(234, 205, 215);
        public static readonly Color Accent = Color.FromArgb(193, 154, 62);       
        public static readonly Color Back = Color.FromArgb(246, 241, 243);   
        public static readonly Color Surface = Color.White;
        public static readonly Color Text = Color.FromArgb(40, 34, 37);
        public static readonly Color Muted = Color.FromArgb(125, 112, 118);
        public static readonly Color Border = Color.FromArgb(228, 216, 221);
        public static readonly Color Danger = Color.FromArgb(185, 48, 48);
        public static readonly Color DangerLight = Color.FromArgb(253, 232, 232);
        public static readonly Color Success = Color.FromArgb(46, 125, 84);
        public static readonly Color Warning = Color.FromArgb(200, 120, 20);

        public static readonly Color Sidebar = Color.FromArgb(104, 22, 48);       // menu dọc
        public static readonly Color SidebarHover = Color.FromArgb(128, 36, 64);
        public static readonly Color SidebarActive = Color.FromArgb(150, 48, 80);
        public static readonly Color GridHeader = Color.FromArgb(250, 244, 246);

        public static readonly Font Base = new Font("Segoe UI", 10F);
        public static readonly Font Bold = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font Small = new Font("Segoe UI", 9F);
        public static readonly Font Title = new Font("Segoe UI", 15F, FontStyle.Bold);
        public static readonly Font Big = new Font("Segoe UI", 18F, FontStyle.Bold);
    }

    /// <summary>
    /// Thông tin cửa hàng và TÀI KHOẢN NGÂN HÀNG nhận chuyển khoản (mã QR).
    /// ⚠️ Đổi BankId / AccountNo / AccountName thành tài khoản thật của bạn để quét QR chuyển tiền được.
    /// BankId là tên viết tắt ngân hàng: MB, VCB (Vietcombank), TCB (Techcombank), ICB (VietinBank),
    /// BIDV, ACB, VPB (VPBank), TPB (TPBank)... hoặc mã BIN 6 số.
    /// </summary>
    internal static class ShopInfo
    {
        public const string Name = "TWENTY FIVE";
        public const string Address = "Giải Phóng, Hai Bà Trưng, Hà Nội";
        public const string Phone = "0900 252 525";

        public const string BankId = "MB";
        public const string AccountNo = "0123456789";
        public const string AccountName = "TWENTY FIVE SHOP";
    }

    internal static class Fmt
    {
        public static readonly CultureInfo VN = new CultureInfo("vi-VN");
        public static string Money(decimal v) => v.ToString("N0", VN) + " đ";
        public static string Num(decimal v) => v.ToString("N0", VN);
        public static string Date(DateTime d) => d.ToString("dd/MM/yyyy HH:mm");
    }

    internal static class Session
    {
        public static int UserId { get; set; }
        public static string Username { get; set; } = "";
        public static string FullName { get; set; } = "";
        public static string Role { get; set; } = "";
        public static bool IsAdmin => Role == "Admin";
        public static string RoleText => RoleName(Role);

        public static string RoleName(string role) => role == "Admin" ? "Quản lý" : "Nhân viên bán hàng";

        public static void Clear()
        {
            UserId = 0; Username = FullName = Role = "";
        }
    }

    internal static class Security
    {
        /// <summary>Băm mật khẩu SHA-256 (khớp với hàm SHA2(...,256) của MySQL).</summary>
        public static string Hash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
