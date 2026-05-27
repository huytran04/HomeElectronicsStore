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

        public ActionResult ThanhToan(string selectedIds = "")
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

            if (!string.IsNullOrEmpty(selectedIds))
            {
                var ids = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse)
                                     .ToList();
                lstGioHang = lstGioHang.Where(item => ids.Contains(item.MaSanPham.Value)).ToList();
            }

            if (lstGioHang.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                return RedirectToAction("Index");
            }

            ViewBag.SelectedIds = selectedIds;
            ViewBag.TongTien = lstGioHang.Sum(n => n.SoLuong * n.SanPham.GiaBan) ?? 0;
            return View(lstGioHang);
        }

        [HttpPost]
        public ActionResult DatHang(FormCollection f)
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("DangNhap", "TaiKhoan");

            NguoiDung user = (NguoiDung)Session["TaiKhoan"];
            List<GioHang> lstGioHang = LayGioHang();

            string selectedIds = f["selectedIds"] ?? "";
            List<GioHang> itemsToOrder = lstGioHang.ToList();

            if (!string.IsNullOrEmpty(selectedIds))
            {
                var ids = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse)
                                     .ToList();
                itemsToOrder = lstGioHang.Where(item => ids.Contains(item.MaSanPham.Value)).ToList();
            }

            if (itemsToOrder.Count == 0)
            {
                TempData["Error"] = "Đơn hàng không có sản phẩm nào hợp lệ!";
                return RedirectToAction("Index");
            }

            var freshProducts = new Dictionary<int, SanPham>();
            decimal tongTien = 0;

            foreach (var item in itemsToOrder)
            {
                var sp = db.SanPhams.Find(item.MaSanPham);
                if (sp == null || sp.SoLuongTon < item.SoLuong)
                {
                    TempData["Error"] = $"Sản phẩm '{(sp != null ? sp.TenSanPham : "Không xác định")}' đã hết hàng hoặc không đủ số lượng!";
                    return RedirectToAction("Index");
                }
                freshProducts[sp.MaSanPham] = sp;
                tongTien += (item.SoLuong ?? 0) * sp.GiaBan;
            }

            string phuongThuc = f["PhuongThucThanhToan"] ?? "COD";

            DonHang dh = new DonHang();
            dh.MaKhachHang = user.MaNguoiDung;
            dh.NgayDat = DateTime.Now;
            dh.TongTien = tongTien;
            dh.DiaChiGiaoHang = f["DiaChi"] ?? user.DiaChi;
            dh.TrangThaiDonHang = (phuongThuc == "VNPAY") ? "Chờ thanh toán" : "Chờ xử lý";
            dh.TrangThaiThanhToan = "Chưa thanh toán";
            dh.PhuongThucThanhToan = phuongThuc;
            
            db.DonHangs.Add(dh);
            db.SaveChanges();
            VuongBanDienTu.Helpers.ActivityLogger.Log($"Đặt đơn hàng mới #ORD-{dh.MaDonHang}", $"Phương thức: {dh.PhuongThucThanhToan}, Tổng tiền: {dh.TongTien:N0}₫", dh.TrangThaiDonHang);

            foreach (var item in itemsToOrder)
            {
                var sp = freshProducts[item.MaSanPham.Value];
                ChiTietDonHang ctdh = new ChiTietDonHang();
                ctdh.MaDonHang = dh.MaDonHang;
                ctdh.MaSanPham = item.MaSanPham;
                ctdh.SoLuong = (int)item.SoLuong;
                ctdh.GiaLuuTru = sp.GiaBan;
                db.ChiTietDonHangs.Add(ctdh);
            }
            db.SaveChanges();

            var orderId = dh.MaDonHang;
            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    using (var context = new VuongDienTuEntities())
                    {
                        var orderToSend = context.DonHangs
                            .Include("NguoiDung")
                            .Include("ChiTietDonHangs")
                            .Include("ChiTietDonHangs.SanPham")
                            .FirstOrDefault(o => o.MaDonHang == orderId);
                        if (orderToSend != null)
                        {
                            VuongBanDienTu.Services.EmailService.SendOrderEmail(orderToSend, "DatHang");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DatHang Email Error: " + ex.Message);
                }
            });

            // Chỉ xóa những sản phẩm đã đặt ra khỏi giỏ hàng
            foreach (var item in itemsToOrder)
            {
                lstGioHang.RemoveAll(x => x.MaSanPham == item.MaSanPham);
            }
            Session["GioHang"] = lstGioHang;

            if (dh.PhuongThucThanhToan == "VNPAY")
            {
                return RedirectToAction("ThanhToanVnPay", "ThanhToan", new { orderId = dh.MaDonHang });
            }

            return View("ThanhCong", dh);
        }
    }
}
