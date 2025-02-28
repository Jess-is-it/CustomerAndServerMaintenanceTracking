using System;
using System.Collections.Generic;
using tik4net;
using tik4net.Objects.Ppp;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public class MikrotikService
    {
        /// <summary>
        /// Connects to a Mikrotik router and retrieves the list of PPPoE accounts.
        /// </summary>
        /// <param name="host">IP or hostname of the Mikrotik router</param>
        /// <param name="username">Router username</param>
        /// <param name="password">Router password</param>
        /// <returns>List of PPPoE account details as PppSecret objects</returns>
        public List<PppSecret> GetPPPoeAccounts(string host, string username, string password)
        {
            List<PppSecret> accounts = new List<PppSecret>();

            try
            {
                // Create a connection to the Mikrotik router using the API.
                using (var connection = ConnectionFactory.CreateConnection(TikConnectionType.Api))
                {
                    connection.Open(host, username, password);

                    // Retrieve all PPPoE secrets (accounts)
                    accounts = connection.LoadList<PppSecret>();

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                // For debugging, you might log the error or rethrow it.
                throw new Exception("Error retrieving PPPoE accounts", ex);
            }

            return accounts;
        }
    }
}
