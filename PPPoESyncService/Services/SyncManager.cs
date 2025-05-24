using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.DataAccess;
using SharedLibrary.Models;
using tik4net;
using tik4net.Objects.Ppp; // For PppSecret
using System.Diagnostics;
using tik4net.Objects;

namespace PPPoESyncService.Services
{

    public class SyncManager
    {
        private readonly CustomerRepository _customerRepository;
        private readonly ServiceLogRepository _logRepository;
        private readonly MikrotikRouterRepository _routerRepository;
        private readonly string _serviceNameForLogging;
        private readonly NetwatchConfigRepository _netwatchConfigRepository;

        public SyncManager(ServiceLogRepository logRepository, string serviceName)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _serviceNameForLogging = serviceName ?? throw new ArgumentNullException(nameof(serviceName));

            // Initialize repositories here
            _customerRepository = new CustomerRepository();
            _routerRepository = new MikrotikRouterRepository();
            _netwatchConfigRepository = new NetwatchConfigRepository(_logRepository, new TagRepository());

            Log(LogLevel.DEBUG, "SyncManager Constructor: Initializing repositories (this log is now from the base of constructor).");
            // The try-catch block for repository initialization might be redundant if already done above,
            // but keeping it doesn't harm.
            try
            {
                // These are already initialized above.
                // _customerRepository = new CustomerRepository();
                // _routerRepository = new MikrotikRouterRepository();
                Log(LogLevel.DEBUG, "SyncManager Constructor: Repositories initialization confirmed.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, "SyncManager Constructor: CRITICAL ERROR during secondary repository initialization check.", ex);
                throw;
            }
        }

