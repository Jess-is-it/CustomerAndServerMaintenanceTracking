using System;
using System.Collections.Generic;
using System.Linq;
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
            // Retrieve current accounts from Mikrotik
            List<PppSecret> accounts = mikrotikService.GetPPPoeAccounts(host, username, password);

            // Create a list of account names currently active on Mikrotik
            var activeAccountNames = accounts.Select(a => a.Name).ToList();

            // For each account retrieved, update or insert it as active
            foreach (var account in accounts)
            {
                Customer customer = new Customer()
                {
                    AccountName = account.Name,
                    // You can add additional mapping here.
                    IsArchived = false  // Always active if found in Mikrotik
                };

                customerRepository.InsertOrUpdateCustomer(customer);
            }

            // Now, retrieve all customers from the database
            List<Customer> allCustomers = customerRepository.GetCustomers();

            // For each customer that is currently active in our system,
            // if it is not found in the list of activeAccountNames, mark it as archived.
            foreach (var customer in allCustomers)
            {
                if (!activeAccountNames.Contains(customer.AccountName) && !customer.IsArchived)
                {
                    customerRepository.ArchiveCustomer(customer.AccountName);
                }
                else if (activeAccountNames.Contains(customer.AccountName) && customer.IsArchived)
                {
                    // If an archived customer reappears on Mikrotik, mark it as active
                    customerRepository.MarkActiveCustomer(customer.AccountName);
                }
            }
        }
    }
}
