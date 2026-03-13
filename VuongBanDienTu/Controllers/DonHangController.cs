using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class DonHangController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult TimKiem(int maDon, string sdt)
        {
            var order = db.DonHangs.Include("NguoiDung").Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham")
                        .FirstOrDefault(o => o.MaDonHang == maDon);

            if (order != null)
            {
                bool isValid = false;
                if (order.NguoiDung != null && order.NguoiDung.SoDienThoai.Trim() == sdt.Trim())
                {
                    isValid = true;
                }
                
                if (isValid)
                {
                    return View("KetQua", order);
                }
            }

            ViewBag.Error = "Không tìm thấy đơn hàng khớp với mã đơn và số điện thoại!";
            ViewBag.MaDon = maDon;
            ViewBag.SDT = sdt;
            return View("Index");
        }

        public ActionResult LichSu()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var orders = db.DonHangs.Where(o => o.MaKhachHang == user.MaNguoiDung)
                        .OrderByDescending(o => o.NgayDat).ToList();
            return View(orders);
        }
        
        [HttpPost]
        public ActionResult XacNhanThanhToan(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var order = db.DonHangs.Find(id);
            if (order != null && order.MaKhachHang == user.MaNguoiDung)
            {
                order.TrangThaiThanhToan = "Đã thanh toán";
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        public ActionResult ChiTiet(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham")
                        .FirstOrDefault(o => o.MaDonHang == id);

            if (order == null) return HttpNotFound();
            
            if (user == null) {
                 return RedirectToAction("Index");
            }

            if (order.MaKhachHang != user.MaNguoiDung) return HttpNotFound();

            return View(order);
        }
    }
}
