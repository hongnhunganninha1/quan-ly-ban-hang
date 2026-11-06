using System.Data;
using MySqlConnector;

namespace QuanLyBanHang
{
    /// <summary>
    /// Lớp kết nối và truy vấn MySQL dùng chung cho toàn bộ chương trình.
    /// Đổi Password cho đúng mật khẩu root MySQL trên máy của bạn.
    /// </summary>
    internal static class Database
    {
        private const string ConnStr =
            "Server=localhost;Port=3306;Database=shopthoitrang;User=root;Password=123456;";

        public static MySqlConnection GetConnection() => new MySqlConnection(ConnStr);

        /// <summary>Chạy câu SELECT, trả về DataTable để đổ lên DataGridView.</summary>
        public static DataTable Query(string sql, params (string Name, object? Value)[] ps)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            AddParams(cmd, ps);
            using var da = new MySqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        /// <summary>Lấy 1 giá trị (COUNT, SUM...). Trả về null nếu không có.</summary>
        public static object? Scalar(string sql, params (string Name, object? Value)[] ps)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            AddParams(cmd, ps);
            var v = cmd.ExecuteScalar();
            return v == DBNull.Value ? null : v;
        }

        /// <summary>Chạy INSERT / UPDATE / DELETE, trả về số dòng bị ảnh hưởng.</summary>
        public static int Execute(string sql, params (string Name, object? Value)[] ps)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            AddParams(cmd, ps);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>Tạo command trong 1 transaction (dùng cho lập hóa đơn, nhập kho...).</summary>
        public static MySqlCommand Command(MySqlConnection conn, MySqlTransaction tx, string sql,
            params (string Name, object? Value)[] ps)
        {
            var cmd = new MySqlCommand(sql, conn, tx);
            AddParams(cmd, ps);
            return cmd;
        }

        public static void AddParams(MySqlCommand cmd, (string Name, object? Value)[] ps)
        {
            foreach (var (name, value) in ps)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        public static decimal ToDecimal(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToDecimal(v);
        public static int ToInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    }
}
