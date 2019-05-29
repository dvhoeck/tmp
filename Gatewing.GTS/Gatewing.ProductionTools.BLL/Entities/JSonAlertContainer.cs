using System;
using System.Collections.Generic;

namespace Gatewing.ProductionTools.BLL
{
    public class JSonAlertContainer
    {
        public List<JSonAlert> Alerts { get; set; }
    }

    public class JSonAlert
    {
        public Guid Id { get; set; }
        public string Section { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
