using System.Collections.Generic;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// 
    /// </summary>
    public class DeviceManipulatedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceManipulatedEventArgs" /> class.
        /// </summary>
        /// <param name="deviceId">Name of the device.</param>
        /// <param name="action">The action.</param>
        /// <param name="deviceType">Type of the device.</param>
        /// <param name="parameters">The parameters.</param>
        public DeviceManipulatedEventArgs(string deviceId, int action, int deviceType, Dictionary<string, object> parameters)
        {
            DeviceId = deviceId;
            DeviceType = deviceType;
            Action = action;
            Parameters = parameters;
        }

        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        /// <value>
        /// The name of the device.
        /// </value>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public int Action { get; private set; }

        /// <summary>
        /// Gets the device type.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public int DeviceType { get; private set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public Dictionary<string, object> Parameters { get; set; }  
    }
}
