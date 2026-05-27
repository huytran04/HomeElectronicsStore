using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Helpers;
using System.Data.Entity;
using System.Data;

namespace VuongBanDienTu.Controllers
{
    public class QuanLyNguoiDungController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsAuthorized()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAuthorized())
            {
                if (Request.IsAjaxRequest())
                {
                    filterContext.Result = Json(new { success = false, message = "Bạn không có quyền quản lý người dùng!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var user = Session["TaiKhoan"] as NguoiDung;
                    if (user != null && PhanQuyen.IsStaff(user.MaVaiTro))
                    {
                        TempData["Error"] = "Bạn không có quyền truy cập quản lý người dùng!";
                        filterContext.Result = RedirectToAction("Index", "QuanLySanPham");
                    }
                    else
                    {
                        filterContext.Result = RedirectToAction("DangNhap", "TaiKhoan");
                    }
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            var users = db.NguoiDungs.Include("VaiTro").OrderByDescending(u => u.MaNguoiDung).ToList();
            ViewBag.Roles = db.VaiTroes.ToList();
            return View(users);
        }

        [HttpPost]
        public ActionResult TaoNhanVien(NguoiDung user)
        {
            var currentUser = Session["TaiKhoan"] as NguoiDung;
            if (currentUser != null && currentUser.MaVaiTro == PhanQuyen.QUAN_LY && user.MaVaiTro == PhanQuyen.ADMIN)
            {
                return Json(new { success = false, message = "Quản lý không thể tạo tài khoản Admin!" });
            }

            if (ModelState.IsValid)
            {
                var check = db.NguoiDungs.FirstOrDefault(s => s.TenDangNhap == user.TenDangNhap);
                if (check == null)
                {
                    user.MatKhau = MaHoa.ToSHA256(user.MatKhau);
                    user.NgayTao = DateTime.Now;
                    user.TrangThai = true;
                    
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.NguoiDungs.Add(user);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }

        [HttpPost]
        public ActionResult DoiTrangThai(int id)
        {
            try
            {
                var user = db.NguoiDungs.Find(id);
                if (user != null)
                {
                    user.TrangThai = !user.TrangThai;
                    db.Entry(user).State = EntityState.Modified;
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateUserRole(int maND, int maVT)
        {
            try
            {
                var currentUser = Session["TaiKhoan"] as NguoiDung;
                if (currentUser == null) return Json(new { success = false, message = "Hết phiên đăng nhập!" });

                if (currentUser.MaNguoiDung == maND)
                {
                    return Json(new { success = false, message = "Bạn không thể tự thay đổi vai trò của chính mình!" });
                }

                if (currentUser.MaVaiTro == PhanQuyen.QUAN_LY)
                {
                    if (maVT == PhanQuyen.ADMIN)
                    {
                        return Json(new { success = false, message = "Quản lý không thể chỉ định vai trò Admin!" });
                    }
                    var target = db.NguoiDungs.Find(maND);
                    if (target != null && target.MaVaiTro == PhanQuyen.ADMIN)
                    {
                        return Json(new { success = false, message = "Quản lý không thể thay đổi vai trò của Admin!" });
                    }
                }

                var user = db.NguoiDungs.Find(maND);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                var roleExists = db.VaiTroes.Any(v => v.MaVaiTro == maVT);
                if (!roleExists) return Json(new { success = false, message = "Vai trò không hợp lệ!" });

                user.MaVaiTro = maVT;
                
                db.Entry(user).State = EntityState.Modified;
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult XoaNguoiDung(int id)
        {
            try
            {
                var currentUser = Session["TaiKhoan"] as NguoiDung;
                if (currentUser != null && currentUser.MaNguoiDung == id)
                {
                    return Json(new { success = false, message = "Bạn không thể tự xử lý tài khoản của chính mình!" });
                }

                var user = db.NguoiDungs.Find(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }

                if (currentUser != null && currentUser.MaVaiTro == PhanQuyen.QUAN_LY && user.MaVaiTro == PhanQuyen.ADMIN)
                {
                    return Json(new { success = false, message = "Quản lý không thể xóa hoặc khóa tài khoản Admin!" });
                }

                if (user.MaVaiTro == 4)
                {
                    user.TrangThai = false;
                    db.Entry(user).State = EntityState.Modified;
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã khóa hoạt động tài khoản khách hàng thành công!" });
                }
                else
                {
                    bool hasProcessedOrders = db.DonHangs.Any(o => o.MaNhanVienXuLy == id);
                    if (hasProcessedOrders)
                    {
                        user.TrangThai = false;
                        db.Entry(user).State = EntityState.Modified;
                        db.Configuration.ValidateOnSaveEnabled = false;
                        db.SaveChanges();
                        return Json(new { success = true, message = "Nhân sự này đã có lịch sử xử lý đơn hàng. Để bảo toàn lịch sử hóa đơn, tài khoản đã được chuyển sang trạng thái 'Bị khóa' thay vì xóa cứng!" });
                    }

                    var carts = db.GioHangs.Where(g => g.MaNguoiDung == id).ToList();
                    foreach (var cart in carts)
                    {
                        db.GioHangs.Remove(cart);
                    }

                    db.NguoiDungs.Remove(user);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xóa vĩnh viễn tài khoản nhân sự khỏi hệ thống!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xử lý xóa người dùng: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ChiTietNguoiDung(int id)
        {
            try
            {
                var user = db.NguoiDungs.Include("VaiTro").FirstOrDefault(u => u.MaNguoiDung == id);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng!" }, JsonRequestBehavior.AllowGet);

                var currentUser = Session["TaiKhoan"] as NguoiDung;
                if (currentUser != null && currentUser.MaVaiTro == PhanQuyen.QUAN_LY && user.MaVaiTro == PhanQuyen.ADMIN)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem thông tin tài khoản Admin!" }, JsonRequestBehavior.AllowGet);
                }

                int orderCount = db.DonHangs.Count(o => o.MaKhachHang == id && o.TrangThaiDonHang != "Chờ thanh toán");
                decimal totalSpent = db.DonHangs.Where(o => o.MaKhachHang == id && o.TrangThaiDonHang == "Đã xác nhận").Sum(o => (decimal?)o.TongTien) ?? 0;

                var result = new
                {
                    success = true,
                    MaNguoiDung = user.MaNguoiDung,
                    TenDangNhap = user.TenDangNhap,
                    HoTen = user.HoTen,
                    Email = user.Email,
                    SoDienThoai = user.SoDienThoai,
                    DiaChi = user.DiaChi ?? "Chưa cập nhật",
                    MaVaiTro = user.MaVaiTro,
                    TenVaiTro = user.VaiTro?.TenVaiTro ?? "Chưa xác định",
                    NgayTaoStr = user.NgayTao.HasValue ? user.NgayTao.Value.ToString("dd/MM/yyyy HH:mm") : "---",
                    TrangThaiStr = user.TrangThai == true ? "Hoạt động" : "Bị khóa",
                    IsLocked = user.TrangThai != true,
                    OrderCount = orderCount,
                    TotalSpentStr = totalSpent.ToString("N0") + "₫"
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SuaNhanVien(NguoiDung user)
        {
            try
            {
                if (user == null || user.MaNguoiDung <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var existingUser = db.NguoiDungs.Find(user.MaNguoiDung);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }

                var currentUser = Session["TaiKhoan"] as NguoiDung;
                if (currentUser != null)
                {
                    if (currentUser.MaNguoiDung == user.MaNguoiDung && existingUser.MaVaiTro != user.MaVaiTro)
                    {
                        return Json(new { success = false, message = "Bạn không thể tự thay đổi vai trò của chính mình!" });
                    }
                    if (currentUser.MaVaiTro == PhanQuyen.QUAN_LY)
                    {
                        if (user.MaVaiTro == PhanQuyen.ADMIN)
                        {
                            return Json(new { success = false, message = "Quản lý không thể chỉ định vai trò Admin!" });
                        }
                        if (existingUser.MaVaiTro == PhanQuyen.ADMIN)
                        {
                            return Json(new { success = false, message = "Quản lý không thể chỉnh sửa thông tin của Admin!" });
                        }
                    }
                }

                var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯưẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỶỷỸỹỴỵ\s]+$");
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^0[35789]\d{8}$");

                if (string.IsNullOrEmpty(user.HoTen) || !nameRegex.IsMatch(user.HoTen))
                {
                    return Json(new { success = false, message = "Họ và tên chỉ được nhập chữ, không được chứa số hay ký tự đặc biệt!" });
                }

                if (string.IsNullOrEmpty(user.SoDienThoai) || !phoneRegex.IsMatch(user.SoDienThoai))
                {
                    return Json(new { success = false, message = "Số điện thoại không đúng định dạng! Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số." });
                }

                if (!string.IsNullOrEmpty(user.Email))
                {
                    var checkEmail = db.NguoiDungs.FirstOrDefault(s => s.Email == user.Email && s.MaNguoiDung != user.MaNguoiDung);
                    if (checkEmail != null)
                    {
                        return Json(new { success = false, message = "Email này đã được đăng ký bởi tài khoản khác!" });
                    }
                }

                string matKhauMoi = Request.Form["MatKhauMoi"];
                if (!string.IsNullOrEmpty(matKhauMoi))
                {
                    if (matKhauMoi.Length < 8)
                    {
                        return Json(new { success = false, message = "Mật khẩu mới phải tối thiểu 8 ký tự!" });
                    }

                    var hasUpperCase = new System.Text.RegularExpressions.Regex(@"[A-Z]");
                    var hasLowerCase = new System.Text.RegularExpressions.Regex(@"[a-z]");
                    var hasSpecialChar = new System.Text.RegularExpressions.Regex(@"[^a-zA-Z0-9]");

                    if (!hasUpperCase.IsMatch(matKhauMoi) || !hasLowerCase.IsMatch(matKhauMoi) || !hasSpecialChar.IsMatch(matKhauMoi))
                    {
                        return Json(new { success = false, message = "Mật khẩu mới phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 ký tự đặc biệt!" });
                    }

                    existingUser.MatKhau = MaHoa.ToSHA256(matKhauMoi);
                }

                var roleExists = db.VaiTroes.Any(v => v.MaVaiTro == user.MaVaiTro && v.MaVaiTro != PhanQuyen.KHACH_HANG);
                if (!roleExists)
                {
                    return Json(new { success = false, message = "Vai trò không hợp lệ!" });
                }

                existingUser.HoTen = user.HoTen;
                existingUser.Email = user.Email;
                existingUser.SoDienThoai = user.SoDienThoai;
                existingUser.MaVaiTro = user.MaVaiTro;

                db.Entry(existingUser).State = EntityState.Modified;
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật thông tin nhân viên: " + ex.Message });
            }
        }
    }
}
