using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using System.IO;
using System.Data.Entity;

namespace VuongBanDienTu.Controllers
{
    public class QuanLySanPhamController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();

        public ActionResult Index()
        {
            var products = db.SanPhams.Include(p => p.DanhMuc).Include(p => p.HinhAnhSanPhams).OrderByDescending(p => p.NgayTao).ToList();
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
            if (ModelState.IsValid)
            {
                // Đảm bảo thư mục tồn tại
                string folderPath = Server.MapPath("~/Content/Images/Products/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Xử lý ảnh chính
                if (AnhChinhFile != null && AnhChinhFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AnhChinhFile.FileName);
                    string path = Path.Combine(folderPath, fileName);
                    AnhChinhFile.SaveAs(path);
                    sp.AnhChinh = fileName;
                }

                sp.NgayTao = DateTime.Now;
                db.SanPhams.Add(sp);
                db.SaveChanges(); // Lưu SP để lấy MaSanPham

                // Xử lý ảnh phụ
                if (AnhPhuFiles != null && AnhPhuFiles.Any())
                {
                    string galleryPath = Server.MapPath("~/Content/Images/Gallery/");
                    if (!Directory.Exists(galleryPath))
                    {
                        Directory.CreateDirectory(galleryPath);
                    }

                    // Lấy tối đa 9 ảnh
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
            if (ModelState.IsValid)
            {
                // Đảm bảo thư mục tồn tại
                string folderPath = Server.MapPath("~/Content/Images/Products/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var existingProduct = db.SanPhams.Find(sp.MaSanPham);
                if (existingProduct == null) return HttpNotFound();

                // Cập nhật thông tin cơ bản
                existingProduct.TenSanPham = sp.TenSanPham;
                existingProduct.MaDanhMuc = sp.MaDanhMuc;
                existingProduct.GiaBan = sp.GiaBan;
                existingProduct.SoLuongTon = sp.SoLuongTon;
                existingProduct.MoTaTongQuan = sp.MoTaTongQuan;
                existingProduct.ThongSoKyThuat = sp.ThongSoKyThuat;
                existingProduct.TrangThai = sp.TrangThai;

                // Xử lý ảnh chính mới nếu có
                if (AnhChinhFile != null && AnhChinhFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AnhChinhFile.FileName);
                    string path = Path.Combine(folderPath, fileName);
                    AnhChinhFile.SaveAs(path);
                    existingProduct.AnhChinh = fileName;
                }

                db.SaveChanges();

                // Xử lý thêm ảnh phụ mới
                if (AnhPhuFiles != null && AnhPhuFiles.Any())
                {
                    string galleryPath = Server.MapPath("~/Content/Images/Gallery/");
                    if (!Directory.Exists(galleryPath))
                    {
                        Directory.CreateDirectory(galleryPath);
                    }

                    // Kiểm tra số lượng ảnh hiện có
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
            var hinhAnh = db.HinhAnhSanPhams.Find(id);
            if (hinhAnh != null)
            {
                // Xóa file vật lý trong thư mục Gallery
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
            var sp = db.SanPhams.Find(id);
            if (sp != null)
            {
                db.SanPhams.Remove(sp);
                db.SaveChanges();
            }

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }
    }
}
