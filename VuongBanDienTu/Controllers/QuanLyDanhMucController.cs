using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class QuanLyDanhMucController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsInternal()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && (user.MaVaiTro == 1 || user.MaVaiTro == 2 || user.MaVaiTro == 3);
        }

        public ActionResult Index()
        {
            if (!IsInternal()) return RedirectToAction("DangNhap", "TaiKhoan");
            
            var categories = db.DanhMucs.Include("DanhMuc2").OrderByDescending(d => d.MaDanhMuc).ToList();
            ViewBag.ParentCategories = db.DanhMucs.Where(d => d.MaDanhMucCha == null).ToList();
            return View(categories);
        }

        [HttpPost]
        public ActionResult Luu(DanhMuc dm)
        {
            if (!IsInternal()) return Json(new { success = false, message = "Bạn không có quyền này!" });

            try
            {
                if (dm.MaDanhMuc > 0)
                {
                    var existing = db.DanhMucs.Find(dm.MaDanhMuc);
                    if (existing != null)
                    {
                        existing.TenDanhMuc = dm.TenDanhMuc;
                        existing.MaDanhMucCha = dm.MaDanhMucCha;
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
            if (!IsInternal()) return Json(new { success = false, message = "Bạn không có quyền này!" });

            var dm = db.DanhMucs.Find(id);
            if (dm != null)
            {
                if (dm.SanPhams.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục đang có sản phẩm!" });
                }
                if (dm.DanhMuc1.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục đang có danh mục con!" });
                }
                db.DanhMucs.Remove(dm);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy danh mục!" });
        }
    }
}
