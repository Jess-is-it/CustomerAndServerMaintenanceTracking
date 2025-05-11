using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public class PingService
    {
        public async Task<PingReply> PingAsync(string targetIP, int timeout)
        {
            using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
            {
                try
                {
                    return await ping.SendPingAsync(targetIP, timeout);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
