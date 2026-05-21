using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class HomeController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public ActionResult Index()
        {
            ViewBag.Categories = db.DanhMucs.ToList();

            var newestProduct = db.SanPhams
                .Include("HinhAnhSanPhams")
                .Include("DanhMuc")
                .Where(p => p.TrangThai != "Ngừng bán")
                .OrderByDescending(p => p.NgayTao)
                .FirstOrDefault();
            ViewBag.NewestProduct = newestProduct;

            int? topSellerId = db.ChiTietDonHangs
                .GroupBy(ct => ct.MaSanPham)
                .Select(g => new { MaSanPham = g.Key, TotalSold = g.Sum(ct => ct.SoLuong) })
                .OrderByDescending(x => x.TotalSold)
                .Select(x => x.MaSanPham)
                .FirstOrDefault();

            var bestSeller = db.SanPhams
                .Include("HinhAnhSanPhams")
                .Include("DanhMuc")
                .FirstOrDefault(p => p.MaSanPham == topSellerId && p.TrangThai != "Ngừng bán");

            if (bestSeller == null)
            {
                bestSeller = db.SanPhams
                    .Include("HinhAnhSanPhams")
                    .Include("DanhMuc")
                    .Where(p => p.TrangThai != "Ngừng bán")
                    .OrderByDescending(p => p.NgayTao)
                    .Skip(1)
                    .FirstOrDefault();
            }
            ViewBag.BestSellerProduct = bestSeller;

            var featuredProduct = db.SanPhams
                .Include("HinhAnhSanPhams")
                .Include("DanhMuc")
                .Where(p => p.TrangThai != "Ngừng bán")
                .OrderByDescending(p => p.GiaBan)
                .FirstOrDefault();

            if (featuredProduct == null || (newestProduct != null && featuredProduct.MaSanPham == newestProduct.MaSanPham))
            {
                featuredProduct = db.SanPhams
                    .Include("HinhAnhSanPhams")
                    .Include("DanhMuc")
                    .Where(p => p.TrangThai != "Ngừng bán")
                    .OrderByDescending(p => p.NgayTao)
                    .Skip(2)
                    .FirstOrDefault();
            }
            ViewBag.FeaturedProduct = featuredProduct;

            var products = db.SanPhams.Include("DanhMuc").OrderByDescending(p => p.NgayTao).ToList();
            return View(products);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult CamNang() => View();
        public ActionResult GiaoLap() => View();
        public ActionResult KhuyenMai() => View();
        public ActionResult DoanhNghiep() => View();

        public ActionResult BaoTri()
        {
            var config = VuongBanDienTu.Helpers.CauHinhHelper.LayCauHinh();
            if (config == null || !config.MaintenanceMode)
            {
                return RedirectToAction("Index");
            }
            return View(config);
        }
    }
}