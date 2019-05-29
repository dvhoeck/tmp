using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// A class to report task progress
    /// </summary>
    public class TaskReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskReport" /> class.
        /// </summary>
        /// <param name="taskMessage">The task message.</param>
        /// <param name="taskNumber">The task number.</param>
        /// <param name="value">The value.</param>
        public TaskReport(string taskMessage, int taskNumber, object value)
        {
            TaskNumber = taskNumber;
            TaskMessage = taskMessage;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the task number.
        /// </summary>
        /// <value>
        /// The task number.
        /// </value>
        public int TaskNumber { get; set; }

        /// <summary>
        /// Gets or sets the task number.
        /// </summary>
        /// <value>
        /// The task number.
        /// </value>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the task message.
        /// </summary>
        /// <value>
        /// The task message.
        /// </value>
        public string TaskMessage { get; set; }
    }
}