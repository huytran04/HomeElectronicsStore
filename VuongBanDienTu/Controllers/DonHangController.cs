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

            var orders = db.DonHangs.Where(o => o.MaKhachHang == user.MaNguoiDung && o.TrangThaiDonHang != "Chờ thanh toán")
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

        [HttpPost]
        public ActionResult HuyDon(int id, string lyDo)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var order = db.DonHangs.Find(id);
            if (order == null || order.MaKhachHang != user.MaNguoiDung)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.TrangThaiDonHang?.Trim() != "Chờ xử lý")
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xử lý'!" });

            bool daDangThanhToan = order.TrangThaiThanhToan?.Trim() == "Đã thanh toán";
            string lyDoGhiChu = string.IsNullOrEmpty(lyDo) ? "Khách hàng yêu cầu hủy" : lyDo;

            if (daDangThanhToan)
            {
                // Đơn đã thanh toán → chờ admin duyệt hoàn tiền
                order.TrangThaiDonHang = "Chờ hoàn tiền";
                order.GhiChu = lyDoGhiChu;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Yêu cầu hủy đơn #ORD-{order.MaDonHang}", $"Lý do: {order.GhiChu}", "Chờ hoàn tiền");

                var orderId = order.MaDonHang;
                var reason = order.GhiChu;
                System.Threading.Tasks.Task.Run(() => {
                    try {
                        using (var ctx = new VuongDienTuEntities())
                        {
                            var o = ctx.DonHangs
                                .Include("NguoiDung").Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham")
                                .FirstOrDefault(x => x.MaDonHang == orderId);
                            if (o != null)
                                VuongBanDienTu.Services.EmailService.SendRefundRequestEmail(o, reason);
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("HuyDon HoanTien: " + ex.Message); }
                });

                return Json(new { success = true, message = "Yêu cầu hủy đã được ghi nhận! Vì đơn hàng đã thanh toán, Admin sẽ xét duyệt và xác nhận hoàn tiền cho bạn sớm nhất." });
            }
            else
            {
                // Chưa thanh toán → hủy ngay, gửi email xác nhận cho khách
                order.TrangThaiDonHang = "Đã hủy";
                order.GhiChu = lyDoGhiChu;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Hủy đơn hàng #ORD-{order.MaDonHang}", $"Lý do: {order.GhiChu}", "Đã hủy");

                var orderId = order.MaDonHang;
                System.Threading.Tasks.Task.Run(() => {
                    try {
                        using (var ctx = new VuongDienTuEntities())
                        {
                            var o = ctx.DonHangs
                                .Include("NguoiDung").Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham")
                                .FirstOrDefault(x => x.MaDonHang == orderId);
                            if (o != null)
                                VuongBanDienTu.Services.EmailService.SendSelfCancelEmail(o);
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("HuyDon SelfCancel: " + ex.Message); }
                });

                return Json(new { success = true, message = "Hủy đơn hàng thành công! Email xác nhận đã được gửi đến bạn." });
            }
        }

        public ActionResult ChiTiet(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs
                        .Include("ChiTietDonHangs")
                        .Include("ChiTietDonHangs.SanPham")
                        .Include("ChiTietDonHangs.SanPham.HinhAnhSanPhams")
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
