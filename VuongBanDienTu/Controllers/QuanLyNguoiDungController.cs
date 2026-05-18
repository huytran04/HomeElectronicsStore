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
            return user != null && (user.MaVaiTro == 1 || user.MaVaiTro == 2);
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
                    // Nếu là nhân viên mà cố vào trang này, thông báo lỗi thay vì đá về Home
                    TempData["Error"] = "Bạn không có quyền truy cập khu vực này!";
                    filterContext.Result = RedirectToAction("TongQuan", "QuanTri");
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
                var user = db.NguoiDungs.Find(maND);
                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                // Kiểm tra vai trò mới có tồn tại không
                var roleExists = db.VaiTroes.Any(v => v.MaVaiTro == maVT);
                if (!roleExists) return Json(new { success = false, message = "Vai trò không hợp lệ!" });

                user.MaVaiTro = maVT;
                
                // Ép EF đánh dấu là đã thay đổi để lưu vào DB
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
    }
}
