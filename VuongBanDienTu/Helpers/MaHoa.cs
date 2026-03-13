using System;
using System.Security.Cryptography;
using System.Text;

namespace VuongBanDienTu.Helpers
{
    public static class MaHoa
    {
        /// <summary>
        /// Mã hóa mật khẩu sử dụng SHA256
        /// </summary>
        public static string ToSHA256(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
