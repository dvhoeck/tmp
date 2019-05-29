using System;
using System.Collections.Generic;
using System.Linq;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// 
    /// </summary>
    public class ComparisonResult
    {
        private List<ComparisonResultHelper> _comparisonResultHelpers = new List<ComparisonResultHelper>();
        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public List<ComparisonResultHelper> Results
        {
            get
            {
                return _comparisonResultHelpers.OrderBy(x => x.SatellitePrn).OrderBy(x => x.SystemId).ToList();
            }
        }


        private GBoxToolingConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComparisonResult" /> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public ComparisonResult(GBoxToolingConfig config)
        {
            _config = config;
        }


        /// <summary>
        /// Calculates the results.
        /// </summary>
        /// <param name="refSats">The reference sats.</param>
        /// <param name="testSats">The test sats.</param>
        public void CalculateResults(List<Satellite> refSats, List<Satellite> testSats)
        {
            // iterate over all test satellites
            foreach (var testSat in testSats)
            {
                // try to retrieve corresponding reference satellite
                var refSat = refSats.Where(x => x.SystemId == testSat.SystemId && x.SatellitePrn == testSat.SatellitePrn).FirstOrDefault();

                // try to retrieve an existing helper object
                var comparisonResultHelper = _comparisonResultHelpers.Where(x => x.SatellitePrn == testSat.SatellitePrn && x.SystemId == testSat.SystemId).FirstOrDefault();
                if (comparisonResultHelper == null)
                {
                    comparisonResultHelper = new ComparisonResultHelper
                    {
                        SatellitePrn = testSat.SatellitePrn,
                        SystemId = testSat.SystemId
                    };

                    _comparisonResultHelpers.Add(comparisonResultHelper);
                }

                // add current score
                comparisonResultHelper.CurrentTestChannel01Score = testSat.SubChannels[0] != null ? testSat.SubChannels[0].SNRx4 : 0;
                comparisonResultHelper.CurrentTestChannel02Score = testSat.SubChannels[1] != null ? testSat.SubChannels[1].SNRx4 : 0;
                comparisonResultHelper.CurrentRefChannel01Score = refSat != null ? (refSat.SubChannels[0] != null ? refSat.SubChannels[0].SNRx4 : 0) : 0;
                comparisonResultHelper.CurrentRefChannel02Score = refSat != null ? (refSat.SubChannels[1] != null ? refSat.SubChannels[1].SNRx4 : 0) : 0;

                // add current score to history collections
                comparisonResultHelper.TestChannel01History.Add(DateTime.Now, comparisonResultHelper.CurrentTestChannel01Score);
                comparisonResultHelper.TestChannel02History.Add(DateTime.Now, comparisonResultHelper.CurrentTestChannel02Score);
                comparisonResultHelper.RefChannel01History.Add(DateTime.Now, comparisonResultHelper.CurrentRefChannel01Score);
                comparisonResultHelper.RefChannel02History.Add(DateTime.Now, comparisonResultHelper.CurrentRefChannel02Score);

                // and recalculate means from the all results within the sliding window
                comparisonResultHelper.AverageTestDeviceFirstChannelScore = (comparisonResultHelper.TestChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow)
                    .Sum(x => x.Value) / comparisonResultHelper.TestChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow).Count());
                comparisonResultHelper.AverageTestDeviceFirstChannelScore = (comparisonResultHelper.TestChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow)
                    .Sum(x => x.Value) / comparisonResultHelper.TestChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow).Count());
                comparisonResultHelper.AverageTestDeviceSecondChannelScore = (comparisonResultHelper.TestChannel02History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow)
                    .Sum(x => x.Value) / comparisonResultHelper.TestChannel02History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow).Count());
                comparisonResultHelper.AverageReferenceDeviceFirstChannelScore = (comparisonResultHelper.RefChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow)
                    .Sum(x => x.Value) / comparisonResultHelper.RefChannel01History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow).Count());
                comparisonResultHelper.AverageReferenceDeviceSecondChannelScore = (comparisonResultHelper.RefChannel02History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow)
                    .Sum(x => x.Value) / comparisonResultHelper.RefChannel02History
                    .Where(x => x.Key >= DateTime.Now - _config.SlidingWindow).Count());
            }
        }

        /// <summary>
        /// Gets the overall test signal strengths compared to reference strengths.
        /// </summary>
        /// <value>
        /// The overall test signal strengths compared to reference.
        /// </value>
        public decimal OverallTestSignalStrengthsComparedToReference
        {
            get
            {
                var testDeviceFirstChannelSum = _comparisonResultHelpers.Sum(x => x.AverageTestDeviceFirstChannelScore);
                var testDeviceSecondChannelSum = _comparisonResultHelpers.Sum(x => x.AverageTestDeviceSecondChannelScore);

                var refDeviceFirstChannelSum = _comparisonResultHelpers.Sum(x => x.AverageReferenceDeviceFirstChannelScore);
                var refDeviceSecondChannelSum = _comparisonResultHelpers.Sum(x => x.AverageReferenceDeviceSecondChannelScore);

                var firstChannelStrengthPercentage = refDeviceFirstChannelSum > 0 ? (testDeviceFirstChannelSum / refDeviceFirstChannelSum) * 100 : 0;
                var secondChannelStrengthPercentage = refDeviceSecondChannelSum > 0 ? (testDeviceSecondChannelSum/ refDeviceSecondChannelSum) * 100 : 0;

                return (firstChannelStrengthPercentage + secondChannelStrengthPercentage) / 2;
            }
        }

        /// <summary>
        /// Gets the amount of valid glonass fixes.
        /// </summary>
        /// <value>
        /// The valid glonass fixes.
        /// </value>
        public int ValidGlonassFixesCount
        {
            get { return GetValidFixes().Where(x => x.SystemId == 2).Count(); }
        }

        /// <summary>
        /// Gets the amount of valid GPS fixes.
        /// </summary>
        /// <value>
        /// The valid GPS fixes.
        /// </value>
        public int ValidGpsFixesCount
        {
            get { return GetValidFixes().Where(x => x.SystemId == 0).Count(); }
        }

        /// <summary>
        /// Gets the collection of valid fixes.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ComparisonResultHelper> GetValidFixes()
        {
            var validFixes = new List<ComparisonResultHelper>();
            foreach (var helper in _comparisonResultHelpers)
            {
                helper.Channel01IsValid = (helper.AverageReferenceDeviceFirstChannelScoreDiv4 - helper.AverageTestDeviceFirstChannelScoreDiv4) <= (_config.UseAsDataLogger ? _config.AllowedChannel01SignalToNoiseRatioDeviationForLogging : _config.AllowedChannel01SignalToNoiseRatioDeviation);
                helper.Channel02IsValid = (helper.AverageReferenceDeviceSecondChannelScoreDiv4 - helper.AverageTestDeviceSecondChannelScoreDiv4) <= (_config.UseAsDataLogger ? _config.AllowedChannel02SignalToNoiseRatioDeviationForLogging : _config.AllowedChannel02SignalToNoiseRatioDeviation);

                if (helper.Channel01IsValid && helper.Channel02IsValid)
                    validFixes.Add(helper);
            }
            
            return validFixes;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ComparisonResultHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComparisonResultHelper" /> class.
        /// </summary>
        public ComparisonResultHelper(){
            TestChannel01History = new Dictionary<DateTime, decimal>();
            TestChannel02History = new Dictionary<DateTime, decimal>();
            RefChannel01History = new Dictionary<DateTime, decimal>();
            RefChannel02History = new Dictionary<DateTime, decimal>();
        }

        public int SystemId { get; set; }
        public int SatellitePrn { get; set; }

        public decimal AverageTestDeviceFirstChannelScore { get; set; }
        public decimal AverageTestDeviceSecondChannelScore { get; set; }
        public decimal AverageReferenceDeviceFirstChannelScore { get; set; }
        public decimal AverageReferenceDeviceSecondChannelScore { get; set; }

        public decimal AverageTestDeviceFirstChannelScoreDiv4 { get { return AverageReferenceDeviceFirstChannelScore / 4; } }
        public decimal AverageTestDeviceSecondChannelScoreDiv4 { get { return AverageTestDeviceSecondChannelScore / 4; } }
        public decimal AverageReferenceDeviceFirstChannelScoreDiv4 { get { return AverageReferenceDeviceFirstChannelScore / 4; } }
        public decimal AverageReferenceDeviceSecondChannelScoreDiv4 { get { return AverageReferenceDeviceSecondChannelScore / 4; } }

        public Dictionary<DateTime, decimal> TestChannel01History { get; set; }
        public Dictionary<DateTime, decimal> TestChannel02History { get; set; }
        public Dictionary<DateTime, decimal> RefChannel01History { get; set; }
        public Dictionary<DateTime, decimal> RefChannel02History { get; set; }

        public decimal CurrentRefChannel01Score{ get; set; }
        public decimal CurrentRefChannel02Score { get; set; }
        public decimal CurrentTestChannel01Score { get; set; }
        public decimal CurrentTestChannel02Score { get; set; }

        public bool Channel01IsValid { get; set; }
        public bool Channel02IsValid { get; set; }
    }
}
