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
            // 2. Nếu chuyển TỪ nhóm xác nhận SANG nhóm hủy/chờ/trả hàng -> Hoàn tồn kho
            else if (wasConfirmed && !isNewConfirmed)
            {
                foreach (var ct in order.ChiTietDonHangs)
                {
                    ct.SanPham.SoLuongTon += ct.SoLuong;
                }
            }
            // 3. Nếu từ "Yêu cầu trả hàng" sang "Đã trả hàng" (Trường hợp Admin duyệt trực tiếp qua UpdateStatus)
            else if (oldStatus == "Yêu cầu trả hàng" && newStatus == "Đã trả hàng")
            {
                foreach (var ct in order.ChiTietDonHangs)
                {
                    ct.SanPham.SoLuongTon += ct.SoLuong;
                }
                order.TrangThaiThanhToan = "Đã hoàn tiền";
            }

            order.TrangThaiDonHang = newStatus;
            
            // Tự động chuyển sang "Đã thanh toán" cho đơn hàng COD khi hoàn thành
            if (newStatus == "Hoàn thành" && order.PhuongThucThanhToan?.Trim() == "COD")
            {
                order.TrangThaiThanhToan = "Đã thanh toán";
            }

            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();
            
            VuongBanDienTu.Helpers.ActivityLogger.Log($"Cập nhật trạng thái đơn #ORD-{order.MaDonHang}", $"Từ '{oldStatus}' sang '{newStatus}'", newStatus);
            
            return Json(new { success = true, message = $"Đã cập nhật trạng thái đơn hàng sang '{newStatus}'" });
        }

        [HttpPost]
        public ActionResult DuyetTraHang(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham").FirstOrDefault(o => o.MaDonHang == id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            // Hoàn tồn kho khi khách trả hàng
            foreach (var ct in order.ChiTietDonHangs)
            {
                ct.SanPham.SoLuongTon += ct.SoLuong;
            }

            order.TrangThaiDonHang = "Đã trả hàng";
            order.TrangThaiThanhToan = "Đã hoàn tiền";
            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();

            VuongBanDienTu.Helpers.ActivityLogger.Log($"Duyệt trả hàng đơn #ORD-{order.MaDonHang}", "Đã nhận lại hàng và hoàn tiền cho khách", "Đã trả hàng");

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
                            VuongBanDienTu.Services.EmailService.SendReturnApprovedEmail(orderToSend);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DuyetTraHang Email Error: " + ex.Message);
                }
            });

            return Json(new { success = true, message = "Đã duyệt trả hàng, hoàn tồn kho và gửi email xác nhận hoàn tiền cho khách!" });
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
            var order = db.DonHangs.Include("ChiTietDonHangs").Include("ChiTietDonHangs.SanPham").FirstOrDefault(o => o.MaDonHang == id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            string currentStatus = order.TrangThaiDonHang?.Trim();
            bool isReturnRequest = currentStatus == "Yêu cầu trả hàng";
            bool isCancelRequest = currentStatus == "Chờ duyệt hủy";
            bool isRefundRequest = currentStatus == "Chờ hoàn tiền";
            bool daThanhToan = order.TrangThaiThanhToan?.Trim() == "Đã thanh toán" || isRefundRequest;

            // Kiểm tra xem đơn hàng đã bị trừ tồn kho chưa (Đã xác nhận, Đang giao)
            string[] deductedInventoryGroup = { "Đã xác nhận", "Đang giao", "Yêu cầu trả hàng" };
            bool shouldReturnInventory = deductedInventoryGroup.Contains(currentStatus);

            if (shouldReturnInventory)
            {
                foreach (var ct in order.ChiTietDonHangs)
                {
                    ct.SanPham.SoLuongTon += ct.SoLuong;
                }
            }

            if (isReturnRequest)
            {
                order.TrangThaiDonHang = "Đã trả hàng";
                order.TrangThaiThanhToan = "Đã hoàn tiền";
            }
            else
            {
                order.TrangThaiDonHang = "Đã hủy";
                if (daThanhToan)
                {
                    order.TrangThaiThanhToan = "Đã hoàn tiền";
                }
            }

            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();

            string logAction = isReturnRequest ? "Duyệt trả hàng & hoàn tiền" : (isCancelRequest ? "Duyệt hủy đơn COD" : "Duyệt hoàn tiền đơn hủy");
            VuongBanDienTu.Helpers.ActivityLogger.Log($"{logAction} #ORD-{order.MaDonHang}", "Đã xử lý yêu cầu của khách hàng", order.TrangThaiDonHang);

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
                        {
                            if (isReturnRequest)
                                VuongBanDienTu.Services.EmailService.SendReturnApprovedEmail(orderToSend);
                            else if (daThanhToan)
                                VuongBanDienTu.Services.EmailService.SendRefundApprovedEmail(orderToSend);
                            else
                                VuongBanDienTu.Services.EmailService.SendSelfCancelEmail(orderToSend); // Gửi email thông báo hủy đơn thường
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DuyetHoanTien Email Error: " + ex.Message);
                }
            });

            return Json(new { success = true, message = "Đã xử lý yêu cầu duyệt hủy/hoàn tiền và gửi email cho khách hàng!" });
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
                
                // Tự động chuyển sang "Đã thanh toán" cho các đơn hàng COD khi hoàn thành
                if (order.PhuongThucThanhToan?.Trim() == "COD" && order.TrangThaiThanhToan?.Trim() != "Đã thanh toán")
                {
                    order.TrangThaiThanhToan = "Đã thanh toán";
                }

                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log($"Hoàn thành đơn hàng #ORD-{order.MaDonHang}", $"Nhân viên xử lý: {user.HoTen}. Tự động xác nhận thanh toán.", "Hoàn thành");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }
    }
}
