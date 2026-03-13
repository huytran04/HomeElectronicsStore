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
    }
}