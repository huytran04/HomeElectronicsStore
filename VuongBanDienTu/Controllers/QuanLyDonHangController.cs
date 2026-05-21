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
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do hủy đơn để thông báo đến khách hàng!" });
            }

            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                order.TrangThaiDonHang = "Đã hủy";
                order.GhiChu = lyDo.Trim();
                order.MaNhanVienXuLy = user.MaNguoiDung;
                db.SaveChanges();

                var orderId = order.MaDonHang;
                var cancellationReason = order.GhiChu;

                // Gửi email thông báo hủy đơn kèm lý do đến khách hàng
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
                                VuongBanDienTu.Services.EmailService.SendOrderEmail(orderToSend, "HuyHang", cancellationReason);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Admin Huy Email Error: " + ex.Message);
                    }
                });

                return Json(new { success = true, message = "Đã hủy đơn hàng và gửi email thông báo đến khách hàng!" });
            }
            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }

        [HttpPost]
        public ActionResult DuyetHoanTien(int id)
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            var order = db.DonHangs.Find(id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.TrangThaiDonHang?.Trim() != "Chờ hoàn tiền")
                return Json(new { success = false, message = "Đơn hàng này không ở trạng thái chờ hoàn tiền!" });

            order.TrangThaiDonHang = "Đã hủy";
            order.TrangThaiThanhToan = "Chờ hoàn tiền";
            order.MaNhanVienXuLy = user.MaNguoiDung;
            db.SaveChanges();

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
    }
}
