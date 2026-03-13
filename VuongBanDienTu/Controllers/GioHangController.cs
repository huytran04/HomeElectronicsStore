using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class GioHangController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public List<GioHang> LayGioHang()
        {
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang == null)
            {
                lstGioHang = new List<GioHang>();
                Session["GioHang"] = lstGioHang;
            }
            return lstGioHang;
        }

        [HttpPost]
        public ActionResult Them(int id, int sl = 1)
        {
            var sp = db.SanPhams.Find(id);
            if (sp == null) return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            
            if (sp.SoLuongTon < sl)
            {
                return Json(new { success = false, message = "Hết hàng hoặc số lượng tồn không đủ!" });
            }

            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.Find(n => n.MaSanPham == id);
            
            if (sanpham == null)
            {
                sanpham = new GioHang
                {
                    MaSanPham = id,
                    SoLuong = sl,
                    NgayThem = DateTime.Now,
                    SanPham = sp
                };
                lstGioHang.Add(sanpham);
            }
            else
            {
                if (sp.SoLuongTon < (sanpham.SoLuong + sl))
                {
                    return Json(new { success = false, message = "Không thể thêm! Vượt quá số lượng sản phẩm trong kho." });
                }
                sanpham.SoLuong += sl;
            }

            return Json(new { success = true, totalItems = lstGioHang.Sum(n => n.SoLuong) });
        }

        public ActionResult Index()
        {
            List<GioHang> lstGioHang = LayGioHang();
            ViewBag.TongTien = lstGioHang.Sum(n => n.SoLuong * n.SanPham.GiaBan) ?? 0;
            return View(lstGioHang);
        }

        [HttpPost]
        public ActionResult CapNhat(int id, int sl)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.Find(n => n.MaSanPham == id);
            
            if (sanpham != null)
            {
                var sp = db.SanPhams.Find(id);
                if (sp == null) return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

                if (sl > sp.SoLuongTon)
                {
                    return Json(new { success = false, message = $"Số lượng trong kho chỉ còn {sp.SoLuongTon}!" });
                }

                sanpham.SoLuong = sl;
                if (sanpham.SoLuong <= 0)
                {
                    lstGioHang.RemoveAll(n => n.MaSanPham == id);
                }
            }

            return Json(new { 
                success = true, 
                totalItems = lstGioHang.Sum(n => n.SoLuong),
                tongTien = (lstGioHang.Sum(n => n.SoLuong * n.SanPham.GiaBan) ?? 0).ToString("N0") + "₫"
            });
        }

        public ActionResult Giam(int id)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.Find(n => n.MaSanPham == id);
            
            if (sanpham != null)
            {
                sanpham.SoLuong--;
                if (sanpham.SoLuong <= 0)
                {
                    lstGioHang.RemoveAll(n => n.MaSanPham == id);
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult Xoa(int id)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sp = lstGioHang.SingleOrDefault(n => n.MaSanPham == id);
            if (sp != null)
            {
                lstGioHang.RemoveAll(n => n.MaSanPham == id);
            }
            return RedirectToAction("Index");
        }

        public ActionResult ThanhToan()
        {
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            List<GioHang> lstGioHang = LayGioHang();
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "SanPham");
            }

            ViewBag.TongTien = lstGioHang.Sum(n => n.SoLuong * n.SanPham.GiaBan);
            return View(lstGioHang);
        }

        [HttpPost]
        public ActionResult DatHang(FormCollection f)
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("DangNhap", "TaiKhoan");

            NguoiDung user = (NguoiDung)Session["TaiKhoan"];
            List<GioHang> lstGioHang = LayGioHang();

            // Kiểm tra tồn kho lần cuối
            foreach (var item in lstGioHang)
            {
                var sp = db.SanPhams.Find(item.MaSanPham);
                if (sp == null || sp.SoLuongTon < item.SoLuong)
                {
                    TempData["Error"] = $"Sản phẩm '{item.SanPham.TenSanPham}' đã hết hàng hoặc không đủ số lượng!";
                    return RedirectToAction("Index");
                }
            }

            DonHang dh = new DonHang();
            dh.MaKhachHang = user.MaNguoiDung;
            dh.NgayDat = DateTime.Now;
            dh.TongTien = (decimal)lstGioHang.Sum(n => n.SoLuong * n.SanPham.GiaBan);
            dh.DiaChiGiaoHang = f["DiaChi"] ?? user.DiaChi;
            dh.TrangThaiDonHang = "Chờ xử lý";
            dh.TrangThaiThanhToan = "Chưa thanh toán";
            dh.PhuongThucThanhToan = f["PhuongThucThanhToan"] ?? "COD";
            
            db.DonHangs.Add(dh);
            db.SaveChanges();

            foreach (var item in lstGioHang)
            {
                ChiTietDonHang ctdh = new ChiTietDonHang();
                ctdh.MaDonHang = dh.MaDonHang;
                ctdh.MaSanPham = item.MaSanPham;
                ctdh.SoLuong = (int)item.SoLuong;
                ctdh.GiaLuuTru = (decimal)item.SanPham.GiaBan;
                db.ChiTietDonHangs.Add(ctdh);
                
                // Lưu ý: Không trừ tồn kho ở đây, trừ khi duyệt đơn để đảm bảo giữ chỗ tạm thời?
                // Tuy nhiên theo luồng trước đó thì trừ ở Duyệt nên ở đây giữ nguyên.
            }
            db.SaveChanges();

            Session["GioHang"] = null;

            return View("ThanhCong", dh);
        }
    }
}
