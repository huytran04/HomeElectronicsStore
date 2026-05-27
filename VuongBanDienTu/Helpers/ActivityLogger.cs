using System;
using System.Linq;
using System.Web;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Helpers
{
    public static class ActivityLogger
    {
        private static readonly object _lock = new object();
        private static bool _tableChecked = false;

        public static void CheckAndCreateTable()
        {
            if (_tableChecked) return;

            lock (_lock)
            {
                if (_tableChecked) return;

                try
                {
                    using (var db = new VuongDienTuEntities())
                    {
                        // Check if table exists using raw SQL
                        var checkQuery = "SELECT object_id('LichSuHoatDong')";
                        var tableId = db.Database.SqlQuery<int?>(checkQuery).FirstOrDefault();
                        
                        if (tableId == null)
                        {
                            // Create table
                            var createQuery = @"
                                CREATE TABLE LichSuHoatDong (
                                    MaLog INT IDENTITY(1,1) PRIMARY KEY,
                                    TenDangNhap NVARCHAR(50) NULL,
                                    HoTen NVARCHAR(100) NULL,
                                    VaiTro NVARCHAR(50) NULL,
                                    HanhDong NVARCHAR(255) NULL,
                                    ChiTiet NTEXT NULL,
                                    ThoiGian DATETIME DEFAULT GETDATE(),
                                    TrangThai NVARCHAR(50) NULL
                                )";
                            db.Database.ExecuteSqlCommand(createQuery);

                            // Seed data from existing orders to populate history immediately
                            var firstAdmin = db.NguoiDungs.FirstOrDefault(u => u.MaVaiTro == 1);
                            string defaultAdminUser = firstAdmin?.TenDangNhap ?? "admin";
                            string defaultAdminName = firstAdmin?.HoTen ?? "Quản trị viên";

                            var orders = db.DonHangs
                                .Include("NguoiDung")
                                .Include("NguoiDung1")
                                .Where(o => o.TrangThaiDonHang != "Chờ thanh toán")
                                .OrderBy(o => o.NgayDat)
                                .ToList();

                            foreach (var o in orders)
                            {
                                string user = o.NguoiDung?.TenDangNhap ?? "Guest";
                                string fullname = o.NguoiDung?.HoTen ?? "Khách hàng";
                                string status = o.TrangThaiDonHang?.Trim() ?? "Chờ xử lý";
                                string action = $"Đặt đơn hàng mới #ORD-{o.MaDonHang}";
                                string detail = o.GhiChu;

                                // Add log for order placement
                                var sql = "INSERT INTO LichSuHoatDong (TenDangNhap, HoTen, VaiTro, HanhDong, ChiTiet, ThoiGian, TrangThai) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)";
                                db.Database.ExecuteSqlCommand(sql, user, fullname, "Khách hàng", action, detail, o.NgayDat ?? DateTime.Now, "Chờ xử lý");

                                // Add log for subsequent state if order is processed
                                if (status != "Chờ xử lý")
                                {
                                    string staffUser = o.NguoiDung1?.TenDangNhap ?? defaultAdminUser;
                                    string staffFullname = o.NguoiDung1?.HoTen ?? defaultAdminName;
                                    int roleId = o.NguoiDung1?.MaVaiTro ?? 1;
                                    string staffRole = roleId == 1 ? "Admin" : (roleId == 2 ? "Quản lý" : (roleId == 3 ? "Nhân viên" : (roleId == 5 ? "Thử việc" : "Khách hàng")));
                                    
                                    string subAction = "";
                                    if (status == "Đã xác nhận") subAction = $"Duyệt & xác nhận đơn #ORD-{o.MaDonHang}";
                                    else if (status == "Đang giao") subAction = $"Bắt đầu giao đơn #ORD-{o.MaDonHang}";
                                    else if (status == "Hoàn thành") subAction = $"Hoàn thành đơn hàng #ORD-{o.MaDonHang}";
                                    else if (status == "Đã hủy") subAction = $"Hủy đơn hàng #ORD-{o.MaDonHang}";
                                    else if (status == "Chờ hoàn tiền") subAction = $"Khách yêu cầu hủy đơn #ORD-{o.MaDonHang}";

                                    db.Database.ExecuteSqlCommand(sql, staffUser, staffFullname, staffRole, subAction, detail, (o.NgayDat ?? DateTime.Now).AddMinutes(15), status);
                                }
                            }
                        }
                    }
                    _tableChecked = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ActivityLogger Init Error: " + ex.Message);
                }
            }
        }

        public static void Log(string hanhDong, string chiTiet = null, string trangThai = null)
        {
            try
            {
                CheckAndCreateTable();
                
                var user = HttpContext.Current?.Session["TaiKhoan"] as NguoiDung;
                string username = user?.TenDangNhap ?? "Guest";
                string fullname = user?.HoTen ?? "Khách hàng";
                string role = "Khách hàng";
                
                if (user != null)
                {
                    role = user.MaVaiTro == 1 ? "Admin" : (user.MaVaiTro == 2 ? "Quản lý" : (user.MaVaiTro == 3 ? "Nhân viên" : (user.MaVaiTro == 5 ? "Thử việc" : "Khách hàng")));
                }

                using (var db = new VuongDienTuEntities())
                {
                    var sql = "INSERT INTO LichSuHoatDong (TenDangNhap, HoTen, VaiTro, HanhDong, ChiTiet, ThoiGian, TrangThai) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)";
                    db.Database.ExecuteSqlCommand(sql, username, fullname, role, hanhDong, chiTiet, DateTime.Now, trangThai);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ActivityLogger Write Error: " + ex.Message);
            }
        }
    }
}
