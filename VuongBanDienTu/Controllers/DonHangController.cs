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

            string currentStatus = order.TrangThaiDonHang?.Trim();
            string[] allowedToCancel = { "Chờ xử lý", "Đã xác nhận", "Đang giao" };
            if (!allowedToCancel.Contains(currentStatus))
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng khi chưa hoàn thành hoặc chưa bị hủy!" });

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
                // Đơn COD / Chưa thanh toán -> chờ admin duyệt hủy
                order.TrangThaiDonHang = "Chờ duyệt hủy";
                order.GhiChu = lyDoGhiChu;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Yêu cầu hủy đơn #ORD-{order.MaDonHang}", $"Lý do: {order.GhiChu}", "Chờ duyệt hủy");

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
                                // Gửi email thông báo cho Admin có yêu cầu hủy đơn COD
                                VuongBanDienTu.Services.EmailService.SendRefundRequestEmail(o, reason); // Tạm dùng chung mẫu request vì nội dung tương tự (cần admin xử lý)
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("HuyDon COD Request: " + ex.Message); }
                });

                return Json(new { success = true, message = "Yêu cầu hủy đơn hàng đã được gửi! Admin sẽ sớm xem xét và xác nhận cho bạn." });
            }
        }

        [HttpPost]
        public ActionResult YeuCauTraHang(int id, string lyDo)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var order = db.DonHangs.Find(id);
            if (order == null || order.MaKhachHang != user.MaNguoiDung)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.TrangThaiDonHang?.Trim() != "Hoàn thành")
                return Json(new { success = false, message = "Chỉ có thể yêu cầu trả hàng cho các đơn hàng đã 'Hoàn thành'!" });

            if (string.IsNullOrWhiteSpace(lyDo))
                return Json(new { success = false, message = "Vui lòng nhập lý do trả hàng!" });

            order.TrangThaiDonHang = "Yêu cầu trả hàng";
            order.GhiChu = lyDo.Trim();
            db.SaveChanges();

            VuongBanDienTu.Helpers.ActivityLogger.Log($"Yêu cầu trả hàng đơn #ORD-{order.MaDonHang}", $"Lý do: {order.GhiChu}", "Yêu cầu trả hàng");

            var orderId = order.MaDonHang;
            var reason = order.GhiChu;
            System.Threading.Tasks.Task.Run(() => {
                try
                {
                    using (var ctx = new VuongDienTuEntities())
                    {
                        var o = ctx.DonHangs
                            .Include("NguoiDung").Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham")
                            .FirstOrDefault(x => x.MaDonHang == orderId);
                        if (o != null)
                            VuongBanDienTu.Services.EmailService.SendReturnRequestEmail(o, reason);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("YeuCauTraHang Email Error: " + ex.Message); }
            });

            return Json(new { success = true, message = "Yêu cầu trả hàng của bạn đã được gửi đi! Admin sẽ sớm xem xét và phản hồi qua email." });
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
