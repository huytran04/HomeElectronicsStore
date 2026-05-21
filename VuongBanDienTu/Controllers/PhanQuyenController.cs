using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Helpers;

namespace VuongBanDienTu.Controllers
{
    public class PhanQuyenController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsAdmin()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && PhanQuyen.IsAdmin(user.MaVaiTro);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                if (Request.IsAjaxRequest())
                {
                    filterContext.Result = Json(new { success = false, message = "Bạn không có quyền quản lý phân quyền!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    TempData["Error"] = "Chỉ Admin mới có quyền truy cập mục Phân quyền!";
                    var user = Session["TaiKhoan"] as NguoiDung;
                    if (user != null && user.MaVaiTro == PhanQuyen.QUAN_LY)
                    {
                        filterContext.Result = RedirectToAction("TongQuan", "QuanTri");
                    }
                    else
                    {
                        filterContext.Result = RedirectToAction("Index", "QuanLySanPham");
                    }
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {

            ViewBag.Roles = db.VaiTroes.Include("QuyenHans").ToList();
            ViewBag.Permissions = db.QuyenHans.ToList();
            
            return View();
        }

        [HttpPost]
        public ActionResult UpdatePermission(int maVaiTro, int maQuyen, bool hasPermission)
        {

            var role = db.VaiTroes.Include("QuyenHans").FirstOrDefault(r => r.MaVaiTro == maVaiTro);
            var permission = db.QuyenHans.Find(maQuyen);

            if (role != null && permission != null)
            {
                if (hasPermission)
                {
                    if (!role.QuyenHans.Any(p => p.MaQuyen == maQuyen))
                    {
                        role.QuyenHans.Add(permission);
                    }
                }
                else
                {
                    var pToRemove = role.QuyenHans.FirstOrDefault(p => p.MaQuyen == maQuyen);
                    if (pToRemove != null)
                    {
                        role.QuyenHans.Remove(pToRemove);
                    }
                }
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }

        [HttpPost]
        public ActionResult CreatePermission(string tenQuyen, string code)
        {

            if (db.QuyenHans.Any(q => q.Code == code))
            {
                return Json(new { success = false, message = "Mã quyền này đã tồn tại!" });
            }

            var newPerm = new QuyenHan { TenQuyen = tenQuyen, Code = code };
            db.QuyenHans.Add(newPerm);
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult DeletePermission(int id)
        {

            var perm = db.QuyenHans.Find(id);
            if (perm != null)
            {
                db.QuyenHans.Remove(perm);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
