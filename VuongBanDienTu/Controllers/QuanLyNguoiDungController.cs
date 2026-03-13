using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Helpers;

namespace VuongBanDienTu.Controllers
{
    public class QuanLyNguoiDungController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsAdmin()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && user.MaVaiTro == 1;
        }

        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            
            var users = db.NguoiDungs.Include("VaiTro").OrderByDescending(u => u.MaNguoiDung).ToList();
            ViewBag.Roles = db.VaiTroes.ToList();
            return View(users);
        }

        [HttpPost]
        public ActionResult TaoNhanVien(NguoiDung user)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Bạn không có quyền này!" });

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
            if (!IsAdmin()) return Json(new { success = false, message = "Bạn không có quyền này!" });

            var user = db.NguoiDungs.Find(id);
            if (user != null)
            {
                user.TrangThai = !user.TrangThai;
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
