using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public enum BoxType
    {
        StandardSmall = 1,
        StandardMedium = 2,
        StandardLarge = 4,
        Standard = 8,
        Accessory = 16,
        SpareWing = 32,
        Launcher = 64,
        UX11Kit = 128,
        SpareKit = 256,
        Custom = 4096
    }

    public enum ComponentRequirement
    {
        None = 0,
        Revision = 1,
        Serial = 2,
        Both = 4
    }

    public enum EvaluationState
    {
        [Display(Name = "Accepted without remarks")]
        AcceptedWithoutRemarks = 1,

        [Display(Name = "Accepted with remarks")]
        AcceptedWithRemarks = 2,

        [Display(Name = "Blocked by remarks")]
        BlockedByRemarks = 4,

        [Display(Name = "Rejected")]
        Rejected = 8,

        [Display(Name = "In progress")]
        InProgress = 16
    }

    public enum ModelType
    {
        Standard = 1,
        AutoCreated = 2,
        QualityCheck = 4
    }

    public enum ReportType
    {
        AircraftIdWrite = 1,
        EBoxIdWrite = 2,
        BurnInComplete = 4,
        ModemCableTested = 8,
        GBoxConfigured = 16,
        GNSSTestCompleted = 32,
        EBoxConfigured = 64,
        ServoLeft = 128,
        ServoRight = 256
    }

    public enum SatelliteSystem
    {
        GPS = 0,
        SBAS = 1,
        GLONASS = 2,
        Galileo = 3,
        QZSS = 4,
        BeidouPreICD = 5,
        GPSOmniStar = 6,
        BeidouICD = 7,
        TerraLite = 8,
        IRNSS = 9,
        BeidouPhaseCorrection = 10,
        Unknow = 255
    }

    public enum Severity
    {
        Info = 1,
        Warning = 2,
        Critical = 4,
        Fatal = 8
    }

    public enum State
    {
        Default = 0,
        New = 1,
        Improvable = 2,
        Removable = 3,
        Mergable = 4,
        Obsolete = 5,
        Changed = 6,
        ToBeDiscussed = 7
    }

    public class SatelliteFrequencyCollection
    {
        private Dictionary<int, string> _list;

        public SatelliteFrequencyCollection()
        {
            _list = new Dictionary<int, string>();

            _list.Add(0, " L1 (GPS/GLONASS/Galileo/SBAS)");
            _list.Add(1, " L2 (GPS/GLONASS)");
            _list.Add(2, " L5/E5A - 1176.45MHz (GPS/SBAS/ Galileo E5A)");
            _list.Add(3, " E5B - 1207.14MHz (Galileo E5B, Beidou-2/Compass B2)");
            _list.Add(4, " E5A+B - 1191.795MHz (Galileo Combined E5A/B - AltBOC)");
            _list.Add(5, " E6 - 1278.75MHz (Galileo E6)");
            _list.Add(6, " B1 - 1561.098MHz (Beidou-2/Compass L1)");
            _list.Add(7, " B3 - 1268.52MHz (Beidou-2/Compass E6)");
            _list.Add(8, " E1 - 1589.742MHz (Beidou-2/Compass upper L1)");
            _list.Add(9, " G3 - 1202.025MHz (GLONASS G3 CDMA)");
            _list.Add(10, " XPS - 9752.0MHz (Terralite XPS)");
            _list.Add(11, " S1 - 2492.028MHz (IRNSS S band)");
        }

        public Dictionary<int, string> List
        {
            get { return _list; }
        }
    }

    public class SatelliteSignalCollection
    {
        private Dictionary<int, string> _list;

        public SatelliteSignalCollection()
        {
            _list = new Dictionary<int, string>();

            _list.Add(0, "C/A - (GPS/GLONASS/SBAS) / BOC(1,1) - Galileo");
            _list.Add(1, "P");
            _list.Add(2, "Enhanced Cross Correlation");
            _list.Add(3, "L2C (CM)");
            _list.Add(4, "L2C (CL)");
            _list.Add(5, "L2C (CM+CL)[2]");
            _list.Add(6, "L5 (I)");
            _list.Add(7, "L5 (Q)");
            _list.Add(8, "L5 (I+Q)");
            _list.Add(9, "Y");
            _list.Add(10, "M");
            _list.Add(11, "BPSK(10)-PD (Galileo E5A or E5B - Pilot & Data)");
            _list.Add(12, "BPSK(10)-P (Galileo E5A or E5B - Pilot only)");
            _list.Add(13, "BPSK(10)-D (Galileo E5A or E5B - Data only, Beidou-2/Compass B2 & B3?)");
            _list.Add(14, "AltBOC-Comp-PD (Galileo Component Mode AltBOC - Pilot & Data)");
            _list.Add(15, "AltBOC-Comp-P (Galileo Component Mode AltBOC - Pilot only)");
            _list.Add(16, "AltBOC-Comp-D (Galileo Component Mode AltBOC - Data only)");
            _list.Add(17, "AltBOC-PD (Galileo Non-Component Mode AltBOC - Pilot & Data)");
            _list.Add(18, "L2 Enhanced Cross Correlation (L1 estimate derived from P1 estimate (type 19))");
            _list.Add(19, "L1 Enhanced Cross Correlation (W-code tracking)");
            _list.Add(20, "BOC(1,1) Pilot & Data - Galileo E1/GPS L1C");
            _list.Add(21, "BOC(1,1) Pilot Only - Galileo E1/GPS L1C");
            _list.Add(22, "BOC(1,1) Data Only - Galileo E1/GPS L1C");
            _list.Add(23, "MBOC[1](1,1) Pilot & Data - Galileo E1/GPS L1C");
            _list.Add(24, "MBOC[1](1,1) Pilot Only - Galileo E1/GPS L1C");
            _list.Add(25, "MBOC[1](1,1) Data Only - Galileo E1/GPS L1C");
            _list.Add(26, "BPSK(2) (Beidou-2/Compass B1)");
            _list.Add(27, "BPSK(2) (Beidou-2/Compass B1-2) (Deprecated)");
            _list.Add(28, "BPSK(2) (Beidou-2/Compass B2)");
            _list.Add(29, "BPSK(10) (Beidou-2/Compass B3)");
            _list.Add(30, "QZSS SAIF signal (L1 C/A SBAS like signal)");
            _list.Add(31, "QZSS LEX signal (pilot component only)");
            _list.Add(32, "G3-PD (Glonass G3 CDMA BPSK(10) - Pilot & Data)");
            _list.Add(33, "G3-P (Glonass G3 CDMA BPSK(10) - Pilot)");
            _list.Add(34, "G3-D (Glonass G3 CDMA BPSK(10) - Data)");
            _list.Add(35, "Terralite XPS code");
            _list.Add(36, "E6-PD (Galileo E6 BPSK(5) - Pilot & Data)");
            _list.Add(37, "E6-P (Galileo E6 BPSK(5) - Pilot)");
            _list.Add(38, "E6-D (Galileo E6 BPSK(5) - Data)");
        }

        public Dictionary<int, string> List
        {
            get { return _list; }
        }
    }
}