        private void Log(LogLevel level, string message, Exception ex = null, int? routerId = null)
        {
            string routerContext = routerId.HasValue ? $"[RouterID: {routerId}] " : "";
            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_serviceNameForLogging}] [{level}] SM: {routerContext}{message}{(ex != null ? " - Exception: " + ex.Message : "")}"); // Your original console log
            _logRepository.WriteLog(new ServiceLogEntry
            {
                ServiceName = _serviceNameForLogging,
                LogLevel = level.ToString(),
                Message = $"SM: {routerContext}{message}", // Ensure SM: prefix is intended for all SyncManager logs
                ExceptionDetails = ex?.ToString(),
                LogTimestamp = DateTime.Now // Added for explicitness
            });
        }

        // Your GetPPPActiveConnections method (structure retained, example ITikReSentence usage for clarity)
        // Ensure this matches how you get IpAddress and MacAddress from active connections
        private Dictionary<string, (string IpAddress, string MacAddress)> GetPPPActiveConnections(ITikConnection connection, int routerIdForLog)
        {
            var activeConnectionsMap = new Dictionary<string, (string IpAddress, string MacAddress)>();
            Log(LogLevel.DEBUG, "GetPPPActiveConnections: Attempting to retrieve PPP active connections...", routerId: routerIdForLog);
            IEnumerable<ITikReSentence> responseSentences = Enumerable.Empty<ITikReSentence>();

            try
            {
                if (!connection.IsOpened)
                {
                    Log(LogLevel.ERROR, "GetPPPActiveConnections: Connection is not open.", routerId: routerIdForLog);
                    return activeConnectionsMap;
                }

                // MODIFICATION: Try simpler command without .proplist and without-paging
                Log(LogLevel.DEBUG, "GetPPPActiveConnections: Using simplified command: /ppp/active/print", routerId: routerIdForLog);
                var cmd = connection.CreateCommand("/ppp/active/print");
                // Original command for reference:
                // var cmd = connection.CreateCommand("/ppp/active/print",
                //                                    connection.CreateParameter(".proplist", "name,address,caller-id"),
                //                                    connection.CreateParameter("without-paging", ""));

                try
                {
                    responseSentences = cmd.ExecuteList();
                    Log(LogLevel.DEBUG, $"GetPPPActiveConnections: Retrieved {responseSentences.Count()} raw active PPP entries with simplified command.", routerId: routerIdForLog);
                }
                catch (Exception exList)
                {
                    // Specifically check if it's the '!empty' issue
                    if (exList is NotImplementedException && exList.Message.Contains("!empty"))
                    {
                        Log(LogLevel.INFO, $"GetPPPActiveConnections: '/ppp/active/print' returned '!empty' (no active users or RouterOS reported no data). Proceeding with empty active list.", exList, routerId: routerIdForLog);
                    }
                    else
                    {
                        Log(LogLevel.WARN, $"GetPPPActiveConnections: Could not retrieve active PPP connections (API error: {exList.Message}). Proceeding with empty active list.", exList, routerId: routerIdForLog);
                    }
                    return activeConnectionsMap; // Return empty map on error/!empty
                }

                foreach (var item in responseSentences) // item is ITikReSentence
                {
                    string pppName = item.GetWordValueOrDefault("name");
                    string remoteIP = item.GetWordValueOrDefault("address");
                    string callerId = item.GetWordValueOrDefault("caller-id");
                    string serviceType = item.GetWordValueOrDefault("service");

                    Log(LogLevel.DEBUG, $"GetPPPActiveConnections: Raw Active Detail - Name='{pppName}', IP='{remoteIP}', CallerID(MAC)='{callerId}', Service='{serviceType}'", routerId: routerIdForLog);

                    if (!string.IsNullOrEmpty(pppName) && serviceType == "pppoe")
                    {
                        if (!activeConnectionsMap.ContainsKey(pppName))
                        {
                            activeConnectionsMap[pppName] = (remoteIP, callerId);
                        }
                        else
                        {
                            Log(LogLevel.WARN, $"GetPPPActiveConnections: Duplicate active PPP user name '{pppName}' found. Using first entry.", routerId: routerIdForLog);
                        }
                    }
                }
                Log(LogLevel.INFO, $"GetPPPActiveConnections: Successfully processed and mapped {activeConnectionsMap.Count} active PPPoE connections.", routerId: routerIdForLog);
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "GetPPPActiveConnections: Unhandled error in GetPPPActiveConnections (outer catch).", ex, routerId: routerIdForLog);
            }
            return activeConnectionsMap;
        }


        public void SyncCustomers()
        {
            Log(LogLevel.INFO, "SyncCustomers: Method Entered. Starting multi-router customer synchronization cycle...");

            List<MikrotikRouter> allRouters;
            try
            {
                Log(LogLevel.DEBUG, "SyncCustomers: Attempting to get routers from repository...");
                allRouters = _routerRepository.GetRouters();
                Log(LogLevel.DEBUG, $"SyncCustomers: _routerRepository.GetRouters() returned {(allRouters == null ? "null" : $"{allRouters.Count} routers")}.");

                if (allRouters == null || !allRouters.Any())
                {
                    Log(LogLevel.WARN, "SyncCustomers: No Mikrotik routers configured in the database or repository returned null/empty. Ending sync cycle.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.FATAL, "SyncCustomers: Could not retrieve router list from database. Ending sync cycle.", ex);
                return;
            }

            Log(LogLevel.INFO, $"SyncCustomers: Found {allRouters.Count} routers to process.");
            var allSyncedMikrotikSecretIdsThisCycle = new HashSet<(int RouterId, string MikrotikSecretId)>();

            foreach (var router in allRouters)
            {
                Log(LogLevel.INFO, $"Processing router: {router.RouterName} (ID: {router.Id}, Host: {router.HostIPAddress})");
                ITikConnection connection = null;
                IEnumerable<PppSecret> pppSecretObjects = Enumerable.Empty<PppSecret>();

                try
                {
                    connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, router.HostIPAddress, router.ApiPort, router.Username, router.Password);
                    Log(LogLevel.INFO, "Successfully connected and logged in.", routerId: router.Id);

                    Log(LogLevel.INFO, "Loading PPP secrets from Mikrotik using LoadAll<PppSecret>()...", routerId: router.Id);
                    try
                    {
                        pppSecretObjects = connection.LoadAll<PppSecret>(); // PppSecret is from tik4net.Objects.Ppp
                        Log(LogLevel.INFO, $"Loaded {pppSecretObjects.Count()} PPP secret objects using LoadAll<PppSecret>().", routerId: router.Id);
                    }
                    catch (Exception exLoadAll)
                    {
                        Log(LogLevel.ERROR, "Error using connection.LoadAll<PppSecret>(). This could be due to permissions or no secrets. Details: " + exLoadAll.Message, exLoadAll, routerId: router.Id);
                        // Optionally continue to the next router if secrets can't be loaded for this one
                        // continue; 
                    }

                    var activeConnections = GetPPPActiveConnections(connection, router.Id); // Your method
                    // Log already exists in GetPPPActiveConnections for count

                    int processedSecretCount = 0;
                    if (pppSecretObjects.Any())
                    {
                        foreach (var secret in pppSecretObjects) // 'secret' is a tik4net.Objects.Ppp.PppSecret object
                        {
                            // --- Existing direct property access from your PppSecret object ---
                            string mikrotikId_raw = secret.Id;
                            string secretName_raw = secret.Name;
                            string remoteAddress_raw = secret.RemoteAddress; // IP from secret (often a pool or static)
                            bool isDisabled = secret.Disabled;

                            // --- ADDED LOGGING (1) ---
                            Log(LogLevel.DEBUG, $"Processing Secret: Name='{secretName_raw}', .id='{mikrotikId_raw}', RemoteAddress(fromSecret)='{remoteAddress_raw ?? "NULL"}', Disabled='{isDisabled}'", routerId: router.Id);

                            if (string.IsNullOrEmpty(secretName_raw))
                            {
                                Log(LogLevel.WARN, $"Skipping secret with no name (Mikrotik .id: {mikrotikId_raw}).", routerId: router.Id);
                                continue;
                            }

                            string finalIpAddress = remoteAddress_raw; // Default to IP from secret
                            string finalMacAddress = null; // MAC is primarily from active connections

                            if (activeConnections.TryGetValue(secretName_raw, out var activeDetail)) // activeDetail is (string IpAddress, string MacAddress)
                            {
                                // --- ADDED LOGGING (2) ---
                                Log(LogLevel.INFO, $"Active connection FOUND for '{secretName_raw}'. ActiveIP='{activeDetail.IpAddress ?? "NULL"}', ActiveMAC='{activeDetail.MacAddress ?? "NULL"}'", routerId: router.Id);

                                if (!string.IsNullOrEmpty(activeDetail.IpAddress))
                                {
                                    finalIpAddress = activeDetail.IpAddress;
                                    // --- ADDED LOGGING (3) ---
                                    Log(LogLevel.DEBUG, $"For '{secretName_raw}': Using IP from ACTIVE connection: '{finalIpAddress}'", routerId: router.Id);
                                }
                                else
                                {
                                    // --- ADDED LOGGING (4) ---
                                    Log(LogLevel.DEBUG, $"For '{secretName_raw}': Active connection IP is NULL/EMPTY. IP remains: '{finalIpAddress ?? "NULL"}' (from secret or null).", routerId: router.Id);
                                }

                                if (!string.IsNullOrEmpty(activeDetail.MacAddress))
                                {
                                    finalMacAddress = activeDetail.MacAddress;
                                    // --- ADDED LOGGING (5) ---
                                    Log(LogLevel.DEBUG, $"For '{secretName_raw}': Using MAC from ACTIVE connection: '{finalMacAddress}'", routerId: router.Id);
                                }
                                else
                                {
                                    // --- ADDED LOGGING (6) ---
                                    Log(LogLevel.DEBUG, $"For '{secretName_raw}': Active connection MAC is NULL/EMPTY. MAC remains NULL.", routerId: router.Id);
                                }
                            }
                            else
                            {
                                // --- ADDED LOGGING (7) ---
                                Log(LogLevel.INFO, $"NO active connection found for '{secretName_raw}'. IP will be '{finalIpAddress ?? "NULL"}' (from secret), MAC will be NULL.", routerId: router.Id);
                            }

                            Customer cust = new Customer
                            {
                                AccountName = secretName_raw,
                                IPAddress = string.IsNullOrEmpty(finalIpAddress) ? null : finalIpAddress,
                                MacAddress = string.IsNullOrEmpty(finalMacAddress) ? null : finalMacAddress,
                                MikrotikSecretId = mikrotikId_raw,
                                RouterId = router.Id,
                                IsArchived = isDisabled // Initially set from secret.Disabled
                            };

                            // Override IsArchived if the user has an active connection
                            if (activeConnections.ContainsKey(secretName_raw))
                            {
                                if (cust.IsArchived) // If secret.Disabled was true
                                {
                                    // --- ADDED LOGGING (8) ---
                                    Log(LogLevel.INFO, $"User '{secretName_raw}' has an active connection. Overriding IsArchived from True (due to secret.Disabled) to False.", routerId: router.Id);
                                }
                                cust.IsArchived = false; // Active user is not archived
                            }

                            // --- ADDED LOGGING (9) ---
                            Log(LogLevel.DEBUG, $"Customer object state before save: Account='{cust.AccountName}', IP='{cust.IPAddress ?? "NULL"}', MAC='{cust.MacAddress ?? "NULL"}', MkID='{cust.MikrotikSecretId}', RouterID='{cust.RouterId}', IsArchived='{cust.IsArchived}'", routerId: router.Id);

                            try
                            {
                                _customerRepository.InsertOrUpdateCustomer(cust);
                                if (!string.IsNullOrEmpty(mikrotikId_raw))
                                {
                                    allSyncedMikrotikSecretIdsThisCycle.Add((router.Id, mikrotikId_raw));
                                }
                            }
                            catch (Exception exRepo)
                            {
                                Log(LogLevel.ERROR, $"SyncCustomers: Failed to InsertOrUpdateCustomer '{cust.AccountName}'.", exRepo, router.Id);
                            }
                            processedSecretCount++;
                        }
                    }
                    Log(LogLevel.INFO, $"SyncCustomers: Finished processing {processedSecretCount} secrets for router {router.RouterName}.", routerId: router.Id);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.ERROR, $"SyncCustomers: General failure during processing for router {router.RouterName} (ID: {router.Id}).", ex, router.Id);
                }
                finally
                {
                    if (connection != null && connection.IsOpened)
                    {
                        connection.Close();
                        Log(LogLevel.DEBUG, $"SyncCustomers: Connection closed for router {router.RouterName}.", routerId: router.Id);
                    }
                }
            } // End of router loop

            // ... (Your existing Reconciliation logic remains unchanged) ...
            Log(LogLevel.INFO, "SyncCustomers: Starting multi-router customer archive reconciliation...");
            try
            {
                List<Customer> allLocalCustomersInDb = _customerRepository.GetCustomers(includeArchived: true);

                foreach (var router in allRouters)
                {
                    var customersFromThisRouterInDb = allLocalCustomersInDb.Where(c => c.RouterId == router.Id).ToList();
                    var activeSecretsOnThisRouterThisCycle = allSyncedMikrotikSecretIdsThisCycle
                                                             .Where(s => s.RouterId == router.Id)
                                                             .Select(s => s.MikrotikSecretId)
                                                             .ToHashSet();
                    int newlyArchivedCount = 0;
                    int newlyUnarchivedCount = 0;

                    if (customersFromThisRouterInDb.Any() || activeSecretsOnThisRouterThisCycle.Any())
                    {
                        Log(LogLevel.DEBUG, $"Reconciliation for Router ID {router.Id}: DB Customers for this router: {customersFromThisRouterInDb.Count}, Synced Secrets from this router: {activeSecretsOnThisRouterThisCycle.Count}");
                    }

                    foreach (var localCust in customersFromThisRouterInDb)
                    {
                        bool existsOnMikrotikThisCycle = !string.IsNullOrEmpty(localCust.MikrotikSecretId) &&
                                                         activeSecretsOnThisRouterThisCycle.Contains(localCust.MikrotikSecretId);

                        if (!existsOnMikrotikThisCycle && !localCust.IsArchived)
                        {
                            Log(LogLevel.INFO, $"SyncCustomers: Archiving customer '{localCust.AccountName}' (ID: {localCust.Id}, MkID: {localCust.MikrotikSecretId}) from Router ID {router.Id} as it's no longer found on the router this cycle.", routerId: router.Id);
                            _customerRepository.ArchiveCustomerById(localCust.Id);
                            newlyArchivedCount++;
                        }
                        else if (existsOnMikrotikThisCycle && localCust.IsArchived)
                        {
                            // This log confirms if a customer who *is* on Mikrotik *was* archived in the DB.
                            // The main loop's logic (setting cust.IsArchived = false if active) should handle unarchiving via InsertOrUpdateCustomer.
                            Log(LogLevel.INFO, $"SyncCustomers: Customer '{localCust.AccountName}' (ID: {localCust.Id}, MkID: {localCust.MikrotikSecretId}) from Router ID {router.Id} exists on Mikrotik and was marked 'Archived' in DB. Main sync logic should have updated IsArchived to false if they were active or not disabled.", routerId: router.Id);
                            newlyUnarchivedCount++; // This counts how many were in an "archived but shouldn't be" state prior to this sync's update.
                        }
                    }
                    if (newlyArchivedCount > 0 || newlyUnarchivedCount > 0 || customersFromThisRouterInDb.Any() || activeSecretsOnThisRouterThisCycle.Any())
                    {
                        Log(LogLevel.INFO, $"SyncCustomers: Reconciliation for Router ID {router.Id}: Archived {newlyArchivedCount}, Found in DB as Archived but Present on Mikrotik (candidates for unarchive by main sync): {newlyUnarchivedCount}.", routerId: router.Id);
                    }
                }
                _netwatchConfigRepository.UpdateLastDataSyncTimestamp(_serviceNameForLogging, DateTime.Now);
                Log(LogLevel.INFO, $"SyncCustomers: Successfully recorded LastDataSyncTimestamp for '{_serviceNameForLogging}'.");

            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "SyncCustomers: Error during customer archive reconciliation.", ex);
            }

            Log(LogLevel.INFO, "SyncCustomers: Multi-router customer synchronization cycle completed.");
        }

        // Helper extension method if not already available (tik4net might have something similar)
        // This ensures GetAttributeValue doesn't throw if a key is missing.
    }

    // If TikSentenceExtensions is not in this file or accessible, you might need to define GetWordValueOrDefault
    // or ensure it's available. For example:
    public static class TikSentenceExtensionsForSyncManager
    {
        public static string GetWordValueOrDefault(this ITikReSentence sentence, string fieldName, string defaultValue = null)
        {
            return sentence.Words.ContainsKey(fieldName) ? sentence.GetAttributeValue(fieldName) : defaultValue;
        }
    }
}


