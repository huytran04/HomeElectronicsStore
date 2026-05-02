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
                var check = db.NguoiDungs.FirstOrDefault(s => s.TenDangNhap == user.TenDangNhap);
                if (check == null)
                {
                    user.MatKhau = MaHoa.ToSHA256(user.MatKhau);
                    user.NgayTao = DateTime.Now;
                    user.MaVaiTro = PhanQuyen.KHACH_HANG;
                    user.TrangThai = true;
                    
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.NguoiDungs.Add(user);
                    db.SaveChanges();

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Đăng ký thành công!" });
                    }
                    return RedirectToAction("DangNhap");
                }
                else
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
                    }
                    ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                    return View();
                }
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
                if (user.TrangThai == false)
                {
                    if (Request.IsAjaxRequest()) return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa!" });
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                Session["TaiKhoan"] = user;
                PhanQuyen.RefreshPermissions();
                
                string redirectUrl = Url.Action("Index", "Home");
                if (PhanQuyen.HasPermission("TRUY_CAP_QUAN_TRI"))
                {
                    redirectUrl = Url.Action("TongQuan", "QuanTri");
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
    }
}
