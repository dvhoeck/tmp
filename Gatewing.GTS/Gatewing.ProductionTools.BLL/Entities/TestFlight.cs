using Gatewing.ProductionTools.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="Gatewing.ProductionTools.BLL.DomainObject" />
    public class TestFlight : DomainObject
    {
        public TestFlight()
        {
        }

        public TestFlight(Guid id, int flightNr, string wingId, string eboxId, int samples, int elevator, int aileron,
            int shutterCommands, int undershoot, DateTime timestamp, int amountOfImagesInDir, int amountOfLogsInDir,
            string windDirection, string windSpeed, string condition, bool damaged, string launcherSerial, string pilot,
            bool pilotApproval, bool logsDownloadOk, int testPictures, int gBoxEvents, bool crash, int minMaxElevator,
            int minMaxAileron, string comment = "")
        {
            Id = id;
            WingId = wingId;
            EboxId = eboxId;
            Comment = comment;
            FlightNumber = flightNr;
            Samples = samples;
            Elevator = elevator;
            Aileron = aileron;
            ShutterCommands = shutterCommands;
            Undershoot = undershoot;
            TimeStamp = timestamp;
            AmountOfImagesUploaded = amountOfImagesInDir;
            AmountOfLogsUploaded = amountOfLogsInDir;
            WindDirection = windDirection;
            WindSpeed = windSpeed;
            WeatherCondition = condition;
            Damaged = damaged;
            LauncherSerial = launcherSerial;
            Pilot = pilot;
            PilotApproval = pilotApproval;
            TestPictures = testPictures;
            LogsDownloadOk = logsDownloadOk;
            GBoxEvents = gBoxEvents;
            Crash = crash;
            MinMaxElevator = minMaxElevator;
            MinMaxAileron = minMaxAileron;
        }

        [Display(Name = "Wing ID")]
        public virtual string WingId { get; set; }

        /// <summary>
        /// Gets the type of the wing.
        /// </summary>
        /// <value>
        /// The type of the wing.
        /// </value>
        /// <exception cref="System.InvalidOperationException">Can not determine type of wing from wing id.</exception>
        public virtual int WingType
        {
            get
            {
                if (WingId.StartsWith("0000"))
                    return 0;

                if (WingId.StartsWith("0001"))
                    return 1;

                if (WingId.StartsWith("0002"))
                    return 2;

                throw new InvalidOperationException("Can not determine type of wing from wing id.");
            }
        }

        [Display(Name = "Ebox ID")]
        [CustomValidation(typeof(TestFlight), "CheckIDs")]
        public virtual string EboxId { get; set; }

        public virtual int FlightNumber { get; set; }

        [CustomValidation(typeof(TestFlight), "CheckSamples")]
        public virtual int Samples { get; set; }

        [CustomValidation(typeof(TestFlight), "CheckElevonTrim")]
        public virtual int Elevator { get; set; }

        public virtual int Aileron { get; set; }

        public virtual int MinMaxElevator { get; set; }

        public virtual int MinMaxAileron { get; set; }

        public virtual int Undershoot { get; set; }

        public virtual DateTime TimeStamp { get; set; }

        /// <summary>
        /// Total amount of images uploaded from SD card
        /// </summary>
        [CustomValidation(typeof(TestFlight), "CheckImagesAndFeedbackEvents")]
        public virtual int AmountOfImagesUploaded { get; set; }

        /// <summary>
        /// Total commands given to camera to take pictures
        /// </summary>
        public virtual int ShutterCommands { get; set; }

        /// <summary>
        /// GBox feedback events
        /// </summary>
        [CustomValidation(typeof(TestFlight), "CheckFeedbackEventsCount")]
        public virtual int GBoxEvents { get; set; }

        /// <summary>
        /// EBox feedback events
        /// </summary>
        [CustomValidation(typeof(TestFlight), "CheckFeedbackEventsCount")]
        public virtual int EBoxEvents { get; set; }

        /// <summary>
        /// Gets or sets the amount of logs uploaded.
        /// </summary>
        /// <value>
        /// The amount of logs uploaded.
        /// </value>
        [CustomValidation(typeof(TestFlight), "CheckForLogAndImages")]
        public virtual int AmountOfLogsUploaded { get; set; }

        public virtual string WindDirection { get; set; }

        public virtual string WindSpeed { get; set; }

        public virtual string WeatherCondition { get; set; }

        [CustomValidation(typeof(TestFlight), "CheckCommentsIfPilotNotApproved")]
        private string _comment;

        public virtual string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value.RemoveIllegalChars();
            }
        }

        [MustBeFalse(ErrorMessage = "Wing is damaged.")]
        public virtual bool Damaged { get; set; }

        public virtual string DamagedStr
        {
            get { return Damaged.ToYesOrNo(); }
            set { Damaged = value == "Yes"; }
        }

        [MustBeFalse(ErrorMessage = "Wing has crashed.")]
        public virtual bool Crash { get; set; }

        public virtual string CrashStr
        {
            get { return Crash.ToYesOrNo(); }
            set { Crash = value == "Yes"; }
        }

        public virtual string Time { get { return TimeStamp.ToString("t"); } }

        public virtual string FlightNumberAsString { get { return FlightNumber.ToString().PadLeft(2, '0'); } }

        public virtual string LauncherSerial { get; set; }

        public virtual string TempFolderName { get { return string.Format("{0} {1} {2}", FlightNumberAsString, WingId, EboxId); } }

        public virtual string ShortName { get { return string.Format("{0} - {1} - {2} - {3}", FlightNumberAsString, TimeStamp.ToShortTimeString(), WingId, EboxId); } }

        public virtual string Pilot { get; set; }

        [MustBeTrue(ErrorMessage = "Pilot did not approve flight.")]
        public virtual bool PilotApproval { get; set; }

        public virtual string PilotApprovalStr
        {
            get { return PilotApproval.ToYesOrNo(); }
            set { PilotApproval = value == "Yes"; }
        }

        [MustBeTrue(ErrorMessage = "Log download failed.")]
        public virtual bool LogsDownloadOk { get; set; }

        public virtual string LogsDownloadOkStr
        {
            get { return LogsDownloadOk.ToYesOrNo(); }
            set { LogsDownloadOk = value == "Yes"; }
        }

        public virtual int TestPictures { get; set; }

        /// <summary>
        /// Returns true if this flight contains all required data
        /// </summary>
        /// <returns></returns>
        public virtual bool IsComplete { get { return (AmountOfImagesUploaded > 0 && AmountOfLogsUploaded > 0); } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Gatewing.TestFlights.UI.Wing"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Gatewing.TestFlights.UI.Wing"/>.</returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Comment) || Comment == " ")
                return string.Format("{0} {1} {2} S {3} E {4} A {5} P {6} U {7}", FlightNumberAsString, WingId, EboxId, Samples, Elevator, Aileron, ShutterCommands, Undershoot);

            return string.Format("{0} {1} {2} S {3} E {4} A {5} P {6} U {7} ({8})", FlightNumberAsString, WingId, EboxId, Samples, Elevator, Aileron, ShutterCommands, Undershoot, Comment);
        }

        /// <summary>
        /// Custom validation method. Will check amount of images and logs uploaded
        /// </summary>
        /// <param name="AmountOfLogsUploaded"></param>
        /// <param name="pValidationContext"></param>
        /// <returns></returns>
        public static ValidationResult CheckForLogAndImages(int AmountOfLogsUploaded, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (flight.AmountOfLogsUploaded == 0 && flight.AmountOfImagesUploaded == 0)
                return new ValidationResultWithSeverity("To resolve a remark, on .", Severity.Warning);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Checks ids.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="pValidationContext">The p validation context.</param>
        /// <returns></returns>
        public static ValidationResult CheckIDs(string ID, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (flight.WingId == "0000000000" && (flight.EboxId == "00000000" || flight.EboxId == "EBX00000"))
                return new ValidationResultWithSeverity("This flight has no ID's for wing and / or eBox.", Severity.Critical);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Compares number of EBox and GBox events
        /// </summary>
        /// <param name="pValidationContext"></param>
        /// <returns></returns>
        public static ValidationResult CheckFeedbackEventsCount(int amount, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (flight.EBoxEvents > 0 && flight.GBoxEvents > 0 && flight.EBoxEvents != flight.GBoxEvents)
                return new ValidationResultWithSeverity("This flight has a different number of GBox events compared to its EBox events.", Severity.Warning);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Checks the images and feedback events.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="pValidationContext">The p validation context.</param>
        /// <returns></returns>
        public static ValidationResult CheckImagesAndFeedbackEvents(int amount, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (flight.EBoxEvents > 0 &&
                flight.GBoxEvents > 0 &&
                flight.AmountOfImagesUploaded > 0 &&
                (
                    flight.EBoxEvents != flight.GBoxEvents ||
                    flight.AmountOfImagesUploaded - flight.TestPictures != flight.EBoxEvents ||
                    flight.AmountOfImagesUploaded - flight.TestPictures != flight.GBoxEvents
                )
                )
                return new ValidationResultWithSeverity("This flight's total amount of images does not correspond to the amount of feedback events and test pictures.", Severity.Warning);

            if (flight.AmountOfImagesUploaded == 0 && (flight.EBoxEvents > 0 || flight.GBoxEvents > 0))
                return new ValidationResultWithSeverity("This flight has no images uploaded.", Severity.Warning);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Checks the comments if pilot did not approve the flight.
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <param name="pValidationContext">The p validation context.</param>
        /// <returns></returns>
        public static ValidationResult CheckCommentsIfPilotNotApproved(string comment, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (!flight.PilotApproval && string.IsNullOrEmpty(flight.Comment))
                return new ValidationResultWithSeverity("You must enter comments when setting \"Pilot approved\" to \"No\".", Severity.Critical);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Checks the elevon trim.
        /// </summary>
        /// <param name="trimValue">The trim value.</param>
        /// <param name="pValidationContext">The p validation context.</param>
        /// <returns></returns>
        public static ValidationResult CheckElevonTrim(int trimValue, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (flight.Elevator != 0 &&
                flight.Aileron != 0 &&
                (
                    flight.Elevator > flight.MinMaxElevator ||
                    flight.Elevator < -flight.MinMaxElevator ||
                    flight.Aileron > flight.MinMaxAileron ||
                    flight.Aileron < -flight.MinMaxAileron
                ))
                return new ValidationResultWithSeverity("One or more trimvalues are outside of their allowed range.", Severity.Warning);

            return ValidationResultWithSeverity.Success;
        }

        /// <summary>
        /// Checks the amount of samples.
        /// </summary>
        /// <param name="samples">The samples.</param>
        /// <param name="pValidationContext">The p validation context.</param>
        /// <returns></returns>
        public static ValidationResult CheckSamples(int samples, ValidationContext pValidationContext)
        {
            var flight = (TestFlight)pValidationContext.ObjectInstance;
            if (samples > 0 && samples <= 600)
                return new ValidationResultWithSeverity("This flight has insufficient samples.", Severity.Warning);

            return ValidationResultWithSeverity.Success;
        }
    }
}