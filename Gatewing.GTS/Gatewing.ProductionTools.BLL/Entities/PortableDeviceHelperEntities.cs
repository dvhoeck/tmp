using Gatewing.ProductionTools.Logging;
using PortableDeviceApiLib;
using PortableDeviceTypesLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    ///
    /// </summary>
    public class PortableDevice
    {
        #region Fields

        private readonly PortableDeviceManager _deviceManager = new PortableDeviceManager();
        private bool _isConnected;
        private readonly PortableDeviceClass _device;
        private int _comportNr;
        private Logger _logger;

        #endregion Fields

        #region ctor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableDevice" /> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="comportNr">The comport nr.</param>
        public PortableDevice(string deviceId, int comportNr)
        {
            this._device = new PortableDeviceClass();
            this.DeviceId = deviceId;
            this._comportNr = comportNr;
        }

        public PortableDevice(string deviceId, int comportNr, Logger _logger) : this(deviceId, comportNr)
        {
            this._logger = _logger;
        }

        #endregion ctor(s)

        #region Properties

        public string DeviceId { get; set; }

        /// <summary>
        /// Gets the name of the product type.
        /// </summary>
        /// <value>
        /// The name of the product type.
        /// </value>
        public string ProductTypeName { get { return GetPropertyValueAsString(8); } }

        /// <summary>
        /// Gets the comport nr.
        /// </summary>
        /// <value>
        /// The comport nr.
        /// </value>
        public int ComportNr { get { return _comportNr; } }

        /// <summary>
        /// Gets the internal serial.
        /// </summary>
        /// <value>
        /// The internal serial.
        /// </value>
        public string InternalSerial
        {
            get
            {
                return GetPropertyValueAsString(9);
            }
        }

        /// <summary>
        /// Gets the friendly name of the device
        /// </summary>
        /// <value>
        /// The friendly name of the device
        /// </value>
        public string FriendlyName
        {
            get
            {
                // use this to check props
                for (var i = 0u; i < 32; i++)
                {
                    try
                    {
                        Console.WriteLine(GetPropertyValueAsString(i));
                    }
                    catch
                    {
                    }
                }
                return GetPropertyValueAsString(7);
            }
        }

        /// <summary>
        /// Determines whether data logging is enabled, this assumes the connected device is a Trimble gBox
        /// </summary>
        /// <returns></returns>
        public bool IsDataLoggingEnabled
        {
            get
            {
                _logger.LogDebug("Attempting to check if T0(4/C) is created.");
                bool result;
                CheckT04Creation(this.GetContents(), out result);

                return result;
            }
        }

        public string[] DownloadMostRecentFileForDate(DateTime date, string destinationFolder)
        {
            try
            {
                _logger.LogDebug("Attempting to download most recent file for date: {0}, for device with internal serial {1}", date, InternalSerial);
                //var date = DateTime.Now;
                var root = this.GetContents();

                _logger.LogDebug("Nr of files in device root: " + root.Files.Count);

                var errorMsg = "Could not find a folder for the date " + date.ToShortDateString() + " on the gBox.";

                var yearFolder = ((PortableDeviceFolder)root.Files[0]).Files.Where(file => file.Name == date.Year.ToString()).FirstOrDefault();

                if (yearFolder == null)
                    throw new InvalidOperationException(errorMsg);

                _logger.LogDebug("Found folder corresponding to current year: " + yearFolder);

                if (yearFolder != null)
                {
                    var monthFolder = ((PortableDeviceFolder)yearFolder).Files.Where(file => file.Name == date.Month.ToString().PadLeft(2, '0')).FirstOrDefault();
                    if (monthFolder == null)
                        throw new InvalidOperationException(errorMsg);
                    else
                    {
                        _logger.LogDebug("Found folder corresponding to current month: " + monthFolder);

                        var dayFolder = ((PortableDeviceFolder)monthFolder).Files.Where(file => file.Name == date.Day.ToString().PadLeft(2, '0')).FirstOrDefault();
                        if (dayFolder == null)
                            throw new InvalidOperationException(errorMsg);

                        _logger.LogDebug("Found folder corresponding to current day: " + dayFolder);

                        var fileNames = new string[2];
                        for (var i = 0; i < 2; i++)
                        {
                            var selectedFile = ((PortableDeviceFolder)dayFolder).Files.OrderByDescending(file => file.Name).ToList()[i];
                            fileNames[i] = selectedFile.Name.ToString();

                            _logger.LogDebug("Attempting to download file {0} to folder {1}", selectedFile, destinationFolder);

                            DownloadFile((PortableDeviceFile)selectedFile, destinationFolder);
                        }

                        return fileNames;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return null;
        }

        #endregion Properties

        #region Methods

        public void DownloadFile(PortableDeviceFile file, string saveToPath)
        {
            IPortableDeviceContent content;
            this._device.Content(out content);

            IPortableDeviceResources resources;
            content.Transfer(out resources);

            PortableDeviceApiLib.IStream wpdStream;
            uint optimalTransferSize = 0;

            var property = new PortableDeviceApiLib._tagpropertykey();
            property.fmtid = new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42);
            property.pid = 0;

            resources.GetStream(file.Id, ref property, 0, ref optimalTransferSize, out wpdStream);

            System.Runtime.InteropServices.ComTypes.IStream sourceStream = (System.Runtime.InteropServices.ComTypes.IStream)wpdStream;

            var filename = Path.GetFileName(file.Name);

            _logger.LogDebug("Attempting to download file stream for file {0} to path {1}", filename, saveToPath);

            FileStream targetStream = new FileStream(Path.Combine(saveToPath, filename), FileMode.Create, FileAccess.Write);

            unsafe
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    sourceStream.Read(buffer, 1024, new IntPtr(&bytesRead));
                    targetStream.Write(buffer, 0, 1024);
                } while (bytesRead > 0);
                targetStream.Close();

                Marshal.ReleaseComObject(sourceStream);
                // Marshal.ReleaseComObject(targetStream);
            }
        }

        public void Connect()
        {
            if (this._isConnected) { return; }

            var clientInfo = (PortableDeviceApiLib.IPortableDeviceValues)new PortableDeviceValuesClass();
            this._device.Open(this.DeviceId, clientInfo);
            this._isConnected = true;
        }

        public void Disconnect()
        {
            if (!this._isConnected) { return; }
            this._device.Close();
            this._isConnected = false;
        }

        public PortableDeviceFolder GetContents()
        {
            var root = new PortableDeviceFolder("DEVICE", "DEVICE");

            IPortableDeviceContent content;
            this._device.Content(out content);
            EnumerateContents(ref content, root);

            return root;
        }

        /// <summary>
        /// Checks the T04 creation, this assumes the connected device is a Trimble gBox.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="result">if set to <c>true</c> [result].</param>
        private void CheckT04Creation(object item, out bool result)
        {
            result = false;
            if (item is PortableDeviceFolder)
            {
                foreach (var currentItem in ((PortableDeviceFolder)item).Files)
                    CheckT04Creation(currentItem, out result);
            }
            else if (item is PortableDeviceFile && ((PortableDeviceFile)item).Name.EndsWith("T04") || ((PortableDeviceFile)item).Name.EndsWith("T0C"))
            {
                result = true;
                return;
            }
        }

        /// <summary>
        /// Gets the property value as string.
        /// </summary>
        /// <param name="pid">The pid.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Not connected to device.</exception>
        private string GetPropertyValueAsString(uint pid)
        {
            if (!this._isConnected)
            {
                throw new InvalidOperationException("Not connected to device.");
            }

            // Retrieve the properties of the device
            IPortableDeviceContent content;
            IPortableDeviceProperties properties;
            this._device.Content(out content);
            content.Properties(out properties);

            // Retrieve the values for the properties
            PortableDeviceApiLib.IPortableDeviceValues propertyValues;
            properties.GetValues("DEVICE", null, out propertyValues);

            // Identify the property to retrieve
            var property = new PortableDeviceApiLib._tagpropertykey();
            property.fmtid = new Guid(0x26D4979A, 0xE643, 0x4626, 0x9E, 0x2B,
                                      0x73, 0x6D, 0xC0, 0xC9, 0x2F, 0xDC);

            // PiD's => 12 = friendlyName,
            property.pid = pid;

            // Retrieve the property
            string propertyValue;
            propertyValues.GetStringValue(ref property, out propertyValue);

            return propertyValue;
        }

        /// <summary>
        /// Enumerates the contents.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="parent">The parent.</param>
        private static void EnumerateContents(ref IPortableDeviceContent content,
            PortableDeviceFolder parent)
        {
            // Get the properties of the object
            IPortableDeviceProperties properties;
            content.Properties(out properties);

            // Enumerate the items contained by the current object
            IEnumPortableDeviceObjectIDs objectIds;
            content.EnumObjects(0, parent.Id, null, out objectIds);

            uint fetched = 0;
            do
            {
                string objectId;

                objectIds.Next(1, out objectId, ref fetched);
                if (fetched > 0)
                {
                    var currentObject = WrapObject(properties, objectId);

                    parent.Files.Add(currentObject);

                    if (currentObject is PortableDeviceFolder)
                    {
                        EnumerateContents(ref content, (PortableDeviceFolder)currentObject);
                    }
                }
            } while (fetched > 0);
        }

        /// <summary>
        /// Wraps the object.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        private static PortableDeviceObject WrapObject(IPortableDeviceProperties properties,
            string objectId)
        {
            PortableDeviceApiLib.IPortableDeviceKeyCollection keys;
            properties.GetSupportedProperties(objectId, out keys);

            PortableDeviceApiLib.IPortableDeviceValues values;
            properties.GetValues(objectId, keys, out values);

            // Get the name of the object
            string name;
            var property = new PortableDeviceApiLib._tagpropertykey();
            property.fmtid = new Guid(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC,
                                      0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C);
            property.pid = 4;
            values.GetStringValue(property, out name);

            // Get the type of the object
            Guid contentType;
            property = new PortableDeviceApiLib._tagpropertykey();
            property.fmtid = new Guid(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC,
                                      0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C);
            property.pid = 7;
            values.GetGuidValue(property, out contentType);

            var folderType = new Guid(0x27E2E392, 0xA111, 0x48E0, 0xAB, 0x0C,
                                      0xE1, 0x77, 0x05, 0xA0, 0x5F, 0x85);
            var functionalType = new Guid(0x99ED0160, 0x17FF, 0x4C44, 0x9D, 0x98,
                                          0x1D, 0x7A, 0x6F, 0x94, 0x19, 0x21);

            if (contentType == folderType || contentType == functionalType)
            {
                return new PortableDeviceFolder(objectId, name);
            }

            return new PortableDeviceFile(objectId, name);
        }

        #endregion Methods

        public override string ToString()
        {
            return string.Format("COM{0} - {1}", ComportNr, ProductTypeName);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="System.Collections.ObjectModel.Collection{Gatewing.ProductionTools.BLL.PortableDevice}" />
    public class PortableDeviceCollection : Collection<PortableDevice>
    {
        /// <summary>
        /// The _portable device collection
        /// </summary>
        private Dictionary<string, string> _serialPortList = new Dictionary<string, string>();

        /// <summary>
        /// The _device manager
        /// </summary>
        private readonly PortableDeviceManager _deviceManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableDeviceCollection"/> class.
        /// </summary>
        public PortableDeviceCollection(Logger logger)
        {
            _logger = logger;
            this._deviceManager = new PortableDeviceManager();
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            _logger.LogDebug("Attempting to refresh WPD collection");

            //
            // Get an instance of the device manager
            //
            PortableDeviceApiLib.PortableDeviceManagerClass devMgr
                = new PortableDeviceApiLib.PortableDeviceManagerClass();

            //
            // Probe for number of devices
            //
            uint cDevices = 1;
            devMgr.GetDevices(null, ref cDevices);

            //
            // Re-allocate if needed
            //
            if (cDevices > 0)
            {
                string[] deviceIDs = new string[cDevices];
                devMgr.GetDevices(deviceIDs, ref cDevices);

                for (int ndxDevices = 0; ndxDevices < cDevices; ndxDevices++)
                {
                    Console.WriteLine("Device[{0}]: {1}",
                            ndxDevices + 1, deviceIDs[ndxDevices]);
                }
            }
            else
            {
                Console.WriteLine("No WPD devices are present!");
            }
            // get all serial ports to get comport nr for devices
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSSerial_PortName");

                _serialPortList.Clear();
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    _logger.LogDebug("Found serial port: {0} - {1}", queryObj["InstanceName"].ToString(), queryObj["PortName"].ToString());
                    _serialPortList.Add(queryObj["InstanceName"].ToString(), queryObj["PortName"].ToString());
                }
            }
            catch (ManagementException e)
            {
                _logger.LogError(e.Message);
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }

            this._deviceManager.RefreshDeviceList();

            // Determine how many WPD devices are connected
            var deviceIds = new string[1];
            uint count = 1;
            this._deviceManager.GetDevices(null, ref count);

            if (count < 1)
                return;

            // Retrieve the device id for each connected device
            deviceIds = new string[count];
            this._deviceManager.GetDevices(deviceIds, ref count);
            foreach (var deviceId in deviceIds)
            {
                var splitId = deviceId.ToLower().Split('&');

                _logger.LogDebug("Attempting to get comport nr for device with hardware id: " + deviceId);

                var selectedDevice = _serialPortList.Where(port => port.Key.ToLower().Contains(splitId[1]) && port.Key.ToLower().Contains(splitId[3])).FirstOrDefault();
                var comportNr = 0;
                if (selectedDevice.Key != null)
                    comportNr = int.Parse(selectedDevice.Value.Substring(3));

                _logger.LogInfo("Found connected device: {0} - COM{1}", deviceId, comportNr);

                Add(new PortableDevice(deviceId, comportNr, _logger));
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="Gatewing.ProductionTools.BLL.PortableDeviceObject" />
    public class PortableDeviceFile : PortableDeviceObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortableDeviceFile"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public PortableDeviceFile(string id, string name) : base(id, name)
        {
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="Gatewing.ProductionTools.BLL.PortableDeviceObject" />
    public class PortableDeviceFolder : PortableDeviceObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortableDeviceFolder"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public PortableDeviceFolder(string id, string name) : base(id, name)
        {
            this.Files = new List<PortableDeviceObject>();
        }

        public IList<PortableDeviceObject> Files { get; set; }
    }

    public abstract class PortableDeviceObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortableDeviceObject"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        protected PortableDeviceObject(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }
    }
}