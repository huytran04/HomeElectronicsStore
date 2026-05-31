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
            if (user == null || user.MaVaiTro == null) return false;
            return user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY || user.MaVaiTro == PhanQuyen.NHAN_VIEN;
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
                    var user = Session["TaiKhoan"] as NguoiDung;
                    if (user != null && PhanQuyen.IsStaff(user.MaVaiTro))
                    {
                        TempData["Error"] = "Bạn không có quyền truy cập quản lý đơn hàng!";
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
            var orders = db.DonHangs
                .Include("NguoiDung")
                .Include("NguoiDung1")
                .Include("ChiTietDonHangs")
                .Include("ChiTietDonHangs.SanPham")
                .Where(o => o.TrangThaiDonHang != "Chờ thanh toán")
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
                // Removed restriction: if (order.TrangThaiDonHang?.Trim() == "Đã xác nhận")

                foreach (var ct in order.ChiTietDonHangs)
                {
                    if (ct.SanPham.SoLuongTon < ct.SoLuong)
                    {
                        return Json(new { success = false, message = $"Sản phẩm '{ct.SanPham.TenSanPham}' không đủ tồn kho (Hiện có: {ct.SanPham.SoLuongTon})!" });
                    }
                }

                if (order.TrangThaiDonHang != "Đã xác nhận")
                {
                    foreach (var ct in order.ChiTietDonHangs)
                    {
                        ct.SanPham.SoLuongTon -= ct.SoLuong;
                    }
                }

                order.TrangThaiDonHang = "Đã xác nhận";
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Duyệt đơn hàng #ORD-{order.MaDonHang}", $"Nhân viên duyệt: {user.HoTen}", "Đã xác nhận");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham").FirstOrDefault(o => o.MaDonHang == id);
            
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            string oldStatus = order.TrangThaiDonHang?.Trim();
            string newStatus = status.Trim();

            if (oldStatus == newStatus) return Json(new { success = true });

            // Xử lý tồn kho khi chuyển TRẠNG THÁI
            // 1. Nếu chuyển SANG "Đã xác nhận", "Đang giao", "Hoàn thành" mà trước đó KHÔNG PHẢI nhóm này -> Trừ tồn kho
            string[] confirmedGroup = { "Đã xác nhận", "Đang giao", "Hoàn thành" };
            bool wasConfirmed = confirmedGroup.Contains(oldStatus);
            bool isNewConfirmed = confirmedGroup.Contains(newStatus);

            if (!wasConfirmed && isNewConfirmed)
            {
                // Trừ tồn kho
                foreach (var ct in order.ChiTietDonHangs)
                {
                    if (ct.SanPham.SoLuongTon < ct.SoLuong)
                        return Json(new { success = false, message = $"Sản phẩm '{ct.SanPham.TenSanPham}' không đủ tồn kho!" });
                    ct.SanPham.SoLuongTon -= ct.SoLuong;
                }
            }
            // 2. Nếu chuyển TỪ nhóm xác nhận SANG nhóm hủy/chờ -> Hoàn tồn kho
            else if (wasConfirmed && !isNewConfirmed)
            {
                foreach (var ct in order.ChiTietDonHangs)
                {
                    ct.SanPham.SoLuongTon += ct.SoLuong;
                }
            }

            order.TrangThaiDonHang = newStatus;
            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();
            
            VuongBanDienTu.Helpers.ActivityLogger.Log($"Cập nhật trạng thái đơn #ORD-{order.MaDonHang}", $"Từ '{oldStatus}' sang '{newStatus}'", newStatus);
            
            return Json(new { success = true, message = $"Đã cập nhật trạng thái đơn hàng sang '{newStatus}'" });
        }

        [HttpPost]
        public ActionResult Huy(int id, string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do hủy đơn để thông báo đến khách hàng!" });
            }

            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                bool daDanhToan = order.TrangThaiThanhToan?.Trim() == "Đã thanh toán";

                order.TrangThaiDonHang = "Đã hủy";
                order.GhiChu = lyDo.Trim();
                order.MaNhanVienXuLy = user.MaNguoiDung;

                // Nếu đã thanh toán → tự động đổi sang "Đã hoàn tiền" ngay
                if (daDanhToan)
                {
                    order.TrangThaiThanhToan = "Đã hoàn tiền";
                }

                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Hủy đơn hàng (Admin) #ORD-{order.MaDonHang}", $"Lý do: {order.GhiChu}", "Đã hủy");

                var orderId = order.MaDonHang;
                var cancellationReason = order.GhiChu;

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
                                if (daDanhToan)
                                {
                                    // Gửi email hoàn tiền kèm lý do hủy
                                    VuongBanDienTu.Services.EmailService.SendAdminCancelRefundEmail(orderToSend, cancellationReason);
                                }
                                else
                                {
                                    // Gửi email hủy thường
                                    VuongBanDienTu.Services.EmailService.SendOrderEmail(orderToSend, "HuyHang", cancellationReason);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Admin Huy Email Error: " + ex.Message);
                    }
                });

                string msg = daDanhToan
                    ? "Đã hủy đơn hàng và gửi email thông báo đã hoàn tiền cho khách hàng!"
                    : "Đã hủy đơn hàng và gửi email thông báo đến khách hàng!";

                return Json(new { success = true, message = msg });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        [HttpPost]
        public ActionResult DuyetHoanTien(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            // Removed restriction: if (order.TrangThaiDonHang?.Trim() != "Chờ hoàn tiền")

            order.TrangThaiDonHang = "Đã hủy";
            order.TrangThaiThanhToan = "Đã hoàn tiền";
            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();
            VuongBanDienTu.Helpers.ActivityLogger.Log($"Duyệt hoàn tiền đơn #ORD-{order.MaDonHang}", "Đã hoàn trả tiền cho khách hàng", "Đã hoàn tiền");

            var orderId = order.MaDonHang;
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
                            VuongBanDienTu.Services.EmailService.SendRefundApprovedEmail(orderToSend);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DuyetHoanTien Email Error: " + ex.Message);
                }
            });

            return Json(new { success = true, message = "Đã duyệt hoàn tiền và gửi email xác nhận đến khách hàng!" });
        }

        [HttpPost]
        public ActionResult GiaoHang(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                // Removed restriction: if (order.TrangThaiDonHang?.Trim() != "Đã xác nhận")

                order.TrangThaiDonHang = "Đang giao";
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Bắt đầu giao đơn #ORD-{order.MaDonHang}", $"Nhân viên xử lý: {user.HoTen}", "Đang giao");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        [HttpPost]
        public ActionResult HoanThanh(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                // Removed restriction: if (order.TrangThaiDonHang?.Trim() != "Đang giao")

                order.TrangThaiDonHang = "Hoàn thành";
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Hoàn thành đơn hàng #ORD-{order.MaDonHang}", $"Nhân viên xử lý: {user.HoTen}", "Hoàn thành");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }
    }
}
