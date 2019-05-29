using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute("GTS", typeof(Gatewing.ProductionTools.GTS.Startup))]

namespace Gatewing.ProductionTools.GTS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}