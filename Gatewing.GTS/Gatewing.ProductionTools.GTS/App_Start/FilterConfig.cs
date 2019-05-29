using System.Web;
using System.Web.Mvc;

namespace Gatewing.ProductionTools.GTS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
