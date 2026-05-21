using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Helpers;

namespace VuongBanDienTu.Controllers
{
    public class QuanLyDanhMucController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsInternal()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user == null || user.MaVaiTro == null) return false;
            return user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsInternal())
            {
                if (Request.IsAjaxRequest())
                {
                    filterContext.Result = Json(new { success = false, message = "Bạn không có quyền này!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var user = Session["TaiKhoan"] as NguoiDung;
                    if (user != null && PhanQuyen.IsStaff(user.MaVaiTro))
                    {
                        TempData["Error"] = "Bạn không có quyền truy cập quản lý danh mục!";
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
            var categories = db.DanhMucs.OrderByDescending(d => d.MaDanhMuc).ToList();
            return View(categories);
        }

        [HttpPost]
        public ActionResult Luu(DanhMuc dm)
        {

            try
            {
                if (dm.MaDanhMuc > 0)
                {
                    var existing = db.DanhMucs.Find(dm.MaDanhMuc);
                    if (existing != null)
                    {
                        existing.TenDanhMuc = dm.TenDanhMuc;
                        existing.MoTa = dm.MoTa;
                        db.SaveChanges();
                    }
                }
                else
                {
                    db.DanhMucs.Add(dm);
                    db.SaveChanges();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult Xoa(int id)
        {
            var dm = db.DanhMucs.Find(id);
            if (dm != null)
            {
                if (dm.SanPhams.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục đang có sản phẩm!" });
                }
                db.DanhMucs.Remove(dm);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy danh mục!" });
        }
    }
}
