using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Helpers
{
    public static class PhanQuyen
    {
        public const int ADMIN = 1;
        public const int QUAN_LY = 2;
        public const int NHAN_VIEN = 3;
        public const int KHACH_HANG = 4;
        public const int THU_VIEC = 5;

        public static bool IsStaff(int? maVaiTro)
        {
            if (!maVaiTro.HasValue) return false;
            return maVaiTro == ADMIN || maVaiTro == QUAN_LY || maVaiTro == NHAN_VIEN || maVaiTro == THU_VIEC;
        }

        public static bool IsAdmin(int? maVaiTro)
        {
            return maVaiTro == ADMIN;
        }

        public static string GetRoleBadgeClass(int? maVaiTro)
        {
            switch (maVaiTro)
            {
                case ADMIN: return "bg-red-50 text-primary";
                case QUAN_LY: return "bg-amber-50 text-amber-600";
                case NHAN_VIEN: return "bg-blue-50 text-blue-600";
                default: return "bg-slate-100 text-slate-500";
            }
        }

        public static bool HasPermission(string permissionCode)
        {
            var user = HttpContext.Current.Session["TaiKhoan"] as NguoiDung;
            if (user == null) return false;
            
            // Nếu là quyền truy cập trang quản trị cơ bản, cho phép Admin, Quản lý và Nhân viên vào luôn
            if (permissionCode == "TRUY_CAP_QUAN_TRI")
            {
                return IsStaff(user.MaVaiTro);
            }

            if (user.MaVaiTro == ADMIN) return true;

            // 1. Kiểm tra trong Cache (Session) trước để đạt hiệu năng tối đa
            var userPermissions = HttpContext.Current.Session["UserPermissions"] as List<string>;
            if (userPermissions != null)
            {
                return userPermissions.Contains(permissionCode);
            }

            // 2. Nếu Cache trống (vừa cập nhật hoặc Session timeout), truy vấn DB và nạp lại Cache
            try
            {
                using (var db = new VuongDienTuEntities())
                {
                    // Lấy thông tin user lại từ session để đảm bảo không null và có MaVaiTro
                    var currentUser = HttpContext.Current.Session["TaiKhoan"] as NguoiDung;
                    if (currentUser == null || currentUser.MaVaiTro == null) return false;

                    var maVT = currentUser.MaVaiTro;

                    // Lấy quyền từ Vai trò (Hệ thống RBAC chuẩn 4 bảng)
                    var role = db.VaiTroes.Include("QuyenHans").FirstOrDefault(r => r.MaVaiTro == maVT);
                    var allPerms = role?.QuyenHans.Select(q => q.Code).Distinct().ToList() ?? new List<string>();

                    // Lưu vào Session Cache
                    HttpContext.Current.Session["UserPermissions"] = allPerms;
                    return allPerms.Contains(permissionCode);
                }
            }
            catch
            {
                // Fallback an toàn
                if (permissionCode == "TRUY_CAP_QUAN_TRI") return IsStaff(user.MaVaiTro);
                return false;
            }
        }

        // Method để ép nạp lại Cache khi có thay đổi quyền
        public static void RefreshPermissions()
        {
            HttpContext.Current.Session["UserPermissions"] = null;
        }

        public static string GetTimeAgo(DateTime? dt)
        {
            if (!dt.HasValue) return "---";
            var span = DateTime.Now - dt.Value;
            if (span.TotalSeconds < 10) return "Vừa xong";
            if (span.TotalMinutes < 1) return $"{(int)span.TotalSeconds} giây trước";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return dt.Value.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
