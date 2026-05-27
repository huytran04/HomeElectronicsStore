using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Helpers;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class TaiKhoanController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(NguoiDung user)
        {
            if (ModelState.IsValid)
            {
                var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯưẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỶỷỸỹỴỵ\s]+$");
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^0[35789]\d{8}$");

                if (string.IsNullOrEmpty(user.HoTen) || !nameRegex.IsMatch(user.HoTen))
                {
                    string msg = "Họ và tên chỉ được nhập chữ, không được chứa số hay ký tự đặc biệt!";
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = msg });
                    ViewBag.Error = msg;
                    return View();
                }

                if (string.IsNullOrEmpty(user.SoDienThoai) || !phoneRegex.IsMatch(user.SoDienThoai))
                {
                    string msg = "Số điện thoại không đúng định dạng! Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số.";
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = msg });
                    ViewBag.Error = msg;
                    return View();
                }

                if (string.IsNullOrEmpty(user.MatKhau) || user.MatKhau.Length < 8)
                {
                    string msg = "Mật khẩu phải tối thiểu 8 ký tự!";
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = msg });
                    ViewBag.Error = msg;
                    return View();
                }

                var hasUpperCase = new System.Text.RegularExpressions.Regex(@"[A-Z]");
                var hasLowerCase = new System.Text.RegularExpressions.Regex(@"[a-z]");
                var hasSpecialChar = new System.Text.RegularExpressions.Regex(@"[^a-zA-Z0-9]");

                if (!hasUpperCase.IsMatch(user.MatKhau) || !hasLowerCase.IsMatch(user.MatKhau) || !hasSpecialChar.IsMatch(user.MatKhau))
                {
                    string msg = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 ký tự đặc biệt!";
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = msg });
                    ViewBag.Error = msg;
                    return View();
                }

                var check = db.NguoiDungs.FirstOrDefault(s => s.TenDangNhap == user.TenDangNhap);
                if (check != null)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
                    }
                    ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                    return View();
                }

                if (!string.IsNullOrEmpty(user.Email))
                {
                    var checkEmail = db.NguoiDungs.FirstOrDefault(s => s.Email == user.Email);
                    if (checkEmail != null)
                    {
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { success = false, message = "Email này đã được đăng ký bởi tài khoản khác!" });
                        }
                        ViewBag.Error = "Email này đã được đăng ký bởi tài khoản khác!";
                        return View();
                    }
                }

                string otp = new Random().Next(100000, 999999).ToString();
                user.MatKhau = MaHoa.ToSHA256(user.MatKhau);
                user.NgayTao = DateTime.Now;
                user.MaVaiTro = PhanQuyen.KHACH_HANG;
                user.TrangThai = true;
                user.KichHoat = false;
                user.MaKichHoat = otp;

                db.Configuration.ValidateOnSaveEnabled = false;
                db.NguoiDungs.Add(user);
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log("Đăng ký tài khoản", $"Tên đăng nhập: {user.TenDangNhap}, Họ tên: {user.HoTen}", "Thành công");

                var registeredUserId = user.MaNguoiDung;
                var username = user.TenDangNhap;
                var savedOtp = otp;

                // Gửi email chứa mã OTP (chạy background để không block response)
                System.Threading.Tasks.Task.Run(() => {
                    try
                    {
                        using (var context = new VuongDienTuEntities())
                        {
                            var registeredUser = context.NguoiDungs.Find(registeredUserId);
                            if (registeredUser != null)
                            {
                                VuongBanDienTu.Services.EmailService.SendRegistrationEmail(registeredUser, savedOtp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Registration Email Error: " + ex.Message);
                    }
                });

                // Chuyển thẳng sang trang nhập mã OTP
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, redirect = Url.Action("XacNhanKichHoat", "TaiKhoan", new { username = username }) });
                }
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra Email để nhận mã kích hoạt tài khoản.";
                return RedirectToAction("XacNhanKichHoat", new { username = username });
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }
            return View();
        }

        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(string TenDangNhap, string MatKhau)
        {
            string hashedPass = MaHoa.ToSHA256(MatKhau);
            var user = db.NguoiDungs.SingleOrDefault(u => u.TenDangNhap == TenDangNhap && u.MatKhau == hashedPass);
            
            if (user == null)
            {
                user = db.NguoiDungs.SingleOrDefault(u => u.TenDangNhap == TenDangNhap && u.MatKhau == MatKhau);
            }

            if (user != null)
            {
                if (user.TrangThai == false || user.TrangThai == null)
                {
                    string msg = "Tài khoản của bạn đã bị khóa! Vui lòng liên hệ hỗ trợ.";
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = msg });
                    ViewBag.Error = msg;
                    return View();
                }

                if (user.KichHoat == false || user.KichHoat == null)
                {
                    string msg = "Tài khoản chưa được kích hoạt!";
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = msg, redirect = Url.Action("XacNhanKichHoat", new { username = user.TenDangNhap }) });
                    }
                    TempData["SuccessMessage"] = "Tài khoản của bạn chưa được kích hoạt. Vui lòng nhập mã kích hoạt bên dưới!";
                    return RedirectToAction("XacNhanKichHoat", new { username = user.TenDangNhap });
                }

                Session["TaiKhoan"] = user;
                PhanQuyen.RefreshPermissions();
                VuongBanDienTu.Helpers.ActivityLogger.Log("Đăng nhập", "Đăng nhập vào hệ thống", "Thành công");
                
                string redirectUrl = Url.Action("Index", "Home");
                if (PhanQuyen.HasPermission("TRUY_CAP_QUAN_TRI"))
                {
                    if (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY)
                    {
                        redirectUrl = Url.Action("TongQuan", "QuanTri");
                    }
                    else
                    {
                        redirectUrl = Url.Action("Index", "QuanLySanPham");
                    }
                }

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, redirect = redirectUrl });
                }
                return Redirect(redirectUrl);
            }

            if (Request.IsAjaxRequest()) return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng!" });
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View();
        }

        public ActionResult DangXuat()
        {
            VuongBanDienTu.Helpers.ActivityLogger.Log("Đăng xuất", "Đăng xuất khỏi hệ thống", "Thành công");
            Session["TaiKhoan"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult ThongTinCaNhan()
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("DangNhap");
            
            var userSession = (NguoiDung)Session["TaiKhoan"];
            var user = db.NguoiDungs.Find(userSession.MaNguoiDung);
            return View(user);
        }

        [HttpPost]
        public ActionResult CapNhatThongTin()
        {
            if (Session["TaiKhoan"] == null) return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });

            var userSession = (NguoiDung)Session["TaiKhoan"];
            var existingUser = db.NguoiDungs.Find(userSession.MaNguoiDung);

            if (existingUser != null)
            {
                string hoTen = Request["HoTen"];
                string sdt = Request["SoDienThoai"];
                string diaChi = Request["DiaChi"];
                string email = Request["Email"];

                if (string.IsNullOrEmpty(hoTen)) 
                    return Json(new { success = false, message = "Họ tên không được để trống!" });

                var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯưẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỶỷỸỹỴỵ\s]+$");
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^0[35789]\d{8}$");

                if (!nameRegex.IsMatch(hoTen))
                {
                    return Json(new { success = false, message = "Họ và tên chỉ được nhập chữ, không được chứa số hay ký tự đặc biệt!" });
                }

                if (string.IsNullOrEmpty(sdt) || !phoneRegex.IsMatch(sdt))
                {
                    return Json(new { success = false, message = "Số điện thoại không đúng định dạng! Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số." });
                }

                if (!string.IsNullOrEmpty(email))
                {
                    var checkEmail = db.NguoiDungs.FirstOrDefault(s => s.Email == email && s.MaNguoiDung != existingUser.MaNguoiDung);
                    if (checkEmail != null)
                    {
                        return Json(new { success = false, message = "Email này đã được đăng ký bởi tài khoản khác!" });
                    }
                }

                existingUser.HoTen = hoTen;
                existingUser.SoDienThoai = sdt;
                existingUser.DiaChi = diaChi;
                existingUser.Email = email;

                db.Configuration.ValidateOnSaveEnabled = false; 
                db.SaveChanges();
                Session["TaiKhoan"] = existingUser; 

                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy người dùng!" });
        }

        private string GenerateSecureTempPassword()
        {
            string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lowercase = "abcdefghijklmnopqrstuvwxyz";
            string digits = "0123456789";
            string specials = "@#$%!*?&";
            
            Random random = new Random();
            char up = uppercase[random.Next(uppercase.Length)];
            char low = lowercase[random.Next(lowercase.Length)];
            char dig = digits[random.Next(digits.Length)];
            char sp = specials[random.Next(specials.Length)];
            
            string allChars = uppercase + lowercase + digits + specials;
            char[] remaining = new char[4];
            for (int i = 0; i < 4; i++)
            {
                remaining[i] = allChars[random.Next(allChars.Length)];
            }
            
            char[] passwordArr = new char[8] { up, low, dig, sp, remaining[0], remaining[1], remaining[2], remaining[3] };
            
            return new string(passwordArr.OrderBy(c => random.Next()).ToArray());
        }

        public ActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        public ActionResult QuenMatKhau(string TenDangNhap, string Email)
        {
            if (string.IsNullOrEmpty(TenDangNhap) || string.IsNullOrEmpty(Email))
            {
                ViewBag.Error = "Tên đăng nhập và Email không được để trống!";
                return View();
            }

            var user = db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap.Trim().ToLower() == TenDangNhap.Trim().ToLower());
            if (user == null || string.IsNullOrEmpty(user.Email) || user.Email.Trim().ToLower() != Email.Trim().ToLower())
            {
                ViewBag.Error = "Tên đăng nhập hoặc Email không đúng!";
                return View();
            }

            string resetCode = new Random().Next(100000, 999999).ToString();

            user.MaQuenMatKhau = resetCode;
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();

            var userId = user.MaNguoiDung;
            var rawResetCode = resetCode;
            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    using (var context = new VuongDienTuEntities())
                    {
                        var targetUser = context.NguoiDungs.Find(userId);
                        if (targetUser != null)
                        {
                            VuongBanDienTu.Services.EmailService.SendForgotPasswordEmail(targetUser, rawResetCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ForgotPassword Email Error: " + ex.Message);
                }
            });

            TempData["SuccessMessage"] = "Mã xác nhận khôi phục mật khẩu đã được gửi đến Email của bạn. Vui lòng nhập mã bên dưới!";
            return RedirectToAction("XacNhanResetMatKhau", new { username = TenDangNhap });
        }

        public ActionResult XacNhanTaiKhoan(int id, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Error = "Mã kích hoạt không hợp lệ!";
                return View("DangNhap");
            }

            var user = db.NguoiDungs.Find(id);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản người dùng!";
                return View("DangNhap");
            }

            if (user.TrangThai == true)
            {
                TempData["SuccessMessage"] = "Tài khoản của bạn đã được kích hoạt trước đó. Bạn có thể đăng nhập!";
                return RedirectToAction("DangNhap");
            }

            string tokenInput = user.TenDangNhap + user.NgayTao.Value.Ticks.ToString();
            string expectedToken = MaHoa.ToSHA256(tokenInput);

            if (token.Trim() == expectedToken.Trim())
            {
                user.TrangThai = true;
                user.KichHoat = true;
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("DangNhap");
            }
            else
            {
                ViewBag.Error = "Đường dẫn kích hoạt tài khoản không hợp lệ hoặc đã hết hạn!";
                return View("DangNhap");
            }
        }

        public ActionResult XacNhanKichHoat(string username = "")
        {
            ViewBag.Username = username;
            ViewBag.Code = Request.QueryString["code"] as string;
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanKichHoat(string TenDangNhap, string MaKichHoat)
        {
            if (string.IsNullOrEmpty(TenDangNhap) || string.IsNullOrEmpty(MaKichHoat))
            {
                ViewBag.Error = "Tên đăng nhập và mã kích hoạt không được để trống!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaKichHoat;
                return View();
            }

            var user = db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap.Trim().ToLower() == TenDangNhap.Trim().ToLower());
            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại trong hệ thống!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaKichHoat;
                return View();
            }

            if (user.KichHoat == true)
            {
                TempData["SuccessMessage"] = "Tài khoản của bạn đã được kích hoạt từ trước. Bạn có thể đăng nhập ngay!";
                return RedirectToAction("DangNhap");
            }

            if (string.IsNullOrEmpty(user.MaKichHoat) || user.MaKichHoat.Trim() != MaKichHoat.Trim())
            {
                ViewBag.Error = "Mã kích hoạt không chính xác. Vui lòng kiểm tra lại Email!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaKichHoat;
                return View();
            }

            user.KichHoat = true;
            user.MaKichHoat = null;
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();

            // Tự động đăng nhập ngay sau khi kích hoạt thành công
            Session["TaiKhoan"] = user;
            PhanQuyen.RefreshPermissions();

            TempData["SuccessMessage"] = "Kích hoạt tài khoản thành công! Chào mừng bạn đến với Vương Bán Điện Tử.";
            string redirectTo = Url.Action("Index", "Home");
            if (PhanQuyen.HasPermission("TRUY_CAP_QUAN_TRI"))
            {
                if (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY)
                {
                    redirectTo = Url.Action("TongQuan", "QuanTri");
                }
                else
                {
                    redirectTo = Url.Action("Index", "QuanLySanPham");
                }
            }
            return Redirect(redirectTo);
        }

        public ActionResult XacNhanResetMatKhau(string username = "")
        {
            ViewBag.Username = username;
            ViewBag.Code = Request.QueryString["code"] as string;
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanResetMatKhau(string TenDangNhap, string MaQuenMatKhau, string MatKhauMoi, string NhapLaiMatKhauMoi)
        {
            if (string.IsNullOrEmpty(TenDangNhap) || string.IsNullOrEmpty(MaQuenMatKhau) || string.IsNullOrEmpty(MatKhauMoi) || string.IsNullOrEmpty(NhapLaiMatKhauMoi))
            {
                ViewBag.Error = "Tất cả các trường thông tin đều bắt buộc!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaQuenMatKhau;
                return View();
            }

            if (MatKhauMoi != NhapLaiMatKhauMoi)
            {
                ViewBag.Error = "Mật khẩu mới nhập lại không khớp!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaQuenMatKhau;
                return View();
            }

            var user = db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap.Trim().ToLower() == TenDangNhap.Trim().ToLower());
            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaQuenMatKhau;
                return View();
            }

            if (string.IsNullOrEmpty(user.MaQuenMatKhau) || user.MaQuenMatKhau.Trim() != MaQuenMatKhau.Trim())
            {
                ViewBag.Error = "Mã xác nhận không đúng hoặc đã hết hạn!";
                ViewBag.Username = TenDangNhap;
                ViewBag.Code = MaQuenMatKhau;
                return View();
            }

            user.MatKhau = MaHoa.ToSHA256(MatKhauMoi);
            user.MaQuenMatKhau = null;
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới để đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        [HttpPost]
        public ActionResult UploadAvatar(HttpPostedFileBase avatar)
        {
            if (Session["TaiKhoan"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });

            if (avatar == null || avatar.ContentLength == 0)
                return Json(new { success = false, message = "Vui lòng chọn một file ảnh hợp lệ!" });

            try
            {
                var userSession = (NguoiDung)Session["TaiKhoan"];
                string folderPath = Server.MapPath("~/Content/Avatars");
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string fileName = "avatar_" + userSession.MaNguoiDung + ".png";
                string physicalPath = System.IO.Path.Combine(folderPath, fileName);

                avatar.SaveAs(physicalPath);

                return Json(new { success = true, message = "Tải lên ảnh đại diện thành công!", avatarUrl = "/Content/Avatars/" + fileName + "?t=" + DateTime.Now.Ticks });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
