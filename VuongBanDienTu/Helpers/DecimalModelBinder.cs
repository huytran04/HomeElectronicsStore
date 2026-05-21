using System;
using System.Web.Mvc;
using System.Globalization;

namespace VuongBanDienTu.Helpers
{
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == null || string.IsNullOrEmpty(valueProviderResult.AttemptedValue))
            {
                return 0m;
            }

            string value = valueProviderResult.AttemptedValue.Trim();
            decimal result;
            
            // 1. Try InvariantCulture (e.g., standard "10.5")
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            // 2. Try standard parsing with CurrentCulture (e.g., local server settings)
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }

            // 3. Force replace comma with dot and parse as Invariant
            if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            // 4. Force replace dot with comma and parse as vi-VN
            if (decimal.TryParse(value.Replace(".", ","), NumberStyles.Any, new CultureInfo("vi-VN"), out result))
            {
                return result;
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Định dạng số không hợp lệ.");
            return 0m;
        }
    }
}
