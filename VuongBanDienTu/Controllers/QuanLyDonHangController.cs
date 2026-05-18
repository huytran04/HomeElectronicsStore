using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Helpers;

namespace VuongBanDienTu.Controllers
{
    public class QuanLyDonHangController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsInternal()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && PhanQuyen.IsStaff(user.MaVaiTro);
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsInternal())
            {
                if (Request.IsAjaxRequest())
                {
                    filterContext.Result = Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    filterContext.Result = RedirectToAction("DangNhap", "TaiKhoan");
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            var orders = db.DonHangs
                .Include("NguoiDung")
                .Include("NguoiDung1")
                .Include("ChiTietDonHangs")
                .Include("ChiTietDonHangs.SanPham")
                .OrderByDescending(o => o.NgayDat).ToList();
            return View(orders);
        }

        public ActionResult GetChiTiet(int id)
        {
            var order = db.DonHangs
                .Include("ChiTietDonHangs")
                .Include("ChiTietDonHangs.SanPham")
                .FirstOrDefault(o => o.MaDonHang == id);

            if (order == null) return HttpNotFound();

            return PartialView("_ChiTietDonHangPartial", order);
        }

        [HttpPost]
        public ActionResult Duyet(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham").FirstOrDefault(o => o.MaDonHang == id);

            if (order != null)
            {
                if (order.TrangThaiDonHang?.Trim() == "Đã xác nhận")
                    return Json(new { success = false, message = "Đơn hàng này đã được duyệt trước đó!" });

                foreach (var ct in order.ChiTietDonHangs)
                {
                    if (ct.SanPham.SoLuongTon < ct.SoLuong)
                    {
                        return Json(new { success = false, message = $"Sản phẩm '{ct.SanPham.TenSanPham}' không đủ tồn kho (Hiện có: {ct.SanPham.SoLuongTon})!" });
                    }
                }

                foreach (var ct in order.ChiTietDonHangs)
                {
                    ct.SanPham.SoLuongTon -= ct.SoLuong;
                }

                order.TrangThaiDonHang = "Đã xác nhận";
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        [HttpPost]
        public ActionResult Huy(int id, string lyDo)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                order.TrangThaiDonHang = "Đã hủy";
                order.GhiChu = lyDo;
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
