using System;
using System.Linq;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using tik4net;

namespace CustomerAndServerMaintenanceTracking
{
    public class MikrotikClientManager
    {
        private static MikrotikClientManager _instance;
        public static MikrotikClientManager Instance => _instance ?? (_instance = new MikrotikClientManager());

        private ITikConnection connection;
        private DateTime lastActivity;
        private int winboxTimeoutMilliseconds = 600000; // e.g., 10 mins

        public void Connect()
        {
            // If already open, do nothing
            if (connection != null && connection.IsOpened) return;

            // Retrieve router info from DB or config:
            var router = new MikrotikRouterRepository().GetRouters().FirstOrDefault();
            if (router == null)
                throw new Exception("No Mikrotik router found.");

            // Create connection (API or API-SSL)
            connection = ConnectionFactory.CreateConnection(TikConnectionType.Api);
            connection.Open(router.HostIPAddress, router.ApiPort, router.Username, router.Password);


            if (!connection.IsOpened)
                throw new Exception("Failed to connect to Mikrotik.");

            lastActivity = DateTime.Now;
        }

        public void Disconnect()
        {
            if (connection != null && connection.IsOpened)
            {
                connection.Close();
                connection = null;
            }
        }

        public void CheckAndReconnect()
        {
            if (connection == null || !connection.IsOpened ||
                (DateTime.Now - lastActivity).TotalMilliseconds > winboxTimeoutMilliseconds)
            {
                Disconnect();
                Connect();
            }
        }

        public ITikConnection GetConnection()
        {
            CheckAndReconnect();
            return connection;
        }

        public void UpdateLastActivity()
        {
            lastActivity = DateTime.Now;
        }
    }
}
