using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class NotificationHistoryLog
    {
        public int HistoryLogId { get; set; }
        public int RuleId { get; set; }
        public DateTime LogTimestamp { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string ExceptionDetails { get; set; }

        public NotificationHistoryLog()
        {
            LogTimestamp = DateTime.Now;
        }
    }
}
