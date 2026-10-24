using System;
using System.Text;

namespace HospitalUsers.Security
{
    /// <summary>
    /// Bam va xac minh mat khau nhan vien bang BCrypt.Net-Next.
    /// Giu nguyen API HashPassword/VerifyPassword de tuong thich AuthController.
    /// </summary>
    public static class PasswordHasher
    {
        // BCrypt truyen thong chi xu ly toi da 72 byte, khong phai 72 ky tu.
        // Dong bo gioi han voi AuthController (dang ky va dang nhap).
        public const int MaxPasswordBytes = 72;

        // Work factor chi ap dung cho hash MOI. Hash cu van duoc Verify binh thuong
        // vi he so tinh toan va salt duoc luu ngay trong chuoi hash.
        private const int WorkFactor = 12;

        /// <summary>
        /// Bam mat khau va tu sinh salt ngau nhien cho moi lan goi.
        /// Khong trim / chuyen doi mat khau vi se thay doi gia tri mat khau.
        /// </summary>
        public static string HashPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    "Mat khau khong duoc de trong hoac chi co khoang trang.",
                    nameof(password));
            }

            if (Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
            {
                throw new ArgumentException(
                    "Mat khau khong duoc vuot qua 72 byte UTF-8.",
                    nameof(password));
            }

            // BCrypt.Net tu sinh salt; khong tu tao/luu salt rieng.
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }

        /// <summary>
        /// Xac minh mat khau. Tra false neu input khong hop le hoac hash bi hong.
        /// Khong nang work factor tai day, khong thay doi hash da luu trong CSDL.
        /// </summary>
        public static bool VerifyPassword(string? password, string? hashedPassword)
        {
            if (string.IsNullOrEmpty(password) ||
                Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes ||
                string.IsNullOrWhiteSpace(hashedPassword) ||
                hashedPassword.Length != 60)
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Hash trong CSDL bi loi dinh dang: tu choi xac thuc.
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
