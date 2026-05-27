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

        public ActionResult Index(int? id, string q, string sort, int? page)
        {
            ViewBag.Categories = db.DanhMucs.ToList();
            var query = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).Where(p => p.TrangThai != "Ngừng kinh doanh").AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(p => p.MaDanhMuc == id);
                ViewBag.CurrentCategory = db.DanhMucs.Find(id);
            }

            if (!string.IsNullOrEmpty(q))
            {
                string term = q.ToLower().Trim();
                query = query.Where(p => p.TenSanPham.ToLower().Contains(term));
                ViewBag.SearchQuery = q;
            }

            ViewBag.CurrentSort = sort;

            switch (sort)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.GiaBan);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.GiaBan);
                    break;
                case "best_seller":
                    query = from p in query
                            let totalSold = db.ChiTietDonHangs.Where(ct => ct.MaSanPham == p.MaSanPham).Sum(ct => (int?)ct.SoLuong) ?? 0
                            orderby totalSold descending
                            select p;
                    break;
                default:
                    query = query.OrderByDescending(p => p.NgayTao);
                    break;
            }

            int pageSize = 8;
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var products = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentId = id; // Store category ID for links

            return View(products);
        }

        [HttpGet]
        public ActionResult GoiYSanPham(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            string term = q.ToLower().Trim();
            var products = db.SanPhams
                .Include(p => p.HinhAnhSanPhams)
                .Where(p => p.TrangThai != "Ngừng kinh doanh" && p.TenSanPham.ToLower().Contains(term))
                .Take(8)
                .ToList();

            var result = products.Select(p => {
                var mainImg = p.HinhAnhSanPhams?.FirstOrDefault(h => h.AnhChinh);
                var imgUrl = mainImg != null ? VuongBanDienTu.Helpers.HinhAnh.GetImageUrl(mainImg.DuongDanAnh, true) : "/Content/Images/no-image.png";
                return new {
                    MaSanPham = p.MaSanPham,
                    TenSanPham = p.TenSanPham,
                    GiaBan = p.GiaBan > 0 ? p.GiaBan.ToString("N0") + "₫" : "Liên hệ",
                    HinhAnh = imgUrl
                };
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SanPhamNoiBat()
        {
            var products = db.SanPhams.Include(p => p.HinhAnhSanPhams).Where(p => p.TrangThai != "Ngừng kinh doanh").OrderByDescending(p => p.NgayTao).Take(4).ToList();
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
                .Where(p => bestSellerIds.Contains(p.MaSanPham) && p.TrangThai != "Ngừng kinh doanh")
                .ToList()
                .OrderBy(p => bestSellerIds.IndexOf(p.MaSanPham)) 
                .ToList();

            return PartialView("_SanPhamBanChayPartial", products);
        }

        public ActionResult ChiTiet(int id)
        {
            var sp = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).FirstOrDefault(p => p.MaSanPham == id && p.TrangThai != "Ngừng kinh doanh");
            if (sp == null) return HttpNotFound();
            
            ViewBag.RelatedProducts = db.SanPhams.Include(p => p.HinhAnhSanPhams).Where(p => p.MaDanhMuc == sp.MaDanhMuc && p.MaSanPham != id && p.TrangThai != "Ngừng kinh doanh").Take(4).ToList();
            
            return View(sp);
        }
    }
}
