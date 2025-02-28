using System;
using System.Collections.Generic;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;
using tik4net.Objects.Ppp;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public class SyncManager
    {
        private MikrotikService mikrotikService;
        private CustomerRepository customerRepository;

        public SyncManager()
        {
            mikrotikService = new MikrotikService();
            customerRepository = new CustomerRepository();
        }

        public void SyncCustomers(string host, string username, string password)
        {
            // Retrieve PPPoE accounts from Mikrotik
            List<PppSecret> accounts = mikrotikService.GetPPPoeAccounts(host, username, password);

            foreach (var account in accounts)
            {
                // Map the Mikrotik PPPoE account to a Customer object
                Customer customer = new Customer()
                {
                    AccountName = account.name  // Assuming 'name' is the identifier
                    // You can map additional fields if available
                };

                // Insert or update the customer in the database
                customerRepository.InsertOrUpdateCustomer(customer);
            }
        }
    }
}
