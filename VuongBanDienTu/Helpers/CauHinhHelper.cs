using System;
using System.IO;
using System.Web;
using Newtonsoft.Json;

namespace VuongBanDienTu.Helpers
{
    public class CauHinhModel
    {
        public string StoreName { get; set; }
        public string StoreAddress { get; set; }
        public string StoreHotline { get; set; }
        public string StoreEmail { get; set; }
        public string StoreWorkingHours { get; set; }
        public string StoreMapEmbed { get; set; }
        public string StoreMapUrl { get; set; }
        public bool MaintenanceMode { get; set; }
    }

    public static class CauHinhHelper
    {
        private static readonly string FilePath = HttpContext.Current.Server.MapPath("~/App_Data/cauhinh.json");
        private static CauHinhModel _currentConfig;
        private static readonly object LockObject = new object();

        public static CauHinhModel LayCauHinh()
        {
            if (_currentConfig != null)
            {
                return _currentConfig;
            }

            lock (LockObject)
            {
                if (_currentConfig != null)
                {
                    return _currentConfig;
                }

                try
                {
                    if (File.Exists(FilePath))
                    {
                        string json = File.ReadAllText(FilePath);
                        _currentConfig = JsonConvert.DeserializeObject<CauHinhModel>(json);
                    }
                    else
                    {
                        _currentConfig = CreateDefaultConfig();
                    }
                }
                catch
                {
                    _currentConfig = CreateDefaultConfig();
                }
            }

            return _currentConfig;
        }

        public static bool LuuCauHinh(CauHinhModel config)
        {
            if (config == null) return false;

            lock (LockObject)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(FilePath, json);
                    _currentConfig = config; // Update cached config
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static CauHinhModel CreateDefaultConfig()
        {
            var config = new CauHinhModel
            {
                StoreName = "Vương Bản Điện Tử",
                StoreAddress = "24 Nguyễn Du, Phường 7, Tuy Hòa, Phú Yên",
                StoreHotline = "1800.6800",
                StoreEmail = "huynhkimvuong.d22ctc1@muce.edu.vn",
                StoreWorkingHours = "08:00 - 22:00 (Cả tuần)",
                StoreMapEmbed = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3885.6177100999676!2d109.28865247595452!3d13.12338748720615!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x316fec8410ea0c51%3A0x1f139522d7911691!2zVHLGsOG7nW5nIMSQ4bqhaSBo4buNYyBYw6J5IGThu7FuZyBNaeG7gW4gVHJ1bmc!5e0!3m2!1svi!2s!4v1779294567889!5m2!1svi!2s",
                StoreMapUrl = "https://www.google.com/maps/search/?api=1&query=Tr%C6%B0%E1%BB%9Dng%20%C4%90%E1%BA%A1i%20h%E1%BB%8Dc%20X%C3%A2y%20d%E1%BB%B1ng%20Mi%E1%BB%81n%20Trung",
                MaintenanceMode = false
            };

            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch { }

            return config;
        }

        public static string LayLinkMapEmbed(string rawEmbed)
        {
            if (string.IsNullOrEmpty(rawEmbed)) return "";
            if (rawEmbed.Contains("<iframe"))
            {
                int srcIndex = rawEmbed.IndexOf("src=\"");
                if (srcIndex != -1)
                {
                    int startIndex = srcIndex + 5;
                    int endIndex = rawEmbed.IndexOf("\"", startIndex);
                    if (endIndex != -1)
                    {
                        return rawEmbed.Substring(startIndex, endIndex - startIndex);
                    }
                }
                
                int srcSingleIndex = rawEmbed.IndexOf("src='");
                if (srcSingleIndex != -1)
                {
                    int startIndex = srcSingleIndex + 5;
                    int endIndex = rawEmbed.IndexOf("'", startIndex);
                    if (endIndex != -1)
                    {
                        return rawEmbed.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            return rawEmbed;
        }
    }
}
