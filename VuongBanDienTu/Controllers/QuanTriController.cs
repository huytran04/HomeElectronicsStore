using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Helpers;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
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
            return user != null && (user.MaVaiTro == 1 || user.MaVaiTro == 2 || user.MaVaiTro == 3);
        }

        public ActionResult TongQuan()
        {
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
    }
}
