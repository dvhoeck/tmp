
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gatewing.ProductionTools.BLL
{
    public class SatelliteComparison
    {
        /// <summary>
        /// Reference satellite
        /// </summary>
        public List<Satellite> ReferenceSatellites { get; set; }

        /// <summary>
        /// Test satellite
        /// </summary>
        public List<Satellite> TestSatellites { get; set; }

        /// <summary>
        /// Returns deviance of a particular subchannel on a particular satellite compared to another satellite / subchannel
        /// </summary>
        /// <param name="referenceSatellite"></param>
        /// <param name="testSatellite"></param>
        /// <param name="codeType"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        public decimal SubChannelDeviance(Satellite referenceSatellite, Satellite testSatellite, int codeType, int frequency)
        {
            if (referenceSatellite == null || testSatellite == null)
                return 100m;

            var referenceSubChannel = referenceSatellite.SubChannels.Where(x => x.CodeType == codeType && x.Frequency == frequency).FirstOrDefault();
            var testSubChannel = testSatellite.SubChannels.Where(x => x.CodeType == codeType && x.Frequency == frequency).FirstOrDefault();

            // both subchannels are non-existant
            if (referenceSubChannel == null || testSubChannel == null)
                return 0;

            // both subchannels have "0" signal to noise ration
            if (testSubChannel.SNRx4 == 0 && referenceSubChannel.SNRx4 == 0)
                return 100;

            // one of the subchannels has "0" signal to noise ratio while the other has a valid ratio
            if (testSubChannel.SNRx4 == 0 || referenceSubChannel.SNRx4 == 0)
                return 0;

            var result = 0m;
            if (testSubChannel.SNRx4 > referenceSubChannel.SNRx4)
                result = ((decimal)referenceSubChannel.SNRx4 / (decimal)testSubChannel.SNRx4) * 100;
            else
                result = ((decimal)testSubChannel.SNRx4 / (decimal)referenceSubChannel.SNRx4) * 100;

            return result;
        }

        private decimal _overallDeviance = 0;
        public decimal OverallDeviance
        {
            get
            {
                if (_overallDeviance == 0)
                    _overallDeviance = OverallDevianceCalculation();

                return _overallDeviance;
            }
            set { _overallDeviance = value; }
        }

        /// <summary>
        /// Percentual deviance across all satellites
        /// </summary>
        private decimal OverallDevianceCalculation()
        {
            var result = 0m;
            var count = 0m;

            // iterate over all possible satellite prn's
            for (var prn = 0; prn < 73; prn++)
            {
                var testSat = TestSatellites.Where(x => x.SatellitePrn == prn).FirstOrDefault();
                var refSat = ReferenceSatellites.Where(x => x.SatellitePrn == prn).FirstOrDefault();

                // a test fix and reference sat fix are found for this PRN
                if (testSat != null && refSat != null)
                {
                    // iterate over all possible satellite subchannels
                    for (var i = 0; i < 2; i++)
                    {
                        // take non mutual subchannel fixes into account
                        if ((testSat.SubChannels[i] == null && refSat.SubChannels[i] != null) || (testSat.SubChannels[i] != null && refSat.SubChannels[i] == null))
                        {
                            result += 0;
                            count++;
                        }

                        if (testSat.SubChannels[i] != null && refSat.SubChannels[i] != null)
                        {
                            result += SubChannelDeviance(refSat, testSat, refSat.SubChannels[0].CodeType, refSat.SubChannels[0].Frequency);
                            count++;
                        }
                    }
                }
            }

            if (count == 0)
                return 0m;

            return result / count;
        }
    }
}
