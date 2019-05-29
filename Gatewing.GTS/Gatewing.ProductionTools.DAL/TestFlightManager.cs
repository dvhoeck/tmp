using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.CommandLine;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Gatewing.ProductionTools.DAL
{
    // Core class of this application, TestFlightManager manages all testflight data related matters
    public class TestFlightDataManager
    {
        private IDataRepository<TestFlight> _testFlightRepository = DataRepository<TestFlight>.Instance;

        /// <summary>
        /// Working directory path
        /// </summary>
        private string _dataPath;

        /// <summary>
        /// logfiles extensions, delimited by ";" and found in app.config
        /// </summary>
        private List<string> _logFilesExtensions = new List<string>();

        /// <summary>
        /// simplified version string
        /// </summary>
        private string _version;

        /// <summary>
        /// string to hold location information (will usually be Assenede)
        /// </summary>
        private string _location = null;

        /// <summary>
        /// string that contains the user names
        /// </summary>
        private string _userNameString = null;

        /// <summary>
        /// integer to check elevator tolerance
        /// </summary>
        private int _elevatorTolerance = 0;
        
        /// <summary>
        /// integer to check aileron tolerance
        /// </summary>
        private int _aileronTolerance = 0;

        /// <summary>
        /// boolean to determine if we should delete files after copying them
        /// </summary>
        private bool _doDeleteAfterCopy;

        /// <summary>
        /// Our fixed landing location in Assenede
        /// </summary>
        private GeoCoordinate _programmedLandingLocation = new GeoCoordinate(51.233338, 3.780976);

        /// <summary>
        /// Creates a new instance of the TestFlightDataManager and accepts configuration input parameters
        /// </summary>
        /// <param name="flightDataTextFileName"></param>
        /// <param name="dataPath"></param>
        /// <param name="logFilesExtensions"></param>
        /// <param name="version"></param>
        /// <param name="location"></param>
        /// <param name="userNameString"></param>
        /// <param name="elevatorTolerance"></param>
        /// <param name="aileronTolerance"></param>
        /// <param name="doDeleteAfterCopy"></param>
        public TestFlightDataManager( 
            string dataPath, 
            List<string> logFilesExtensions, 
            string version, 
            string location, 
            string userNameString,
            string elevatorTolerance,
            string aileronTolerance,
            bool doDeleteAfterCopy)
        {
            _dataPath = dataPath;
            _logFilesExtensions = logFilesExtensions;
            _version = version;
            _location = location;
            _userNameString = userNameString;

            _elevatorTolerance = Convert.ToInt32(elevatorTolerance);
            _aileronTolerance = Convert.ToInt32(aileronTolerance);

            _doDeleteAfterCopy = doDeleteAfterCopy;

            CreateTestFlightDataDir();
        }
     
        /// <summary>
        /// Finalize data in images: Rename dirs with logs and images to append log timestamp as prefix,
        /// create text file containing summary and clear the _flightDataTextFileName file of all data
        /// </summary>
        public void FinalizeTestData(List<TestFlight> flights)
        {
            // create list for new directory names
            var finalDirList = new List<string>();

            // rename dirs
            foreach (var dir in System.IO.Directory.EnumerateDirectories(_dataPath))
            {
                var timestampOfLogFile = "";
                foreach (var file in System.IO.Directory.GetFiles(dir).Where(file => file.EndsWith(".txt")))
                {
                    timestampOfLogFile = file.Substring(file.Length - 19, 15);
                    break;
                }

                if (!string.IsNullOrEmpty(timestampOfLogFile))
                {
                    // create a path for each flight
                    var dirPath = System.IO.Path.GetDirectoryName(dir);
                    var dirNameWithoutPath = System.IO.Path.GetFileName(dir);
                    var wingIdentifierPartsFromTempDirName = dirNameWithoutPath.Split(' ');
                    dirNameWithoutPath = flights.Where(
                        flight => 
                            flight.FlightNumberAsString == wingIdentifierPartsFromTempDirName[0]
                            && flight.WingId == wingIdentifierPartsFromTempDirName[1]
                            && flight.EboxId == wingIdentifierPartsFromTempDirName[2])
                        .FirstOrDefault().ToString();
                    var newDirPath = System.IO.Path.Combine(dirPath, timestampOfLogFile + " " + dirNameWithoutPath.Replace("\r", "").Replace("\n", ""));

                    // add it to the list of final directory names (used in summary)
                    finalDirList.Add(newDirPath);

                    // move temp directory to final (= rename directory in user terms)
                    System.IO.Directory.Move(dir, newDirPath);
                }
            }
            // """""""""""""""""""""""""""""""""""""""""""""

            // write summary html file
            #region HTML Generation
            var stringWriter = new StringWriter();
            using (var writer = new HtmlTextWriter(stringWriter))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Style, "font-family: Arial; font-size: 12px;");
                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(string.Format("These are the test results of {0}:", DateTime.Now.ToString("yy-MM-dd")));
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Timestamp");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Wing");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Ebox");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Samples");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Elevator");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Aileron");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("ShutterCommands");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Photos");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Undershoot");
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write("Comment");
                writer.RenderEndTag();

                writer.RenderEndTag();

                var c = 0;
                foreach (var flight in flights)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                    // conditional formatting
                    if (flight.Elevator >= _elevatorTolerance || flight.Elevator <= -_elevatorTolerance
                        || flight.Aileron >= _aileronTolerance || flight.Aileron <= -_aileronTolerance
                        || !string.IsNullOrEmpty(flight.Comment)
                        || flight.ShutterCommands >= flight.AmountOfImagesUploaded)
                        writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "#FF6600");

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.FlightNumberAsString);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(System.IO.Path.GetFileName(finalDirList[c]).Split(' ')[0]);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.WingId);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.EboxId);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.Samples);
                    writer.RenderEndTag();

                    // conditional formatting
                    if (flight.Elevator >= _elevatorTolerance || flight.Elevator <= -_elevatorTolerance)
                        writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "#FF6600");

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.Elevator);
                    writer.RenderEndTag();

                    // conditional formatting
                    if (flight.Aileron >= _aileronTolerance || flight.Aileron <= -_aileronTolerance)
                        writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "#FF6600");

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.Aileron);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.ShutterCommands);
                    writer.RenderEndTag();

                    // conditional formatting
                    if (flight.ShutterCommands >= flight.AmountOfImagesUploaded      )
                        writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "#FF6600");

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.AmountOfImagesUploaded);
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.Undershoot);
                    writer.RenderEndTag();

                    // conditional formatting
                    if (!string.IsNullOrEmpty(flight.Comment))
                        writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "#FF6600");

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(flight.Comment);
                    writer.RenderEndTag();

                    writer.RenderEndTag();
                    c++;
                }
                writer.RenderEndTag();
                writer.RenderBeginTag(HtmlTextWriterTag.Br);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write("Kind regards,");
                writer.RenderBeginTag(HtmlTextWriterTag.Br);
                writer.Write(_userNameString);
                writer.RenderBeginTag(HtmlTextWriterTag.Br);
                writer.Write(string.Format("(app version: {0})", _version));
                writer.RenderEndTag();
                writer.RenderEndTag();

                var returnstring = stringWriter.ToString();

                using (var w = File.AppendText(System.IO.Path.Combine(_dataPath, "final_data.html")))
                {
                    w.Write(returnstring);
                }
            }
            #endregion
            // """""""""""""""""""""""""""""""""""""""""""""

            // write txt with xls data
            #region Xls content generation
            using (var file = File.AppendText(System.IO.Path.Combine(_dataPath, "xls.txt")))
            {
                foreach (var directoryPath in finalDirList)
                {
                    var directoryName = System.IO.Path.GetFileName(directoryPath);
                    var dataParts = directoryName.Split(' ');
                    var correspondingFlight = flights.Where(flight => flight.FlightNumberAsString == dataParts[1]
                        && flight.WingId == dataParts[2]
                        && flight.EboxId == dataParts[3]).FirstOrDefault();

                    var isDamaged = "No";
                    if(correspondingFlight.Damaged)
                        isDamaged = "Yes";

                    var sb = new StringBuilder();                    
                    sb.Append(dataParts[0].Substring(0, 8));
                    sb.Append("\t");
                    sb.Append(string.Format("{0} {1} {2}", dataParts[0].Substring(9, 6), correspondingFlight.WingId, correspondingFlight.EboxId));
                    sb.Append("\t");
                    sb.Append(correspondingFlight.Samples);
                    sb.Append("\t");
                    sb.Append("\t");
                    sb.Append(correspondingFlight.Elevator);
                    sb.Append("\t");
                    sb.Append("\t");
                    sb.Append(correspondingFlight.Aileron);
                    sb.Append("\t");
                    sb.Append(correspondingFlight.ShutterCommands);
                    sb.Append("\t\t\t\t");
                    sb.Append(_location);
                    sb.Append("\t\t\t\t\t\t");
                    sb.Append(correspondingFlight.WindDirection);
                    sb.Append("\t");
                    sb.Append(correspondingFlight.WindSpeed);
                    sb.Append("\t");
                    sb.Append(correspondingFlight.WeatherCondition);
                    sb.Append("\t");
                    sb.Append(isDamaged);
                    sb.Append("\t");
                    sb.Append(_userNameString);
                    sb.Append("\t");
                    sb.Append(string.Format("U {0}",correspondingFlight.Undershoot));
                    sb.Append("\t");
                    sb.Append(correspondingFlight.Comment);
                    sb.Append("\r\n");

                    file.Write(sb.ToString());
                }
            }
            #endregion
            //"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
            
            // Delete flight data from database
            _testFlightRepository.DeleteAll();
        }

        /// <summary>
        /// Deletes a test flight from the database
        /// </summary>
        /// <param name="flight"></param>
        public void Delete(TestFlight flight)
        {
            _testFlightRepository.DeleteById(flight.Id);

            var flightNumber = flight.FlightNumber;
            _testFlightRepository.ReOrder(flight.FlightNumber, "FlightNumber");
        }

        /// <summary>
        /// Returns a path to one of the images from a test flight
        /// </summary>
        /// <param name="testFlight"></param>
        /// <returns></returns>
        public string GetRandomImagePathFromDir(TestFlight testFlight)
        {
            var path = Path.Combine(_dataPath, testFlight.TempFolderName);
            if (!Directory.Exists(path) || CountImages(path) == 0)
                return string.Empty;

            var files = Directory.GetFiles(path, "*.*").Where(file => file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".jpeg")).ToList();

            var random = new Random();
            var imagePath = files[random.Next(0, files.Count())];

            return imagePath;
        }

        /// <summary>
        /// Counts the logs in a given directory
        /// </summary>
        /// <param name="path">directory to count logs in</param>
        /// <returns>log count</returns>
        public int CountLogs(string path)
        {
            var logCount = 0;
            foreach (var pattern in _logFilesExtensions)
            {
                logCount += System.IO.Directory.GetFiles(path, "*.*").Where(file => file.ToLower().EndsWith(pattern)).Count();
            }
            return logCount;
        }

        /// <summary>
        /// Counts the images in a given directory
        /// </summary>
        /// <param name="path">directory to count images in</param>
        /// <returns>image count</returns>
        public int CountImages(string path)
        {
            return System.IO.Directory.GetFiles(path, "*.*").Where(file => file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("jpeg")).Count();
        }

        /// <summary>
        /// Opens an Explorer window that shows the working directory
        /// </summary>
        public void OpenDataPath()
        {
            // Navigate to a URL.
            System.Diagnostics.Process.Start(_dataPath);
        }

        /// <summary>
        /// Copies a file to a folder, creates folder if it doesn't exist. Folder name is based on test flight data. If logs are
        /// uploaded, the appropriate testflight data is updated
        /// </summary>
        /// <param name="flight"></param>
        /// <param name="fileNames"></param>
        /// <param name="workingPath"></param>
        /// <returns></returns>
        public void TransferFile(TestFlight flight, string filePath, string workingPath)
        {
            // create a path from the working directory and the flight name and data
            var fileStoragePath = System.IO.Path.Combine(_dataPath, workingPath);

            // verify if the directory exists
            if (!System.IO.Directory.Exists(fileStoragePath))
            {
                // create it if it doens't exist
                System.IO.Directory.CreateDirectory(fileStoragePath);
            }

            // get the filename of the file
            var fileNameWithoutPath = System.IO.Path.GetFileName(filePath);

            // Parse logs to update test flight with proper wing ID
            // the file that ends with "_ACEeprom.text" contains Wing ID
            if (fileNameWithoutPath.EndsWith("_ACEeprom.text"))
            {
                // get flight data from log
                var type = File.ReadAllLines(filePath).Where(line => line.Contains("acType")).FirstOrDefault().Substring(7).PadLeft(4, '0');
                flight.WingId = type + File.ReadAllLines(filePath).Where(line => line.Contains("acSerNr")).FirstOrDefault().Substring(8).PadLeft(6, '0');
            }

            // Parse logs to update test flight with proper ebox ID
            // the file that ends with "_eBoxEeprom.text" contains ebox ID
            if (fileNameWithoutPath.EndsWith("_eBoxEeprom.text"))
            {
                // get flight data from log
                flight.EboxId = "EBX" + File.ReadAllLines(filePath).Where(line => line.Contains("eBoxSerNr")).FirstOrDefault().Substring(10).PadLeft(5, '0');
            }

            // Parse logs to update test flight data with Elevator, Aileron and ShutterCommands values
            // the file that ends with "_prodlog.text" contains Elevator, Aileron, Samples and Shutter Commands
            if (fileNameWithoutPath.EndsWith("_prodlog.text"))
            {
                // get flight data from log
                var flightData = File.ReadAllLines(filePath)[0].Split(' ');

                flight.Elevator = Convert.ToInt32(flightData[0]);
                flight.Aileron = Convert.ToInt32(flightData[1]);
                flight.Samples = Convert.ToInt32(flightData[2]);
                flight.ShutterCommands = Convert.ToInt32(flightData[3]);
            }

            // Parse logs to update test flight data with undershoot
            if (fileNameWithoutPath.EndsWith(".txt"))
            {
                var flightData = File.ReadAllLines(filePath).Where(line => line != string.Empty).ToArray();

                for(var i = 0; i < flightData.Count(); i++)
                {
                    var speed = Convert.ToInt32(flightData[i].Split(' ')[26]);
                    var height = Convert.ToInt32(flightData[i].Split(' ')[32]);
                    var status = Convert.ToInt32(flightData[i].Split(' ')[22]);
                    var deltaZ = Convert.ToInt32(flightData[i].Split(' ')[33]);

                    // change these values to fine tune
                    if (speed < 40 && height < 2 && status == 157 && deltaZ > -5 && deltaZ < 5)
                    {

                        // determine actual landing location
                        var latitude = Double.Parse(flightData[i].Split(' ')[25], System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
                        var longitude = Double.Parse(flightData[i].Split(' ')[24], System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
                        var actualLandingLoc = new GeoCoordinate(latitude, longitude);

                        // determine a reference position a little earlier in the AC's current path, this will be used to differentiate between under and overshoot
                        latitude = Double.Parse(flightData[i - 5].Split(' ')[25], System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
                        longitude = Double.Parse(flightData[i - 5].Split(' ')[24], System.Globalization.NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"));
                        var referenceLoc = new GeoCoordinate(latitude, longitude);

                        // calculate distance from actual landing to programmed landing
                        var distance = GetDistanceBetween(_programmedLandingLocation.Latitude, _programmedLandingLocation.Longitude, actualLandingLoc.Latitude, actualLandingLoc.Longitude);

                        // calculate if its an overshoot or an undershoot:
                        // if the distance between reference and programmed is smaller than the distance between actual and programmed then it's an overshoot
                        var distanceBetweenReferenceAndProgrammed = GetDistanceBetween(referenceLoc, _programmedLandingLocation);
                        if (distanceBetweenReferenceAndProgrammed < distance)
                            distance = distance * -1;

                        // assign the calculated undershoot to the flight property
                        flight.Undershoot = Convert.ToInt32(Math.Round(distance, 0));

                        break;
                    }
                }
            }
            
            if(fileNameWithoutPath.EndsWith("T04"))
            {
                // run the T04 file parser app
                var commandLineHelper = new CommandLineHelper();
                commandLineHelper.Run("T01App.exe", filePath);

                // retrieve event number from result
                var record19Value = Convert.ToInt32(commandLineHelper.Output
                    .Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                    .Where(line => line.IndexOf("Record  19 :") > -1)
                    .FirstOrDefault()
                    .Substring(12));

                // store the retrieved number of events in the testflight object
                flight.GBoxEvents = record19Value;
            }

            // Create a directory name
            var newFileName = System.IO.Path.Combine(fileStoragePath, fileNameWithoutPath);

            // Copy file
            using (FileStream SourceStream = File.Open(filePath, FileMode.Open))
                using (FileStream DestinationStream = File.Create(newFileName))
                    SourceStream.CopyTo(DestinationStream);

            // delete T04 file
            if (fileNameWithoutPath.EndsWith("T04"))
                File.Delete(filePath);

            // if _doDeleteAfterCopy is true, clean up images after copying
            if (_doDeleteAfterCopy && (fileNameWithoutPath.ToLower().EndsWith("jpg") || fileNameWithoutPath.ToLower().EndsWith("jpeg")))
                File.Delete(filePath);            
        }
        
        /// <summary>
        /// Renames the directory where a flight stores its files
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="flight"></param>
        public void RenameFlightDirectory(string oldPath, TestFlight flight)
        {
            oldPath = Path.Combine(_dataPath, oldPath);
            var newPath = Path.Combine(_dataPath, flight.TempFolderName);

            if (oldPath != newPath)
                Directory.Move(oldPath, newPath);
        }
                
        /// <summary>
        /// Creates the working directory and _flightDataTextFileName if needed.
        /// </summary>
        private void CreateTestFlightDataDir()
        {
            if (!System.IO.Directory.Exists(_dataPath))
                System.IO.Directory.CreateDirectory(_dataPath);
        }

        /// <summary>
        /// Persists a flight
        /// </summary>
        /// <param name="testFlight"></param>
        public void SaveTestFlight(TestFlight testFlight)
        {
            _testFlightRepository.Save(testFlight);
        }

        /// <summary>
        /// Retrieves all testflights
        /// </summary>
        /// <returns></returns>
        public IList<TestFlight> GetAllTestFlights()
        {
            return _testFlightRepository.GetAll().OrderBy(flight => flight.TimeStamp).ToList();
        }

        /// <summary>
        /// Changes a directory (in effect this moves files around)
        /// </summary>
        /// <param name="oldDirName"></param>
        /// <param name="newDirName"></param>
        public void ChangeDirName(string oldDirName, string newDirName)
        {
            var oldDir = Path.Combine(_dataPath, oldDirName);
            var newDir = Path.Combine(_dataPath, newDirName);

            if (System.IO.Directory.Exists(oldDir))
                System.IO.Directory.Move(oldDir, newDir);
        }

        #region GPS calculations
        /// <summary>
        /// his routine calculates the distance between two points (given the latitude/longitude of those points). 
        /// 
        /// Definitions:                                                    
        ///       South latitudes are negative, east longitudes are positive 
        /// </summary>
        /// <param name="coord1"></param>
        /// <param name="coord2"></param>
        /// <returns></returns>
        public double GetDistanceBetween(GeoCoordinate coord1, GeoCoordinate coord2)
        {
            return GetDistanceBetween(coord1.Latitude, coord1.Longitude, coord2.Latitude, coord2.Longitude);
        }

        /// <summary>
        /// This routine calculates the distance between two points (given the latitude/longitude of those points). 
        /// 
        /// Definitions:                                                    
        ///       South latitudes are negative, east longitudes are positive 
        /// </summary>
        /// <param name="lat1">Latitude of point 1 (in decimal degrees)</param>
        /// <param name="lon1">Longitude of point 1 (in decimal degrees)</param>
        /// <param name="lat2">Latitude of point 2 (in decimal degrees)</param>
        /// <param name="lon2">Longitude of point 2 (in decimal degrees)</param>
        /// <returns></returns>
        public double GetDistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(DegreesToRadians(lat1)) * Math.Sin(DegreesToRadians(lat2)) + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) * Math.Cos(DegreesToRadians(theta));

            dist = Math.Acos(dist);
            dist = RadiansToDegrees(dist);
            dist = dist * 60 * 1.1515;

            return (dist * 1.609344) * 1000;
        }

        /// <summary>
        /// This function converts decimal degrees to radians 
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        private double DegreesToRadians(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        /// <summary>
        /// This function converts radians to decimal degrees 
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private double RadiansToDegrees(double rad)
        {
            return (rad / Math.PI * 180.0);
        }
        #endregion
    }
}
