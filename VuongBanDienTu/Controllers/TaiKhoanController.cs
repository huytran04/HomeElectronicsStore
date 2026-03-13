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
                    user.MaVaiTro = 4;
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
                
                string redirectUrl = Url.Action("Index", "Home");
                if (user.MaVaiTro == 1 || user.MaVaiTro == 2 || user.MaVaiTro == 3)
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
    }
}
