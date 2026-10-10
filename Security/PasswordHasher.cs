using BCrypt.Net; // Cần thiết để sử dụng thư viện băm BCrypt

namespace HospitalUsers.Security
{
    // Lớp này chứa các công cụ hỗ trợ xử lý mật khẩu
    public static class PasswordHasher
    {
        /// <summary>
        /// Hàm này nhận vào mật khẩu dạng chữ thường (plain text) 
        /// và trả về một chuỗi mã băm (hash) lộn xộn không thể đọc được.
        /// </summary>
        /// <param name="password">Mật khẩu người dùng nhập vào</param>
        /// <returns>Chuỗi mật khẩu đã được băm</returns>
        public static string HashPassword(string password)
        {
            // Sử dụng thuật toán BCrypt với số vòng lặp ngẫu nhiên (work factor mặc định thường là 11)
            // Càng nhiều vòng lặp, việc băm càng chậm nhưng càng an toàn trước các cuộc tấn công Brute-Force
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Hàm này dùng để kiểm tra khi người dùng đăng nhập.
        /// So sánh mật khẩu người dùng vừa nhập với chuỗi băm đang lưu trong Database.
        /// </summary>
        /// <param name="password">Mật khẩu người dùng vừa gõ trên khung đăng nhập</param>
        /// <param name="hashedPassword">Mã băm lấy từ cơ sở dữ liệu lên</param>
        /// <returns>Trả về true nếu khớp, false nếu sai mật khẩu</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // So sánh mật khẩu trơn với mã băm
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}