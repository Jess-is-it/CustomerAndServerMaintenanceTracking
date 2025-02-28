using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
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
                using (var connection = ConnectionFactory.CreateConnection(TikConnectionType.Api))
                {
                    connection.Open(host, username, password);

                    // Create a command to print all PPPoE secrets
                    var command = connection.CreateCommand("/ppp/secret/print");
                    // Execute the command; the response is a list of ITikReSentence objects
                    var response = command.ExecuteList();

                    foreach (var item in response)
                    {

                        // Check if the account is disabled.
                        string disabledValue = item.GetAttributeValue("disabled");
                        if (!string.IsNullOrEmpty(disabledValue) &&
                            disabledValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            // Skip this account since it's disabled.
                            continue;
                        }

                        PppSecret secret = new PppSecret();
                        secret.Name = item.GetAttributeValue("name");
                        // Map any additional properties if needed.
                        accounts.Add(secret);
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving PPPoE accounts", ex);
            }

            return accounts;
        }
    }
}
