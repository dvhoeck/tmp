using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class TestFlightMap : ClassMap<TestFlight>
    {
        public TestFlightMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.WingId);
            Map(x => x.EboxId);
            Map(x => x.FlightNumber);
            Map(x => x.Samples);
            Map(x => x.Elevator);
            Map(x => x.Aileron);
            Map(x => x.ShutterCommands);
            Map(x => x.EBoxEvents);
            Map(x => x.GBoxEvents);
            Map(x => x.Undershoot);
            Map(x => x.TimeStamp);
            Map(x => x.AmountOfImagesUploaded);
            Map(x => x.AmountOfLogsUploaded);
            Map(x => x.Damaged);
            Map(x => x.WindDirection);
            Map(x => x.WindSpeed);
            Map(x => x.WeatherCondition);            
            Map(x => x.LauncherSerial);
            Map(x => x.Pilot);
            Map(x => x.PilotApproval);
            Map(x => x.LogsDownloadOk);
            Map(x => x.TestPictures);
            Map(x => x.Crash);
            Map(x => x.MinMaxElevator);
            Map(x => x.MinMaxAileron);
            Map(x => x.Comment);
        }
    }
}
