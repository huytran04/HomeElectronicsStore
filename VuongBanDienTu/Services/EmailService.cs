using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Services
{
    public static class EmailService
    {
        private static readonly string SenderEmail = ConfigurationManager.AppSettings["Email_Sender"];
        private static readonly string SenderPassword = ConfigurationManager.AppSettings["Email_Password"];
        private static readonly string AdminEmail = ConfigurationManager.AppSettings["Email_Admin"];
        private static readonly string SmtpHost = ConfigurationManager.AppSettings["Smtp_Host"] ?? "smtp.gmail.com";
        private static readonly int SmtpPort = int.TryParse(ConfigurationManager.AppSettings["Smtp_Port"], out int port) ? port : 587;

        private static void SendHtmlEmail(string toEmail, string subject, string body)
        {
            try
            {
                if (string.IsNullOrEmpty(SenderEmail) || string.IsNullOrEmpty(SenderPassword))
                {
                    System.Diagnostics.Debug.WriteLine("EmailService Error: Sender email or password is not configured in Web.config.");
                    return;
                }

                if (string.IsNullOrEmpty(toEmail))
                {
                    return;
                }

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(SenderEmail, "Vương Bán Điện Tử");
                    mail.To.Add(toEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.SubjectEncoding = Encoding.UTF8;

                    using (var smtp = new SmtpClient(SmtpHost, SmtpPort))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"EmailService Success: Sent email to {toEmail} with subject '{subject}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmailService Error: Failed to send email to {toEmail}. Details: {ex.Message}");
            }
        }

        public static void SendRegistrationEmail(NguoiDung user, string otpCode = "")
        {
            if (user == null || string.IsNullOrEmpty(user.Email)) return;

            string subject = "Mã kích hoạt tài khoản Vương Bán Điện Tử của bạn";

            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='background: linear-gradient(135deg, #e8192c 0%, #ff4b5c 100%); padding: 30px; text-align: center; color: white;'>
                    <h1 style='margin: 0; font-size: 24px; font-weight: 800; text-transform: uppercase; letter-spacing: 1px;'>VƯƠNG BÁN ĐIỆN TỬ</h1>
                    <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>Xác nhận kích hoạt tài khoản</p>
                </div>
                <div style='padding: 30px; color: #334155; line-height: 1.6;'>
                    <h2 style='color: #e8192c; margin-top: 0;'>Chào {user.HoTen},</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Vương Bán Điện Tử</strong>!</p>
                    <p>Để kích hoạt tài khoản và bắt đầu mua sắm, vui lòng nhập mã OTP gồm 6 chữ số dưới đây:</p>

                    <div style='background-color: #fff5f5; border: 2px dashed #e8192c; padding: 24px; margin: 25px 0; text-align: center; border-radius: 12px;'>
                        <p style='margin: 0 0 8px 0; font-size: 13px; color: #64748b; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;'>MÃ KÍCH HOẠT (OTP)</p>
                        <span style='font-size: 36px; font-weight: 900; font-family: Consolas, monospace; color: #e8192c; letter-spacing: 6px;'>{otpCode}</span>
                    </div>

                    <div style='background-color: #f8fafc; border-left: 4px solid #e8192c; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                        <p style='margin: 0 0 8px 0; font-size: 14px; font-weight: bold; color: #475569;'>Thông tin tài khoản của bạn:</p>
                        <p style='margin: 0 0 4px 0; font-size: 13px;'><strong>Tên đăng nhập:</strong> {user.TenDangNhap}</p>
                        <p style='margin: 0 0 4px 0; font-size: 13px;'><strong>Số điện thoại:</strong> {user.SoDienThoai}</p>
                        <p style='margin: 0; font-size: 13px;'><strong>Email liên hệ:</strong> {user.Email}</p>
                    </div>

                    <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 12px 15px; border-radius: 4px; font-size: 13px; color: #78350f;'>
                        <strong>Lưu ý:</strong> Mã OTP chỉ sử dụng một lần. Tuyệt đối không chia sẻ mã này với bất kỳ ai.
                    </div>
                </div>
                <div style='background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                    <p style='margin: 0 0 5px 0;'>Mọi thắc mắc xin liên hệ tổng đài hỗ trợ: <strong>1800 6800</strong></p>
                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} Vương Bán Điện Tử. All rights reserved.</p>
                </div>
            </div>";

            SendHtmlEmail(user.Email, subject, body);
        }

        public static void SendForgotPasswordEmail(NguoiDung user, string resetCode)
        {
            if (user == null || string.IsNullOrEmpty(user.Email)) return;

            string subject = "Mã xác nhận khôi phục mật khẩu tài khoản Vương Bán Điện Tử";

            string resetLink = $"http://localhost:63259/TaiKhoan/XacNhanResetMatKhau?username={Uri.EscapeDataString(user.TenDangNhap)}&code={resetCode}";

            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 30px; text-align: center; color: white;'>
                    <h1 style='margin: 0; font-size: 24px; font-weight: 800; text-transform: uppercase; letter-spacing: 1px;'>VƯƠNG BÁN ĐIỆN TỬ</h1>
                    <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>Khôi phục mật khẩu tài khoản</p>
                </div>
                <div style='padding: 30px; color: #334155; line-height: 1.6;'>
                    <h2 style='color: #e8192c; margin-top: 0;'>Chào {user.HoTen},</h2>
                    <p>Hệ thống đã nhận được yêu cầu khôi phục mật khẩu cho tài khoản <strong>{user.TenDangNhap}</strong> của bạn.</p>
                    
                    <p>Dưới đây là mã xác nhận (OTP) gồm 6 chữ số để khôi phục mật khẩu của bạn:</p>
                    
                    <div style='background-color: #f8fafc; border: 2px dashed #cbd5e1; padding: 20px; margin: 25px 0; text-align: center; border-radius: 12px;'>
                        <p style='margin: 0 0 5px 0; font-size: 13px; color: #64748b; font-weight: bold; text-transform: uppercase;'>MÃ XÁC NHẬN (OTP)</p>
                        <span style='font-size: 28px; font-weight: bold; font-family: Consolas, monospace; color: #e8192c; letter-spacing: 2px;'>{resetCode}</span>
                    </div>

                    <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px; font-size: 13px; color: #78350f;'>
                        <strong>Bảo mật:</strong> Tuyệt đối không chia sẻ mã này với bất kỳ ai khác để tránh bị mất tài khoản.
                    </div>

                    <div style='text-align: center; margin: 35px 0 15px 0;'>
                        <a href='{resetLink}' style='background-color: #e8192c; color: white; padding: 14px 35px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 4px 10px rgba(232, 25, 44, 0.3); font-size: 15px;'>ĐỔI MẬT KHẨU NGAY</a>
                    </div>
                </div>
                <div style='background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                    <p style='margin: 0 0 5px 0;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc liên hệ CSKH của chúng tôi.</p>
                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} Vương Bán Điện Tử. All rights reserved.</p>
                </div>
            </div>";

            SendHtmlEmail(user.Email, subject, body);
        }

        public static void SendOrderEmail(DonHang order, string type, string cancellationReason = "")
        {
            if (order == null) return;

            string customerEmail = order.NguoiDung?.Email;
            string customerName = order.NguoiDung?.HoTen ?? "Khách hàng";
            string customerPhone = order.NguoiDung?.SoDienThoai ?? "Chưa cập nhật";
            string orderDate = order.NgayDat.HasValue ? order.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string paymentMethod = order.PhuongThucThanhToan == "VNPAY" ? "Thanh toán trực tuyến (VNPAY)" : "Thanh toán khi nhận hàng (COD)";
            
            string subject = "";
            string headerText = "";
            string headerBg = "";
            string statusMessage = "";

            if (type == "DatHang")
            {
                subject = $"[Vương Bán Điện Tử] Xác nhận đặt đơn hàng mới thành công #{order.MaDonHang}";
                headerText = "ĐẶT HÀNG THÀNH CÔNG";
                headerBg = "linear-gradient(135deg, #e8192c 0%, #ff4b5c 100%)";
                statusMessage = $"Cảm ơn bạn đã mua sắm tại Vương Bán Điện Tử! Đơn hàng #{order.MaDonHang} của bạn đã được tiếp nhận thành công và đang được chuẩn bị đóng gói.";
            }
            else if (type == "HuyHang")
            {
                subject = $"[Vương Bán Điện Tử] Thông báo hủy đơn hàng #{order.MaDonHang}";
                headerText = "ĐƠN HÀNG ĐÃ HỦY";
                headerBg = "linear-gradient(135deg, #64748b 0%, #475569 100%)";
                statusMessage = $"Chúng tôi xin thông báo đơn hàng #{order.MaDonHang} của bạn đã được hủy thành công.";
            }

            StringBuilder itemsHtml = new StringBuilder();
            if (order.ChiTietDonHangs != null && order.ChiTietDonHangs.Count > 0)
            {
                foreach (var detail in order.ChiTietDonHangs)
                {
                    string productName = detail.SanPham?.TenSanPham ?? "Sản phẩm công nghệ";
                    int quantity = detail.SoLuong;
                    decimal price = detail.GiaLuuTru;
                    decimal subTotal = price * quantity;

                    itemsHtml.Append($@"
                    <tr style='border-bottom: 1px solid #f1f5f9;'>
                        <td style='padding: 12px 8px; font-size: 13px; color: #1e293b; font-weight: bold;'>{productName}</td>
                        <td style='padding: 12px 8px; font-size: 13px; color: #475569; text-align: center;'>{quantity}</td>
                        <td style='padding: 12px 8px; font-size: 13px; color: #475569; text-align: right;'>{price.ToString("N0")}₫</td>
                        <td style='padding: 12px 8px; font-size: 13px; color: #e8192c; font-weight: bold; text-align: right;'>{subTotal.ToString("N0")}₫</td>
                    </tr>");
                }
            }

            string bodyTemplate = $@"
            <div style='font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='background: {headerBg}; padding: 30px; text-align: center; color: white;'>
                    <h1 style='margin: 0 0 5px 0; font-size: 18px; font-weight: 500; opacity: 0.9; text-transform: uppercase;'>HỆ THỐNG VƯƠNG BÁN ĐIỆN TỬ</h1>
                    <h2 style='margin: 0; font-size: 26px; font-weight: 900; letter-spacing: 1px;'>{headerText}</h2>
                </div>
                <div style='padding: 30px; color: #334155; line-height: 1.6;'>
                    <p style='font-size: 15px;'>Chào <strong>{customerName}</strong>,</p>
                    <p style='font-size: 14px;'>{statusMessage}</p>

                    {(type == "HuyHang" && !string.IsNullOrEmpty(cancellationReason) ? $@"
                    <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; border-radius: 4px; font-size: 13px; color: #991b1b;'>
                        <strong>Lý do hủy đơn:</strong> {cancellationReason}
                    </div>" : "")}

                    <div style='background-color: #f8fafc; border-radius: 8px; padding: 20px; margin: 25px 0; border: 1px solid #f1f5f9;'>
                        <h3 style='margin: 0 0 15px 0; font-size: 14px; font-weight: bold; text-transform: uppercase; color: #475569; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;'>Thông Tin Chi Tiết Đơn Hàng #{order.MaDonHang}</h3>
                        <table style='width: 100%; font-size: 13px; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 4px 0; color: #64748b; width: 140px;'>Ngày đặt hàng:</td>
                                <td style='padding: 4px 0; font-weight: bold; color: #1e293b;'>{orderDate}</td>
                            </tr>
                            <tr>
                                <td style='padding: 4px 0; color: #64748b;'>Họ tên người nhận:</td>
                                <td style='padding: 4px 0; font-weight: bold; color: #1e293b;'>{customerName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 4px 0; color: #64748b;'>Số điện thoại:</td>
                                <td style='padding: 4px 0; font-weight: bold; color: #1e293b;'>{customerPhone}</td>
                            </tr>
                            <tr>
                                <td style='padding: 4px 0; color: #64748b;'>Địa chỉ giao nhận:</td>
                                <td style='padding: 4px 0; font-weight: bold; color: #1e293b;'>{order.DiaChiGiaoHang ?? "Nhận tại cửa hàng"}</td>
                            </tr>
                            <tr>
                                <td style='padding: 4px 0; color: #64748b;'>Phương thức:</td>
                                <td style='padding: 4px 0; font-weight: bold; color: #1e293b;'>{paymentMethod}</td>
                            </tr>
                        </table>
                    </div>

                    <table style='width: 100%; border-collapse: collapse; margin: 25px 0;'>
                        <thead>
                            <tr style='background-color: #f1f5f9; border-bottom: 2px solid #e2e8f0;'>
                                <th style='padding: 10px 8px; text-align: left; font-size: 12px; text-transform: uppercase; color: #475569;'>Tên sản phẩm</th>
                                <th style='padding: 10px 8px; text-align: center; font-size: 12px; text-transform: uppercase; color: #475569; width: 60px;'>SL</th>
                                <th style='padding: 10px 8px; text-align: right; font-size: 12px; text-transform: uppercase; color: #475569; width: 100px;'>Đơn giá</th>
                                <th style='padding: 10px 8px; text-align: right; font-size: 12px; text-transform: uppercase; color: #475569; width: 100px;'>Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                            <tr style='background-color: #fafafa;'>
                                <td colspan='3' style='padding: 15px 8px; font-size: 14px; font-weight: bold; color: #1e293b; text-align: right;'>Tổng tiền thanh toán:</td>
                                <td style='padding: 15px 8px; font-size: 16px; font-weight: 900; color: #e8192c; text-align: right;'>{(order.TongTien ?? 0).ToString("N0")}₫</td>
                            </tr>
                        </tbody>
                    </table>

                    <div style='text-align: center; margin: 30px 0 10px 0;'>
                        <a href='http://localhost:63259/DonHang/Index' style='background-color: #e8192c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px rgba(232, 25, 44, 0.2);'>Tra Cứu Đơn Hàng</a>
                    </div>
                </div>
                <div style='background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                    <p style='margin: 0 0 5px 0;'>Cảm ơn quý khách đã tin dùng dịch vụ của chúng tôi.</p>
                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} Vương Bán Điện Tử. All rights reserved.</p>
                </div>
            </div>";

            // 1. Send to Customer (if email exists)
            if (!string.IsNullOrEmpty(customerEmail))
            {
                SendHtmlEmail(customerEmail, subject, bodyTemplate);
            }

            // 2. Send to Admin (Store owner)
            if (!string.IsNullOrEmpty(AdminEmail))
            {
                string adminSubject = $"[ADMIN ALERT] {(type == "DatHang" ? "Đơn hàng mới" : "Đơn hàng bị HỦY")} #{order.MaDonHang} - {customerName}";
                SendHtmlEmail(AdminEmail, adminSubject, bodyTemplate);
            }
        }

        // Gửi email cho ADMIN khi khách yêu cầu hủy đơn đã thanh toán
        public static void SendRefundRequestEmail(DonHang order, string lyDo)
        {
            if (order == null || string.IsNullOrEmpty(AdminEmail)) return;

            string customerName = order.NguoiDung?.HoTen ?? "Khách hàng";
            string customerEmail = order.NguoiDung?.Email ?? "Chưa có";
            string orderDate = order.NgayDat.HasValue ? order.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "---";
            string totalAmount = (order.TongTien ?? 0).ToString("N0");
            string paymentMethod = order.PhuongThucThanhToan == "VNPAY" ? "VNPAY" : "COD";

            string subject = $"[YÊU CẦU HOÀN TIỀN] Đơn hàng #{order.MaDonHang} - {customerName}";
            string body = $@"
            <div style='font-family:Arial,sans-serif;max-width:620px;margin:0 auto;border:1px solid #e2e8f0;border-radius:12px;overflow:hidden;'>
                <div style='background:linear-gradient(135deg,#f59e0b,#d97706);padding:28px;text-align:center;color:white;'>
                    <h1 style='margin:0 0 4px;font-size:14px;opacity:.9;text-transform:uppercase;'>Hệ Thống Vương Bán Điện Tử</h1>
                    <h2 style='margin:0;font-size:24px;font-weight:900;'>⚠ YÊU CẦU HOÀN TIỀN</h2>
                </div>
                <div style='padding:28px;color:#334155;'>
                    <p style='font-size:14px;'>Khách hàng <strong>{customerName}</strong> vừa yêu cầu hủy đơn hàng <strong>#{order.MaDonHang}</strong> đã thanh toán.</p>
                    <div style='background:#fef3c7;border-left:4px solid #f59e0b;padding:14px;border-radius:4px;margin:16px 0;font-size:13px;color:#92400e;'>
                        <strong>Lý do hủy:</strong> {lyDo}
                    </div>
                    <table style='width:100%;font-size:13px;border-collapse:collapse;margin:16px 0;background:#f8fafc;border-radius:8px;overflow:hidden;'>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Khách hàng:</td><td style='padding:8px 14px;font-weight:bold;'>{customerName}</td></tr>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Email:</td><td style='padding:8px 14px;font-weight:bold;'>{customerEmail}</td></tr>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Mã đơn:</td><td style='padding:8px 14px;font-weight:bold;'>#{order.MaDonHang}</td></tr>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Ngày đặt:</td><td style='padding:8px 14px;font-weight:bold;'>{orderDate}</td></tr>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Số tiền cần hoàn:</td><td style='padding:8px 14px;font-weight:900;color:#e8192c;font-size:15px;'>{totalAmount}₫</td></tr>
                        <tr><td style='padding:8px 14px;color:#64748b;'>Phương thức:</td><td style='padding:8px 14px;font-weight:bold;'>{paymentMethod}</td></tr>
                    </table>
                    <div style='text-align:center;margin:24px 0;'>
                        <a href='http://localhost:63259/QuanLyDonHang' style='background:#e8192c;color:white;padding:12px 28px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;'>Xử Lý Ngay Tại Admin</a>
                    </div>
                    <p style='font-size:12px;color:#94a3b8;'>Vui lòng vào trang Admin → Quản lý Đơn hàng → Duyệt hoàn tiền cho đơn hàng này.</p>
                </div>
            </div>";

            SendHtmlEmail(AdminEmail, subject, body);
        }

        // Gửi email cho KHÁCH HÀNG khi admin duyệt hoàn tiền
        public static void SendRefundApprovedEmail(DonHang order)
        {
            if (order == null) return;
            string customerEmail = order.NguoiDung?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return;

            string customerName = order.NguoiDung?.HoTen ?? "Khách hàng";
            string totalAmount = (order.TongTien ?? 0).ToString("N0");
            string paymentMethod = order.PhuongThucThanhToan == "VNPAY" ? "VNPAY (hoàn về tài khoản ngân hàng)" : "COD (hoàn tiền mặt tại cửa hàng)";

            string subject = $"[Vương Bán Điện Tử] Yêu cầu hoàn tiền đơn #{order.MaDonHang} đã được duyệt";
            string body = $@"
            <div style='font-family:Arial,sans-serif;max-width:620px;margin:0 auto;border:1px solid #e2e8f0;border-radius:12px;overflow:hidden;'>
                <div style='background:linear-gradient(135deg,#16a34a,#15803d);padding:28px;text-align:center;color:white;'>
                    <h1 style='margin:0 0 4px;font-size:14px;opacity:.9;text-transform:uppercase;'>Hệ Thống Vương Bán Điện Tử</h1>
                    <h2 style='margin:0;font-size:24px;font-weight:900;'>✓ YÊU CẦU HOÀN TIỀN ĐÃ ĐƯỢC DUYỆT</h2>
                </div>
                <div style='padding:28px;color:#334155;line-height:1.7;'>
                    <p style='font-size:15px;'>Chào <strong>{customerName}</strong>,</p>
                    <p style='font-size:14px;'>Yêu cầu hủy và hoàn tiền cho đơn hàng <strong>#{order.MaDonHang}</strong> của bạn đã được Admin xác nhận thành công.</p>
                    <div style='background:#f0fdf4;border-left:4px solid #16a34a;padding:16px;border-radius:4px;margin:20px 0;'>
                        <p style='margin:0 0 6px;font-size:13px;color:#166534;'><strong>Số tiền hoàn trả:</strong></p>
                        <p style='margin:0;font-size:28px;font-weight:900;color:#16a34a;'>{totalAmount}₫</p>
                        <p style='margin:6px 0 0;font-size:12px;color:#4ade80;'>Phương thức hoàn tiền: {paymentMethod}</p>
                    </div>
                    <p style='font-size:13px;color:#64748b;'>Thời gian hoàn tiền dự kiến: <strong>3–5 ngày làm việc</strong> tùy theo ngân hàng và phương thức thanh toán.</p>
                    <p style='font-size:13px;'>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ hotline <strong style='color:#e8192c;'>1800 6800</strong> hoặc đến trực tiếp cửa hàng.</p>
                    <div style='text-align:center;margin:28px 0 10px;'>
                        <a href='http://localhost:63259/DonHang/Index' style='background:#16a34a;color:white;padding:12px 28px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;'>Xem Lịch Sử Đơn Hàng</a>
                    </div>
                </div>
                <div style='background:#f1f5f9;padding:18px;text-align:center;font-size:11px;color:#94a3b8;border-top:1px solid #e2e8f0;'>
                    Cảm ơn bạn đã tin dùng Vương Bán Điện Tử &copy; {DateTime.Now.Year}
                </div>
            </div>";

            SendHtmlEmail(customerEmail, subject, body);
        }

        // Gửi email XÁC NHẬN cho KHÁCH khi họ TỰ HỦY đơn (chưa thanh toán)
        public static void SendSelfCancelEmail(DonHang order)
        {
            if (order == null) return;
            string customerEmail = order.NguoiDung?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return;

            string customerName = order.NguoiDung?.HoTen ?? "Khách hàng";
            string orderDate = order.NgayDat.HasValue ? order.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "---";
            string totalAmount = (order.TongTien ?? 0).ToString("N0");
            string lyDo = !string.IsNullOrEmpty(order.GhiChu) ? order.GhiChu : "Khách hàng yêu cầu hủy";

            string subject = $"[Vương Bán Điện Tử] Đơn hàng #{order.MaDonHang} đã được hủy thành công";
            string body = $@"
            <div style='font-family:Arial,sans-serif;max-width:620px;margin:0 auto;border:1px solid #e2e8f0;border-radius:12px;overflow:hidden;'>
                <div style='background:linear-gradient(135deg,#64748b,#475569);padding:28px;text-align:center;color:white;'>
                    <h1 style='margin:0 0 4px;font-size:14px;opacity:.9;text-transform:uppercase;'>Hệ Thống Vương Bán Điện Tử</h1>
                    <h2 style='margin:0;font-size:24px;font-weight:900;'>✓ HỦY ĐƠN HÀNG THÀNH CÔNG</h2>
                </div>
                <div style='padding:28px;color:#334155;line-height:1.7;'>
                    <p style='font-size:15px;'>Chào <strong>{customerName}</strong>,</p>
                    <p style='font-size:14px;'>Đơn hàng <strong>#{order.MaDonHang}</strong> của bạn đã được hủy thành công theo yêu cầu.</p>
                    <div style='background:#f8fafc;border-radius:8px;padding:18px;margin:20px 0;border:1px solid #e2e8f0;font-size:13px;'>
                        <table style='width:100%;border-collapse:collapse;'>
                            <tr><td style='padding:5px 0;color:#64748b;width:140px;'>Mã đơn hàng:</td><td style='padding:5px 0;font-weight:bold;'>#{order.MaDonHang}</td></tr>
                            <tr><td style='padding:5px 0;color:#64748b;'>Ngày đặt:</td><td style='padding:5px 0;font-weight:bold;'>{orderDate}</td></tr>
                            <tr><td style='padding:5px 0;color:#64748b;'>Tổng giá trị:</td><td style='padding:5px 0;font-weight:bold;'>{totalAmount}₫</td></tr>
                            <tr><td style='padding:5px 0;color:#64748b;'>Lý do hủy:</td><td style='padding:5px 0;font-style:italic;color:#475569;'>{lyDo}</td></tr>
                        </table>
                    </div>
                    <p style='font-size:13px;color:#64748b;'>Vì đơn hàng chưa được thanh toán nên không phát sinh khoản hoàn tiền nào.</p>
                    <p style='font-size:13px;'>Nếu bạn muốn đặt lại hoặc cần hỗ trợ, hãy liên hệ hotline <strong style='color:#e8192c;'>1800 6800</strong>.</p>
                    <div style='text-align:center;margin:24px 0 10px;'>
                        <a href='http://localhost:63259' style='background:#e8192c;color:white;padding:12px 28px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;'>Tiếp Tục Mua Sắm</a>
                    </div>
                </div>
                <div style='background:#f1f5f9;padding:18px;text-align:center;font-size:11px;color:#94a3b8;border-top:1px solid #e2e8f0;'>
                    Cảm ơn bạn đã tin dùng Vương Bán Điện Tử &copy; {DateTime.Now.Year}
                </div>
            </div>";

            SendHtmlEmail(customerEmail, subject, body);
        }
    }
}
