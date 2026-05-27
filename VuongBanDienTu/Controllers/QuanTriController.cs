using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Helpers;
using VuongBanDienTu.Models;
using System.Data.Entity;

namespace VuongBanDienTu.Controllers
{
    public class DoanhThuModel
    {
        public string TimeLabel { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }

        public string Label { get { return TimeLabel; } set { TimeLabel = value; } }
        public int Count { get { return OrderCount; } set { OrderCount = value; } }
    }

    public class QuanTriController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsAdmin()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && user.MaVaiTro == 1;
        }

        private bool IsInternal()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && PhanQuyen.IsStaff(user.MaVaiTro);
        }

        private bool IsManagerOrAdmin()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY);
        }

        private ActionResult RedirectToDashboard()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            if (user != null && (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY))
            {
                return RedirectToAction("TongQuan");
            }
            return RedirectToAction("Index", "QuanLySanPham");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsInternal())
            {
                filterContext.Result = RedirectToAction("DangNhap", "TaiKhoan");
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult TongQuan()
        {
            if (!IsManagerOrAdmin())
            {
                return RedirectToAction("Index", "QuanLySanPham");
            }

            ViewBag.TotalCustomers = db.NguoiDungs.Count(u => u.MaVaiTro == 4);

            decimal totalRevenue = db.DonHangs.Where(o => o.TrangThaiDonHang == "Đã xác nhận").Sum(o => (decimal?)o.TongTien) ?? 0;
            ViewBag.TotalRevenue = totalRevenue;

            int totalOrders = db.DonHangs.Count(o => o.TrangThaiDonHang != "Chờ thanh toán");
            ViewBag.TotalOrders = totalOrders;

            int totalProducts = db.SanPhams.Count(p => p.TrangThai != "Ngừng kinh doanh");
            ViewBag.TotalProducts = totalProducts;
            try
            {
                var drive = new System.IO.DriveInfo(HttpContext.Server.MapPath("~"));
                double freeSpacePct = (double)drive.AvailableFreeSpace / drive.TotalSize;
                ViewBag.DiskUsage = (int)Math.Round((1.0 - freeSpacePct) * 100);
            }
            catch
            {
                ViewBag.DiskUsage = 68;
            }

            DateTime now = DateTime.Now;
            int year = now.Year;
            
            DateTime startDate = new DateTime(year, 1, 1);
            DateTime endDate = new DateTime(year, 12, 31, 23, 59, 59);

            var currentYearOrders = db.DonHangs
                .Where(o => o.NgayDat.HasValue && o.NgayDat >= startDate && o.NgayDat <= endDate && o.TrangThaiDonHang != "Chờ thanh toán")
                .ToList();

            int[] monthlyOrders = new int[12];
            decimal[] monthlyRevenue = new decimal[12];

            for (int m = 1; m <= 12; m++)
            {
                var monthOrders = currentYearOrders.Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Month == m).ToList();
                monthlyOrders[m - 1] = monthOrders.Count;
                monthlyRevenue[m - 1] = monthOrders.Where(o => o.TrangThaiDonHang == "Đã xác nhận").Sum(o => (decimal?)o.TongTien) ?? 0;
            }

            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.MonthlyRevenue = monthlyRevenue;

            // Ensure table is created
            ActivityLogger.CheckAndCreateTable();
            var recentLogs = db.Database.SqlQuery<Models.LichSuHoatDong>("SELECT TOP 5 * FROM LichSuHoatDong ORDER BY ThoiGian DESC").ToList();
            ViewBag.RecentLogs = recentLogs;

            return View();
        }

        public ActionResult NhanVien()
        {
            return View();
        }

        public ActionResult QuanLy()
        {
            return View();
        }
        
        public ActionResult NguoiDung()
        {
            return RedirectToAction("Index", "QuanLyNguoiDung");
        }

        public ActionResult DanhMuc()
        {
            return RedirectToAction("Index", "QuanLyDanhMuc");
        }


        public ActionResult DonHang()
        {
            return View();
        }

        public ActionResult BaoCaoDoanhThu(string tuNgay, string denNgay)
        {
            if (!IsManagerOrAdmin())
            {
                TempData["Error"] = "Bạn không có quyền truy cập báo cáo doanh thu!";
                return RedirectToDashboard();
            }

            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(tuNgay))
            {
                if (DateTime.TryParseExact(tuNgay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedStart))
                {
                    start = parsedStart;
                }
                else if (DateTime.TryParse(tuNgay, out DateTime parsedStart2))
                {
                    start = parsedStart2;
                }
            }

            if (!string.IsNullOrEmpty(denNgay))
            {
                if (DateTime.TryParseExact(denNgay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedEnd))
                {
                    end = parsedEnd;
                }
                else if (DateTime.TryParse(denNgay, out DateTime parsedEnd2))
                {
                    end = parsedEnd2;
                }
            }

            var query = db.DonHangs.Where(o => o.NgayDat.HasValue && o.TrangThaiDonHang.Contains("Đã xác nhận"));

            if (start.HasValue)
            {
                query = query.Where(o => o.NgayDat >= start.Value);
            }
            if (end.HasValue)
            {
                var endDay = end.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(o => o.NgayDat <= endDay);
            }

            var orders = query.ToList();

            var dailyData = orders
                .GroupBy(o => o.NgayDat.Value.Date)
                .Select(g => new DoanhThuModel
                {
                    TimeLabel = g.Key.ToString("dd/MM/yyyy"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TongTien ?? 0)
                })
                .OrderByDescending(o => DateTime.ParseExact(o.TimeLabel, "dd/MM/yyyy", null))
                .ToList();

            var monthlyData = orders
                .GroupBy(o => new { o.NgayDat.Value.Year, o.NgayDat.Value.Month })
                .Select(g => new DoanhThuModel
                {
                    TimeLabel = $"Tháng {g.Key.Month}/{g.Key.Year}",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TongTien ?? 0)
                })
                .OrderByDescending(o => int.Parse(o.TimeLabel.Split('/')[1]))
                .ThenByDescending(o => int.Parse(o.TimeLabel.Split('/')[0].Replace("Tháng ", "")))
                .ToList();

            var yearlyData = orders
                .GroupBy(o => o.NgayDat.Value.Year)
                .Select(g => new DoanhThuModel
                {
                    TimeLabel = $"Năm {g.Key}",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TongTien ?? 0)
                })
                .OrderByDescending(o => int.Parse(o.TimeLabel.Replace("Năm ", "")))
                .ToList();

            ViewBag.DailyData = dailyData;
            ViewBag.MonthlyData = monthlyData;
            ViewBag.YearlyData = yearlyData;
            ViewBag.TuNgay = start;
            ViewBag.DenNgay = end;

            return View();
        }

        [HttpGet]
        public ActionResult XuatExcelDoanhThu(string type, string tuNgay, string denNgay)
        {
            if (!IsManagerOrAdmin())
            {
                TempData["Error"] = "Bạn không có quyền xuất dữ liệu báo cáo doanh thu!";
                return RedirectToDashboard();
            }

            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(tuNgay))
            {
                if (DateTime.TryParseExact(tuNgay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedStart))
                {
                    start = parsedStart;
                }
                else if (DateTime.TryParse(tuNgay, out DateTime parsedStart2))
                {
                    start = parsedStart2;
                }
            }

            if (!string.IsNullOrEmpty(denNgay))
            {
                if (DateTime.TryParseExact(denNgay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedEnd))
                {
                    end = parsedEnd;
                }
                else if (DateTime.TryParse(denNgay, out DateTime parsedEnd2))
                {
                    end = parsedEnd2;
                }
            }

            var query = db.DonHangs.Include("NguoiDung").Where(o => o.NgayDat.HasValue && o.TrangThaiDonHang.Contains("Đã xác nhận"));

            if (start.HasValue)
            {
                query = query.Where(o => o.NgayDat >= start.Value);
            }
            if (end.HasValue)
            {
                var endDay = end.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(o => o.NgayDat <= endDay);
            }

            var orders = query.ToList();
            string filename = $"BaoCaoDoanhThu_{type}_{DateTime.Now:yyyyMMdd}";
            string timeColumnHeader = "Thời gian";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
            sb.Append("<head>");
            sb.Append("<meta http-equiv=Content-Type content='text/html; charset=utf-8'>");
            sb.Append("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>Bao Cao</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
            sb.Append("<style>");
            sb.Append("table { border-collapse: collapse; font-family: Arial, sans-serif; }");
            sb.Append("th { background-color: #e8192c; color: white; font-weight: bold; border: 1px solid #cccccc; padding: 8px; text-align: left; }");
            sb.Append("td { border: 1px solid #cccccc; padding: 8px; text-align: left; }");
            sb.Append(".header-title { font-size: 16pt; font-weight: bold; color: #333333; text-align: center; }");
            sb.Append(".info { font-size: 10pt; color: #666666; }");
            sb.Append(".total { font-weight: bold; background-color: #f9f9f9; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");

            sb.Append("<table>");
            sb.Append($"<tr><td colspan='3' class='header-title'>BÁO CÁO DOANH THU THEO {type.ToUpper()}</td></tr>");
            string dateRangeStr = (start.HasValue || end.HasValue) 
                ? $"Từ ngày: {(start.HasValue ? start.Value.ToString("dd/MM/yyyy") : "---")} - Đến ngày: {(end.HasValue ? end.Value.ToString("dd/MM/yyyy") : "---")}" 
                : "Tất cả thời gian";
            sb.Append($"<tr><td colspan='3' class='info'>{dateRangeStr}</td></tr>");
            sb.Append($"<tr><td colspan='3' class='info'>Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append("<tr><td colspan='3'></td></tr>");

            if (type == "ngay") timeColumnHeader = "Ngày";
            else if (type == "thang") timeColumnHeader = "Tháng";
            else if (type == "nam") timeColumnHeader = "Năm";

            sb.Append("<tr>");
            sb.Append($"<th>{timeColumnHeader}</th>");
            sb.Append("<th>Số đơn hàng thành công</th>");
            sb.Append("<th>Doanh thu (VNĐ)</th>");
            sb.Append("</tr>");

            decimal grandTotal = 0;
            int totalOrdersCount = 0;

            if (type == "ngay")
            {
                var daily = orders
                    .GroupBy(o => o.NgayDat.Value.Date)
                    .Select(g => new DoanhThuModel { Label = g.Key.ToString("dd/MM/yyyy"), Count = g.Count(), Revenue = g.Sum(o => o.TongTien ?? 0) })
                    .OrderByDescending(g => DateTime.ParseExact(g.Label, "dd/MM/yyyy", null))
                    .ToList();

                foreach (var item in daily)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{item.Label}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\";'>{item.Count}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\";'>{item.Revenue}</td>");
                    sb.Append("</tr>");
                    grandTotal += item.Revenue;
                    totalOrdersCount += item.Count;
                }
            }
            else if (type == "thang")
            {
                var monthly = orders
                    .GroupBy(o => new { o.NgayDat.Value.Year, o.NgayDat.Value.Month })
                    .Select(g => new DoanhThuModel { Label = $"Tháng {g.Key.Month}/{g.Key.Year}", Count = g.Count(), Revenue = g.Sum(o => o.TongTien ?? 0) })
                    .OrderByDescending(g => int.Parse(g.Label.Split('/')[1]))
                    .ThenByDescending(g => int.Parse(g.Label.Split('/')[0].Replace("Tháng ", "")))
                    .ToList();

                foreach (var item in monthly)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{item.Label}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\";'>{item.Count}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\";'>{item.Revenue}</td>");
                    sb.Append("</tr>");
                    grandTotal += item.Revenue;
                    totalOrdersCount += item.Count;
                }
            }
            else if (type == "nam")
            {
                var yearly = orders
                    .GroupBy(o => o.NgayDat.Value.Year)
                    .Select(g => new DoanhThuModel { Label = $"Năm {g.Key}", Count = g.Count(), Revenue = g.Sum(o => o.TongTien ?? 0) })
                    .OrderByDescending(g => int.Parse(g.Label.Replace("Năm ", "")))
                    .ToList();

                foreach (var item in yearly)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{item.Label}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\";'>{item.Count}</td>");
                    sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\";'>{item.Revenue}</td>");
                    sb.Append("</tr>");
                    grandTotal += item.Revenue;
                    totalOrdersCount += item.Count;
                }
            }

            sb.Append("<tr class='total'>");
            sb.Append("<td>TỔNG CỘNG</td>");
            sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\";'>{totalOrdersCount}</td>");
            sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\";'>{grandTotal}</td>");
            sb.Append("</tr>");

            sb.Append("</table>");

            sb.Append("<br/><br/>");
            sb.Append("<table>");
            sb.Append("<tr><td colspan='10' class='header-title' style='background-color: #333333; color: white;'>DANH SÁCH CHI TIẾT ĐƠN HÀNG & THÔNG TIN KHÁCH HÀNG</td></tr>");
            sb.Append("<tr>");
            sb.Append("<th>STT</th>");
            sb.Append("<th>Mã đơn hàng</th>");
            sb.Append("<th>Họ tên khách hàng</th>");
            sb.Append("<th>Số điện thoại</th>");
            sb.Append("<th>Email</th>");
            sb.Append("<th>Địa chỉ giao hàng</th>");
            sb.Append("<th>Ngày đặt</th>");
            sb.Append("<th>Phương thức thanh toán</th>");
            sb.Append("<th>Ghi chú</th>");
            sb.Append("<th>Tổng tiền (VNĐ)</th>");
            sb.Append("</tr>");

            int index = 1;
            decimal totalRevenue = 0;
            foreach (var o in orders.OrderByDescending(x => x.NgayDat))
            {
                string customerName = o.NguoiDung != null ? o.NguoiDung.HoTen : "Khách vãng lai";
                string phone = o.NguoiDung != null ? o.NguoiDung.SoDienThoai : "";
                string email = o.NguoiDung != null ? o.NguoiDung.Email : "";
                string dateStr = o.NgayDat.HasValue ? o.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "";
                
                sb.Append("<tr>");
                sb.Append($"<td>{index++}</td>");
                sb.Append($"<td>DH{o.MaDonHang}</td>");
                sb.Append($"<td>{customerName}</td>");
                sb.Append($"<td style='mso-number-format:\"\\@\";'>{phone}</td>"); // Keep leading zero for phone numbers
                sb.Append($"<td>{email}</td>");
                sb.Append($"<td>{o.DiaChiGiaoHang}</td>");
                sb.Append($"<td>{dateStr}</td>");
                sb.Append($"<td>{o.PhuongThucThanhToan}</td>");
                sb.Append($"<td>{o.GhiChu}</td>");
                sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\";'>{o.TongTien ?? 0}</td>");
                sb.Append("</tr>");
                totalRevenue += o.TongTien ?? 0;
            }

            sb.Append("<tr class='total'>");
            sb.Append("<td colspan='9' style='text-align: right; font-weight: bold;'>TỔNG CỘNG DỒN</td>");
            sb.Append($"<td style='mso-number-format:\"\\#\\,\\#\\#0\\ \\\"₫\\\"\"; font-weight: bold;'>{totalRevenue}</td>");
            sb.Append("</tr>");
            sb.Append("</table>");

            sb.Append("</body>");
            sb.Append("</html>");

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", $"attachment;filename={filename}.xls");
            Response.Charset = "utf-8";
            Response.ContentType = "application/vnd.ms-excel";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Write(sb.ToString());
            Response.End();

            return null;
        }

        [HttpGet]
        public ActionResult CaiDat()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bạn không có quyền truy cập cài đặt hệ thống!";
                return RedirectToDashboard();
            }
            var config = VuongBanDienTu.Helpers.CauHinhHelper.LayCauHinh();
            return View(config);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CaiDat(VuongBanDienTu.Helpers.CauHinhModel config)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bạn không có quyền thay đổi cài đặt hệ thống!";
                return RedirectToDashboard();
            }
            if (ModelState.IsValid)
            {
                if (VuongBanDienTu.Helpers.CauHinhHelper.LuuCauHinh(config))
                {
                    ActivityLogger.Log("Cấu hình hệ thống", "Cập nhật cài đặt cấu hình cửa hàng", "Thành công");
                    TempData["Success"] = "Cập nhật cấu hình hệ thống thành công!";
                    return RedirectToAction("CaiDat");
                }
                else
                {
                    ModelState.AddModelError("", "Không thể lưu cấu hình. Vui lòng kiểm tra lại quyền ghi file.");
                }
            }
            return View(config);
        }
        public ActionResult LichSuHoatDong()
        {
            if (!IsManagerOrAdmin())
            {
                return RedirectToAction("Index", "QuanLySanPham");
            }
            ActivityLogger.CheckAndCreateTable();
            var logs = db.Database.SqlQuery<Models.LichSuHoatDong>("SELECT * FROM LichSuHoatDong ORDER BY ThoiGian DESC").ToList();
            return View(logs);
        }
    }
}
