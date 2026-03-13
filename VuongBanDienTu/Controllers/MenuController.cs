using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;

namespace VuongBanDienTu.Controllers
{
    public class MenuController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        [ChildActionOnly]
        public ActionResult CategoryMenu()
        {
            var categories = db.DanhMucs.ToList();
            return PartialView("_CategoryMenu", categories);
        }
    }
}
