using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.IO;
using System.Collections.Concurrent;

namespace ColoringBudsCache
{
    //https://coloringbuds.com/de/
    //https://coloringbuds.com/da/
    //https://coloringbuds.com/fi/
    //https://coloringbuds.com/sk/
    //https://coloringbuds.com/nl/
    public class WSCachedHtmlItem
    {
        public string Html { get; set; }
        public DateTime SoftExpiry { get; set; } 
    }

    public static class CacheHelper
    {

        private static readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        public static IHtmlString CachedPartial(this HtmlHelper htmlHelper, string partialViewName, object model, int softDurationHours = 2)
        {
            int langId = (htmlHelper.ViewBag.Language != null) ? (int)htmlHelper.ViewBag.Language : 0;
            string autoCacheKey = string.Format("html_{0}_{1}", partialViewName.Replace("_", ""), langId);
            return CachedPartial(htmlHelper, partialViewName, model, autoCacheKey, softDurationHours);
        }

        public static IHtmlString CachedPartial(this HtmlHelper htmlHelper, string partialViewName, object model, string cacheKey, int softDurationHours = 2)
        {

            WSCachedHtmlItem item = HttpRuntime.Cache[cacheKey] as WSCachedHtmlItem;

            if (item != null && string.IsNullOrWhiteSpace(item.Html))
            {
                HttpRuntime.Cache.Remove(cacheKey);
                item = null; 

            }

            if (item != null && DateTime.Now <= item.SoftExpiry)
            {
                return htmlHelper.Raw(item.Html);
            }

            var syncLock = _locks.GetOrAdd(cacheKey, _ => new object());

            lock (syncLock)
            {

                item = HttpRuntime.Cache[cacheKey] as WSCachedHtmlItem;

                if (item != null && string.IsNullOrWhiteSpace(item.Html))
                {
                    HttpRuntime.Cache.Remove(cacheKey);
                    item = null;
                }

                if (item != null && DateTime.Now <= item.SoftExpiry)
                {
                    return htmlHelper.Raw(item.Html);
                }

                string renderedHtml = RenderAndCache(
                    htmlHelper.ViewContext.Controller.ControllerContext,
                    partialViewName,
                    model,
                    htmlHelper.ViewData,
                    htmlHelper.ViewContext.TempData,
                    cacheKey,
                    softDurationHours
                );

                if (string.IsNullOrWhiteSpace(renderedHtml) && item != null && !string.IsNullOrWhiteSpace(item.Html))
                {

                    item.SoftExpiry = DateTime.Now.AddMinutes(5);
                    return htmlHelper.Raw(item.Html);
                }

                return htmlHelper.Raw(renderedHtml ?? string.Empty);
            }
        }

        private static string RenderAndCache(ControllerContext context, string partialName, object model, ViewDataDictionary vData, TempDataDictionary tData, string key, int hours)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(context, partialName);
                    if (viewResult.View == null) return null;

                    ViewDataDictionary newViewData = new ViewDataDictionary(vData) { Model = model };
                    ViewContext viewContext = new ViewContext(context, viewResult.View, newViewData, tData, sw);

                    viewResult.View.Render(viewContext, sw);
                    string htmlString = sw.ToString();

                    if (string.IsNullOrWhiteSpace(htmlString)) return null;

                    WSCachedHtmlItem newItem = new WSCachedHtmlItem
                    {
                        Html = htmlString,
                        SoftExpiry = DateTime.Now.AddHours(hours)
                    };

                    HttpRuntime.Cache.Insert(
                        key,
                        newItem,
                        null,
                        DateTime.Now.AddHours(hours + 12),
                        System.Web.Caching.Cache.NoSlidingExpiration,
                        System.Web.Caching.CacheItemPriority.Normal,
                        null
                    );

                    return htmlString;
                }
            }
            catch (Exception ex)
            {

                System.Diagnostics.Trace.TraceError($"Error RenderAndCache [{key}]: {ex.Message}");
                return null; 

            }
        }
    }
}
