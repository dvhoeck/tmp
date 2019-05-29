using System;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// a Simpler version of Singleton class with lazy initialization.
    /// </summary>
    public class GBoxToolingConfig : ConfigBase<GBoxToolingConfig>
    {
        // General
        public int ReferenceComportNumber { get; private set; }

        public int TestComportNumber { get; private set; }
        public bool UseAsDataLogger { get; set; } // this prop has a public setter
        public string DataLogLocation { get; private set; }
        public string BD930OptionCode { get; private set; }
        public string BD935OptionCode { get; private set; }
        public string TestInternalSerial { get; private set; }
        public string RefInternalSerial { get; private set; }

        // TestRules
        public int MinDeviationPercentage { get; private set; }

        public TimeSpan SlidingWindow { get; private set; }
        public TimeSpan MaxTestTime { get; set; } // this prop has a public setter
        public TimeSpan RequiredValidTestTime { get; private set; }
        public decimal AllowedChannel01SignalToNoiseRatioDeviation { get; private set; }
        public decimal AllowedChannel02SignalToNoiseRatioDeviation { get; private set; }
        public decimal AllowedChannel01SignalToNoiseRatioDeviationForLogging { get; private set; }
        public decimal AllowedChannel02SignalToNoiseRatioDeviationForLogging { get; private set; }
        public int TestIntervalInMilliseconds { get; private set; }
        public int MinRequiredGPSFixAmount { get; private set; }
        public int MinRequiredGLONASSFixAmount { get; private set; }

        // Firmware
        public string BD930FWFileName { get; private set; }

        public string BD935FWFileName { get; private set; }
        public string UpdateTool { get; private set; }

        // Clone file
        public string BD930CloneFileName { get; private set; }

        public string BD935CloneFileName { get; private set; }

        // not found in ini
        public bool DeviceIsBD935 { get; set; }
        public string DeviceId{ get; set; }
    }
}