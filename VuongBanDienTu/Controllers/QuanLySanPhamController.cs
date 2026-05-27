using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using System.IO;
using System.Data.Entity;
using VuongBanDienTu.Helpers;
using System.Data;

namespace VuongBanDienTu.Controllers
{
    public class QuanLySanPhamController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        private bool IsInternal()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && PhanQuyen.IsStaff(user.MaVaiTro);
        }

        private bool IsAuthorized()
        {
            var user = Session["TaiKhoan"] as NguoiDung;
            return user != null && (user.MaVaiTro == PhanQuyen.ADMIN || user.MaVaiTro == PhanQuyen.QUAN_LY);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsInternal())
            {
                if (Request.IsAjaxRequest())
                {
                    filterContext.Result = Json(new { success = false, message = "Hết phiên đăng nhập hoặc không có quyền!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    filterContext.Result = RedirectToAction("DangNhap", "TaiKhoan");
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index(int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            var query = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).OrderByDescending(p => p.NgayTao);
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var products = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        public ActionResult ThemSanPham()
        {
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc");
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ThemSanPhamPartial");
            }
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ThemSanPham(SanPham sp, HttpPostedFileBase AnhChinhFile, IEnumerable<HttpPostedFileBase> AnhPhuFiles)
        {
            if (sp.TrangThai == "Hết hàng")
            {
                sp.SoLuongTon = 0;
            }

            if (sp.GiaBan < 0)
            {
                ModelState.AddModelError("GiaBan", "Giá bán không được phép âm!");
            }
            if (!sp.SoLuongTon.HasValue)
            {
                ModelState.AddModelError("SoLuongTon", "Số lượng tồn không được để trống!");
            }
            else if (sp.SoLuongTon < 0)
            {
                ModelState.AddModelError("SoLuongTon", "Số lượng tồn không được phép âm!");
            }

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ!";
                    return Json(new { success = false, message = firstError });
                }
            }

            if (ModelState.IsValid)
            {
                string folderPath = Server.MapPath("~/Content/Images/Products/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (AnhChinhFile != null && AnhChinhFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AnhChinhFile.FileName);
                    string path = Path.Combine(folderPath, fileName);
                    AnhChinhFile.SaveAs(path);
                    
                    db.HinhAnhSanPhams.Add(new HinhAnhSanPham
                    {
                        MaSanPham = sp.MaSanPham,
                        DuongDanAnh = fileName,
                        AnhChinh = true
                    });
                }

                sp.NgayTao = DateTime.Now;
                db.SanPhams.Add(sp);
                db.SaveChanges(); 
                VuongBanDienTu.Helpers.ActivityLogger.Log("Thêm sản phẩm", $"Thêm sản phẩm '{sp.TenSanPham}', Giá bán: {sp.GiaBan:N0}₫", "Thành công");

                if (AnhPhuFiles != null && AnhPhuFiles.Any())
                {
                    string galleryPath = Server.MapPath("~/Content/Images/Gallery/");
                    if (!Directory.Exists(galleryPath))
                    {
                        Directory.CreateDirectory(galleryPath);
                    }

                    var filesToSave = AnhPhuFiles.Where(f => f != null && f.ContentLength > 0).Take(9);

                    foreach (var file in filesToSave)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string path = Path.Combine(galleryPath, fileName);
                        file.SaveAs(path);

                        db.HinhAnhSanPhams.Add(new HinhAnhSanPham
                        {
                            MaSanPham = sp.MaSanPham,
                            DuongDanAnh = fileName
                        });
                    }
                    db.SaveChanges();
                }
                
                if (Request.IsAjaxRequest())
                    return Json(new { success = true });

                return RedirectToAction("Index");
            }
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", sp.MaDanhMuc);
            
            if (Request.IsAjaxRequest())
                return PartialView("_ThemSanPhamPartial", sp);

            return View(sp);
        }

        public ActionResult SuaSanPham(int id)
        {
            if (!IsAuthorized())
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" }, JsonRequestBehavior.AllowGet);
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction("Index");
            }
            var sp = db.SanPhams.Find(id);
            if (sp == null) return HttpNotFound();
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", sp.MaDanhMuc);
            
            if (Request.IsAjaxRequest())
            {
                return PartialView("_SuaSanPhamPartial", sp);
            }
            return View(sp);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SuaSanPham(SanPham sp, HttpPostedFileBase AnhChinhFile, IEnumerable<HttpPostedFileBase> AnhPhuFiles)
        {
            if (!IsAuthorized())
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" });
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction("Index");
            }
            if (sp.TrangThai == "Hết hàng")
            {
                sp.SoLuongTon = 0;
            }

            if (sp.GiaBan < 0)
            {
                ModelState.AddModelError("GiaBan", "Giá bán không được phép âm!");
            }
            if (!sp.SoLuongTon.HasValue)
            {
                ModelState.AddModelError("SoLuongTon", "Số lượng tồn không được để trống!");
            }
            else if (sp.SoLuongTon < 0)
            {
                ModelState.AddModelError("SoLuongTon", "Số lượng tồn không được phép âm!");
            }

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ!";
                    return Json(new { success = false, message = firstError });
                }
            }

            if (ModelState.IsValid)
            {
                string folderPath = Server.MapPath("~/Content/Images/Products/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var existingProduct = db.SanPhams.Find(sp.MaSanPham);
                if (existingProduct == null) return HttpNotFound();

                existingProduct.TenSanPham = sp.TenSanPham;
                existingProduct.MaDanhMuc = sp.MaDanhMuc;
                existingProduct.GiaBan = sp.GiaBan;
                existingProduct.SoLuongTon = sp.TrangThai == "Hết hàng" ? 0 : sp.SoLuongTon;
                existingProduct.MoTaTongQuan = sp.MoTaTongQuan;
                existingProduct.ThongSoKyThuat = sp.ThongSoKyThuat;
                existingProduct.TrangThai = sp.TrangThai;

                if (AnhChinhFile != null && AnhChinhFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AnhChinhFile.FileName);
                    string path = Path.Combine(folderPath, fileName);
                    AnhChinhFile.SaveAs(path);

                    var mainImage = db.HinhAnhSanPhams.FirstOrDefault(h => h.MaSanPham == existingProduct.MaSanPham && h.AnhChinh);
                    if (mainImage != null)
                    {
                        string oldPath = Path.Combine(folderPath, mainImage.DuongDanAnh);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        
                        mainImage.DuongDanAnh = fileName;
                    }
                    else
                    {
                        db.HinhAnhSanPhams.Add(new HinhAnhSanPham
                        {
                            MaSanPham = existingProduct.MaSanPham,
                            DuongDanAnh = fileName,
                            AnhChinh = true
                        });
                    }
                }

                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log("Cập nhật sản phẩm", $"Cập nhật thông tin sản phẩm '{existingProduct.TenSanPham}'", "Thành công");

                if (AnhPhuFiles != null && AnhPhuFiles.Any())
                {
                    string galleryPath = Server.MapPath("~/Content/Images/Gallery/");
                    if (!Directory.Exists(galleryPath))
                    {
                        Directory.CreateDirectory(galleryPath);
                    }

                    int currentCount = db.HinhAnhSanPhams.Count(h => h.MaSanPham == existingProduct.MaSanPham);
                    int canAdd = 9 - currentCount;

                    if (canAdd > 0)
                    {
                        var filesToSave = AnhPhuFiles.Where(f => f != null && f.ContentLength > 0).Take(canAdd);

                        foreach (var file in filesToSave)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string path = Path.Combine(galleryPath, fileName);
                            file.SaveAs(path);

                            db.HinhAnhSanPhams.Add(new HinhAnhSanPham
                            {
                                MaSanPham = existingProduct.MaSanPham,
                                DuongDanAnh = fileName
                            });
                        }
                        db.SaveChanges();
                    }
                }

                if (Request.IsAjaxRequest())
                    return Json(new { success = true });

                return RedirectToAction("Index");
            }
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", sp.MaDanhMuc);
            
            if (Request.IsAjaxRequest())
                return PartialView("_SuaSanPhamPartial", sp);

            return View(sp);
        }

        [HttpPost]
        public ActionResult XoaAnhPhu(int id)
        {
            if (!IsAuthorized())
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" });
            }
            var hinhAnh = db.HinhAnhSanPhams.Find(id);
            if (hinhAnh != null)
            {
                string path = Path.Combine(Server.MapPath("~/Content/Images/Gallery/"), hinhAnh.DuongDanAnh);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                db.HinhAnhSanPhams.Remove(hinhAnh);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy ảnh để xóa." });
        }

        [HttpPost]
        public ActionResult XoaSanPham(int id)
        {
            if (!IsAuthorized())
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" });
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction("Index");
            }
            var sp = db.SanPhams.Include(p => p.HinhAnhSanPhams).FirstOrDefault(p => p.MaSanPham == id);
            if (sp != null)
            {
                var hasOrders = db.ChiTietDonHangs.Any(ct => ct.MaSanPham == id);
                if (hasOrders)
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Không thể xóa sản phẩm đã phát sinh đơn hàng! Nếu muốn ẩn sản phẩm, vui lòng chuyển sang trạng thái ngừng kinh doanh." });
                    
                    TempData["Error"] = "Không thể xóa sản phẩm đã phát sinh đơn hàng! Nếu muốn ẩn sản phẩm, vui lòng chuyển sang trạng thái ngừng kinh doanh.";
                    return RedirectToAction("Index");
                }
                var images = sp.HinhAnhSanPhams.ToList();
                foreach (var img in images)
                {
                    db.HinhAnhSanPhams.Remove(img);
                }
                db.SanPhams.Remove(sp);
                db.SaveChanges();
                VuongBanDienTu.Helpers.ActivityLogger.Log("Xóa sản phẩm", $"Xóa sản phẩm '{sp.TenSanPham}'", "Thành công");
                
                if (Request.IsAjaxRequest())
                    return Json(new { success = true, message = "Đã xóa sản phẩm thành công!" });
            }

            if (Request.IsAjaxRequest())
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            return RedirectToAction("Index");
        }
    }
}
