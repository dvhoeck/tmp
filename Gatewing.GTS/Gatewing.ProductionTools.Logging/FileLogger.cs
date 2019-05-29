using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.Logging
{
    public class Logger
    {
        private readonly ILog _log;
        public Logger(string name)
        {
            _log = log4net.LogManager.GetLogger(name);
            log4net.Config.XmlConfigurator.Configure();
        }

        public void LogInfo(string message, params object[] args)
        {
            LogInfo(string.Format(message, args));
        }

        public void LogInfo(string message)
        {
            _log.Info(message);
        }



        public void LogDebug(string message, params object[] args)
        {
            LogDebug(string.Format(message, args));
        }

        public void LogDebug(string message)
        {
            _log.Debug(message);
        }

        public void LogError(Exception e)
        {
            _log.Error("An error occured: ", e);
        }
        
        public void LogError(string message, params object[] args)
        {
            LogError(string.Format(message, args));
        }

        public void LogError(string message)
        {
            _log.Error(message);
        }
    }
}
