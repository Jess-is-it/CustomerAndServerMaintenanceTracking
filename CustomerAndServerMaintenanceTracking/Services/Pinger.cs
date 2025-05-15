using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public static class Pinger
    {
        public static async Task<PingReply> SendPingAsync(string ipAddressOrHost, int timeoutMilliseconds = 1000)
        {
            if (string.IsNullOrWhiteSpace(ipAddressOrHost))
            {
                throw new ArgumentNullException(nameof(ipAddressOrHost), "IP address or host name cannot be null or empty.");
            }

            if (timeoutMilliseconds <= 0)
            {
                timeoutMilliseconds = 1000; // Default to 1 second if invalid timeout is given
            }

            Ping pingSender = new Ping();
            PingReply reply = null;

            try
            {
                // Send the ping asynchronously.
                // The default TTL (Time To Live) is typically 128, which is usually sufficient.
                // You can specify PingOptions if you need to change TTL, DonFragment, etc.
                reply = await pingSender.SendPingAsync(ipAddressOrHost, timeoutMilliseconds);

                // Optional: Log the attempt and result for debugging
                // Debug.WriteLine($"Ping to {ipAddressOrHost}: Status={reply.Status}, RTT={(reply.Status == IPStatus.Success ? reply.RoundtripTime.ToString() : "N/A")}");
            }
            catch (PingException ex)
            {
                // This can happen for various reasons, e.g., host not found (if DNS resolution fails),
                // or network errors preventing the ping request from being sent.
                Debug.WriteLine($"PingException for {ipAddressOrHost}: {ex.Message}");
                // We might want to return a custom object or a specific PingReply status for such errors
                // For now, re-throwing or returning null might be options, or craft a reply.
                // Let's simulate a "GeneralFailure" or return null to indicate an issue before even getting a reply object.
                // For simplicity in consumption, often it's better to let it throw or handle it by returning a specific status
                // if PingReply cannot represent this state.
                // However, SendPingAsync itself might return a PingReply with a status like DestinationHostUnreachable,
                // BadDestination, etc., even if an exception *could* be thrown by the Ping object itself prior to sending.
                // The most common case for PingException here is if the host name cannot be resolved.
                // If it's an IP, this is less likely.

                // For now, let's log and allow `reply` to remain null or be the result from a failed SendPingAsync attempt.
                // If SendPingAsync itself throws, `reply` will be null.
                // If SendPingAsync completes but indicates failure (e.g. DestinationHostUnreachable), reply will have that status.
                // This catch block is more for issues with the Ping object or fundamental send issues not covered by IPStatus.
            }
            catch (Exception ex) // Catch other potential exceptions
            {
                Debug.WriteLine($"Generic Exception during ping to {ipAddressOrHost}: {ex.Message}");
                // Handle other unexpected errors, `reply` will be null.
            }
            finally
            {
                // The Ping object implements IDisposable, but SendPingAsync typically handles its resources.
                // If you were using the synchronous Send method in a loop, explicit Dispose would be more critical.
                // For SendPingAsync, the Ping object can often be reused if needed, or let it be.
                // For a static helper like this, creating a new Ping() each time is fine.
                pingSender.Dispose();
            }

            return reply; // This can be null if a PingException occurred before reply was set,
                          // or it will contain the status from the ping attempt (e.g., Success, TimedOut, DestinationHostUnreachable).
        }
    }
}
