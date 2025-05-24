using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tik4net;
using PPPoESyncService.DataAccess;
using PPPoESyncService.Models;

namespace PPPoESyncService
{
    public class MikrotikClientManager
    {
        private static MikrotikClientManager _instance;
        public static MikrotikClientManager Instance => _instance ?? (_instance = new MikrotikClientManager());

        private ITikConnection _connection;
        private DateTime _lastActivity;
        // Timeout before forcing a reconnect if no activity (e.g., 10 minutes)
        // This could also be an App.config setting
        private readonly int _connectionTimeoutMilliseconds = 600000;
        private readonly object _lock = new object(); // For thread-safe connect/disconnect

        // Private constructor for Singleton pattern
        private MikrotikClientManager() { }

        public void Connect()
        {
            lock (_lock)
            {
                // If already open, do nothing
                if (_connection != null && _connection.IsOpened)
                {
                    // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Connection already open.");
                    return;
                }

                // Retrieve router info from DB 
                // Ensure MikrotikRouterRepository is available and configured
                MikrotikRouterRepository routerRepo = new MikrotikRouterRepository();
                var router = routerRepo.GetRouters().FirstOrDefault();

                if (router == null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM ERROR: No Mikrotik router configuration found in the database.");
                    throw new InvalidOperationException("No Mikrotik router configuration found in the database. Please configure a router in the UI application's settings.");
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Attempting to connect to Mikrotik router '{router.RouterName}' at {router.HostIPAddress}:{router.ApiPort}...");

                try
                {
                    // Create connection (API or API-SSL). Api is typically port 8728, ApiSsl 8729.
                    // The ConnectionFactory determines this based on TikConnectionType.
                    _connection = ConnectionFactory.CreateConnection(TikConnectionType.Api); // Use .Api for non-SSL, .ApiSsl for SSL
                    _connection.Open(router.HostIPAddress, router.ApiPort, router.Username, router.Password);

                    if (!_connection.IsOpened)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM ERROR: Failed to connect to Mikrotik router '{router.RouterName}'. Connection state: Not Opened.");
                        throw new Exception($"Failed to connect to Mikrotik router '{router.RouterName}'.");
                    }

                    _lastActivity = DateTime.Now;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Successfully connected to Mikrotik router '{router.RouterName}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM ERROR: Exception during Mikrotik connection to '{router.RouterName}': {ex.Message}");
                    _connection?.Dispose(); // Ensure connection is disposed if Open fails
                    _connection = null;
                    throw; // Re-throw to indicate connection failure
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    if (_connection.IsOpened)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Closing Mikrotik connection.");
                        _connection.Close();
                    }
                    _connection.Dispose(); // Dispose the connection object
                    _connection = null;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Mikrotik connection disposed.");
                }
            }
        }

        private void CheckAndReconnect()
        {
            lock (_lock)
            {
                bool needsReconnect = false;
                if (_connection == null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Connection is null. Reconnecting.");
                    needsReconnect = true;
                }
                else if (!_connection.IsOpened)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Connection is not open. Reconnecting.");
                    needsReconnect = true;
                }
                else if ((DateTime.Now - _lastActivity).TotalMilliseconds > _connectionTimeoutMilliseconds)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Connection timed out due to inactivity. Reconnecting.");
                    needsReconnect = true;
                }

                if (needsReconnect)
                {
                    Disconnect(); // Ensure clean state before attempting to connect
                    Connect();    // This will throw if it fails, handled by calling method
                }
            }
        }

        public ITikConnection GetConnection()
        {
            // The lock in CheckAndReconnect handles thread safety for connect/disconnect logic.
            CheckAndReconnect();
            // It's possible _connection is still null if Connect() failed and threw an exception.
            // The calling code should handle potential exceptions from Connect().
            if (_connection == null || !_connection.IsOpened)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM ERROR: GetConnection called, but connection is not established or null after CheckAndReconnect.");
                throw new InvalidOperationException("Mikrotik connection is not established. Check service logs for connection errors.");
            }
            return _connection;
        }

        public void UpdateLastActivity()
        {
            _lastActivity = DateTime.Now;
            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MCM: Last activity updated.");
        }
    }
}
