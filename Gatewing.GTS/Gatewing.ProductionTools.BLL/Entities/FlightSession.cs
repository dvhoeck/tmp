using System;

namespace Gatewing.ProductionTools.BLL
{
    public class FlightSession : DomainObject
    {
        public FlightSession()
        {
        }

        public FlightSession(DateTime timestamp)
        {
            Timestamp = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day);
        }

        public virtual DateTime Timestamp { get; protected set; }
        public virtual string TempFolderName { get { return Timestamp.ToString("yy-MM-dd"); } }

        public virtual int FinalizedFoldersPercentage { get; set; }
        public virtual int FinalizedMailPercentage { get; set; }
        public virtual int FinalizedSheetsPercentage { get; set; }
    }
}