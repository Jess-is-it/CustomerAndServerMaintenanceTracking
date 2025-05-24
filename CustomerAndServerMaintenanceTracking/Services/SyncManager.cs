using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using CustomerAndServerMaintenanceTracking.Services;
using tik4net;
using tik4net.Objects;
using tik4net.Objects.Ppp;
using SharedLibrary.Models;
using SharedLibrary.DataAccess;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public class SyncManager
    {

        private CustomerRepository customerRepository;
        public static event EventHandler DataSynced;

        public SyncManager()
        {
            customerRepository = new CustomerRepository();
        }

        // This method now uses the persistent connection from MikrotikClientManager.
        public Dictionary<string, string> GetPPPActiveConnections()
        {
            Dictionary<string, string> activeDict = new Dictionary<string, string>();

            try
            {
                // Retrieve the connection from MikrotikClientManager.
                // This connection is maintained persistently, so you no longer pass host/username/password.
                ITikConnection connection = MikrotikClientManager.Instance.GetConnection();

                // Execute "/ppp/active/print" to list active PPP connections
                var cmd = connection.CreateCommand("/ppp/active/print");
                var response = cmd.ExecuteList();

                foreach (var item in response)
                {
                    // Assume the attributes are named "name" and "address" (or "remote-address" if applicable)
                    string pppName = item.GetAttributeValue("name");
                    string remoteIP = item.GetAttributeValue("address");


                    if (!string.IsNullOrEmpty(pppName) && !string.IsNullOrEmpty(remoteIP))
                    {
                        activeDict[pppName] = remoteIP;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving PPP active connections", ex);
            }

            return activeDict;
        }


        public void SyncCustomers()
        {
            // 1) Get an ITikConnection from MikrotikClientManager
            var connection = MikrotikClientManager.Instance.GetConnection();

            // 2) Load PPP secrets from Mikrotik (PPPoe accounts)
            var accounts = connection.LoadAll<PppSecret>();

            // Retrieve the active connections dictionary
            Dictionary<string, string> activeDict = GetPPPActiveConnections();

            // .Name is the PPP secret name

            // 3) (Optional) If you need active connections, do:
            // var activeConns = connection.LoadAll<PppActive>();

            var activeAccountNames = accounts.Select(a => a.Name).ToList();

            // 4) Insert/Update each account in DB
            foreach (var account in accounts)
            {
                activeDict.TryGetValue(account.Name, out string ip);

                Customer cust = new Customer
                {
                    AccountName = account.Name,
                    IPAddress = ip, // or from PppActive if needed
                    IsArchived = false
                };
                customerRepository.InsertOrUpdateCustomer(cust);
            }

            // 5) Archive or unarchive based on active list
            var allCustomers = customerRepository.GetCustomers();
            foreach (var cust in allCustomers)
            {
                bool isActive = activeAccountNames.Contains(cust.AccountName);
                if (!isActive && !cust.IsArchived)
                    customerRepository.ArchiveCustomer(cust.AccountName);
                else if (isActive && cust.IsArchived)
                    customerRepository.MarkActiveCustomer(cust.AccountName);
            }

            // Raise the event to notify that data has been synced.
            DataSynced?.Invoke(this, EventArgs.Empty);
            // 6) Keep the connection alive
            MikrotikClientManager.Instance.UpdateLastActivity();
        }
    }
}
