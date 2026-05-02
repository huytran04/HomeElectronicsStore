using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using System.Data.Entity;

namespace VuongBanDienTu.Controllers
{
    public class SanPhamController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public ActionResult Index(int? id)
        {
            ViewBag.Categories = db.DanhMucs.ToList();
            var products = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).AsQueryable();

            if (id.HasValue)
            {
                products = products.Where(p => p.MaDanhMuc == id);
                ViewBag.CurrentCategory = db.DanhMucs.Find(id);
            }

            return View(products.OrderByDescending(p => p.NgayTao).Take(4).ToList());
        }

        public ActionResult SanPhamNoiBat()
        {
            var products = db.SanPhams.Include(p => p.HinhAnhSanPhams).OrderByDescending(p => p.NgayTao).Take(4).ToList();
            return PartialView("SanPhamNoiBat/Index", products);
        }

        public ActionResult SanPhamBanChay()
        {
            var bestSellerIds = db.ChiTietDonHangs
                .GroupBy(ct => ct.MaSanPham)
                .Select(g => new { MaSanPham = g.Key, TotalSold = g.Sum(ct => ct.SoLuong) })
                .OrderByDescending(x => x.TotalSold)
                .Take(4)
                .Select(x => x.MaSanPham)
                .ToList();

            var products = db.SanPhams
                .Include(p => p.HinhAnhSanPhams)
                .Where(p => bestSellerIds.Contains(p.MaSanPham))
                .ToList()
                .OrderBy(p => bestSellerIds.IndexOf(p.MaSanPham)) 
                .ToList();

            return PartialView("_SanPhamBanChayPartial", products);
        }

        public ActionResult ChiTiet(int id)
        {
            var sp = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).FirstOrDefault(p => p.MaSanPham == id);
            if (sp == null) return HttpNotFound();
            
            ViewBag.RelatedProducts = db.SanPhams.Include(p => p.HinhAnhSanPhams).Where(p => p.MaDanhMuc == sp.MaDanhMuc && p.MaSanPham != id).Take(4).ToList();
            
            return View(sp);
        }
    }
}
