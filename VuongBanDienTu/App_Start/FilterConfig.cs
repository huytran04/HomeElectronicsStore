using System.Web;
using System.Web.Mvc;

namespace VuongBanDienTu
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new BaoTriFilter());
        }
    }

    public class BaoTriFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var config = VuongBanDienTu.Helpers.CauHinhHelper.LayCauHinh();
            if (config != null && config.MaintenanceMode)
            {
                string controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.ToLower();
                string actionName = filterContext.ActionDescriptor.ActionName.ToLower();

                var sessionUser = filterContext.HttpContext.Session["TaiKhoan"] as VuongBanDienTu.Models.NguoiDung;
                bool isAdmin = sessionUser != null && (sessionUser.MaVaiTro == 1 || sessionUser.MaVaiTro == 2 || sessionUser.MaVaiTro == 3);

                if (!isAdmin && 
                    !controllerName.Contains("quantri") && 
                    !controllerName.Contains("quanly") && 
                    !(controllerName == "home" && actionName == "baotri") && 
                    !(controllerName == "taikhoan" && (actionName == "dangnhap" || actionName == "dangxuat" || actionName == "nhapmaotp" || actionName == "kichhoattaikhoan")))
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary(
                            new { controller = "Home", action = "BaoTri" }
                        )
                    );
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
