using System;
using System.Collections.Generic;

namespace Gatewing.ProductionTools.BLL
{
    public class JSonBurnInResultContainer
    {
        public List<BurnIn> BurnInData { get; set; }
        public Dictionary<int, bool> CurrentServoStateList { get; set; }
        public ServoFailMeanCalculation ServoFailMeanCalculation { get; set; }
        public TimeSpan BurnInTime { get; set; }
    }
}
