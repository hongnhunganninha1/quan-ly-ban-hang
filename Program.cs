namespace QuanLyBanHang
{
    internal static class Program
    {
        /// <summary>
        ///  Điểm bắt đầu chương trình: Đăng nhập → Màn hình chính. Đăng xuất thì quay lại màn hình đăng nhập.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Thread.CurrentThread.CurrentCulture = Fmt.VN;

            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                }
                bool loggedOut;
                using (var main = new MainForm())
                {
                    Application.Run(main);
                    loggedOut = main.LoggedOut;
                }
                Session.Clear();
                if (!loggedOut) break;
            }
        }
    }
}
