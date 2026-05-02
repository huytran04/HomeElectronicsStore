using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace VuongBanDienTu.Helpers
{
    public static class HinhAnh
    {
        public static string GetImageUrl(string fileName, bool isMainImage = true)
        {
            if (string.IsNullOrEmpty(fileName)) return "";

            string cleanFileName = fileName;
            if (fileName.Contains(":\\") || fileName.Contains("/"))
            {
                cleanFileName = Path.GetFileName(fileName);
            }

            string folder = isMainImage ? "Products" : "Gallery";
            string virtualPath = $"~/Content/Images/{folder}/{cleanFileName}";
            
            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            return urlHelper.Content(virtualPath);
        }
    }
}
