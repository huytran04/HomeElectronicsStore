using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VuongBanDienTu.Models;
using VuongBanDienTu.Services;

namespace VuongBanDienTu.Controllers
{
    public class ThanhToanController : Controller
    {
        private VuongDienTuEntities db = new VuongDienTuEntities();
        private VnPayService _vnPayService = new VnPayService();

        public ActionResult ThanhToanVnPay(int orderId)
        {
            var dh = db.DonHangs.Find(orderId);
            if (dh == null) return HttpNotFound();

            string ipAddress = Request.UserHostAddress;
            string paymentUrl = _vnPayService.CreatePaymentUrl(dh.MaDonHang, dh.TongTien ?? 0, ipAddress);

            return Redirect(paymentUrl);
        }

        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                int orderId = Convert.ToInt32(vnpay.GetResponseData("vnp_TxnRef"));
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                String vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        var dh = db.DonHangs.Find(orderId);
                        if (dh != null)
                        {
                            dh.TrangThaiThanhToan = "Đã thanh toán";
                            dh.TrangThaiDonHang = "Chờ xử lý";
                            
                            var thanhToan = new ThanhToan
                            {
                                MaDonHang = orderId,
                                NgayThanhToan = DateTime.Now,
                                SoTien = vnp_Amount,
                                PhuongThuc = "VNPAY",
                                NoiDung = vnp_OrderInfo
                            };
                            db.ThanhToans.Add(thanhToan);
                            db.SaveChanges();
                        }
                        ViewBag.InnerText = "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ";
                    }
                    else
                    {
                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: " + vnp_ResponseCode;
                    }
                }
                else
                {
                    ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý (Sai chữ ký)";
                }

                ViewBag.OrderId = orderId;
                ViewBag.VnpayTranId = vnpayTranId;
                ViewBag.Amount = vnp_Amount;
            }
            return View();
        }

        public JsonResult VnpayIPN()
        {
            string returnCode = "00";
            string message = "Confirm Success";

            try
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                int orderId = Convert.ToInt32(vnpay.GetResponseData("vnp_TxnRef"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    var dh = db.DonHangs.Find(orderId);
                    if (dh != null)
                    {
                        if (dh.TongTien == vnp_Amount)
                        {
                            if (dh.TrangThaiThanhToan == "Chưa thanh toán")
                            {
                                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                                {
                                    dh.TrangThaiThanhToan = "Đã thanh toán";
                                    dh.TrangThaiDonHang = "Chờ xử lý";
                                }
                                else
                                {
                                    dh.TrangThaiThanhToan = "Thanh toán lỗi";
                                }
                                
                                if (dh.TrangThaiThanhToan == "Đã thanh toán")
                                {
                                    db.ThanhToans.Add(new ThanhToan
                                    {
                                        MaDonHang = orderId,
                                        NgayThanhToan = DateTime.Now,
                                        SoTien = vnp_Amount,
                                        PhuongThuc = "VNPAY",
                                        NoiDung = vnpay.GetResponseData("vnp_OrderInfo")
                                    });
                                }

                                db.SaveChanges();
                            }
                            else
                            {
                                returnCode = "02";
                                message = "Order already confirmed";
                            }
                        }
                        else
                        {
                            returnCode = "04";
                            message = "Invalid amount";
                        }
                    }
                    else
                    {
                        returnCode = "01";
                        message = "Order not found";
                    }
                }
                else
                {
                    returnCode = "97";
                    message = "Invalid signature";
                }
            }
            catch (Exception)
            {
                returnCode = "99";
                message = "Input data invalid";
            }

            return Json(new { RspCode = returnCode, Message = message }, JsonRequestBehavior.AllowGet);
        }
    }
}
