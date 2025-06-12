using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NotificationService.Services;
using System.Timers;

namespace NotificationService
{
    public partial class NotificationServiceControl : ServiceBase
    {
        private NotificationProcessingManager _notificationManager;

        public NotificationServiceControl()
        {
            InitializeComponent();
            _notificationManager = new NotificationProcessingManager();
            this.ServiceName = "CSMTNotificationService";
        }

        protected override void OnStart(string[] args)
        {
            _notificationManager.Start();
        }

        protected override void OnStop()
        {
            _notificationManager.Stop();
        }
    }
}
