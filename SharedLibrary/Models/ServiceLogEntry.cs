using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class ServiceLogEntry
    {
        public int LogId { get; set; }
        public DateTime LogTimestamp { get; set; }
        public string ServiceName { get; set; }
        public string LogLevel { get; set; } // Could be an enum: Info, Warning, Error, Debug, Fatal
        public string Message { get; set; }
        public string ExceptionDetails { get; set; }

        public ServiceLogEntry()
        {
            LogTimestamp = DateTime.Now; // Default to current time
        }
    }

    // Optional: Define LogLevel enum if you prefer strong typing
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL
    }
}
