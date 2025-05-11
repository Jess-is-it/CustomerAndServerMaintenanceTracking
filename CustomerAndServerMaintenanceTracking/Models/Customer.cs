namespace CustomerAndServerMaintenanceTracking.Models
{
    public class Customer
    {
        public int Id { get; set; }  // Database primary key
        public string AccountName { get; set; }  // Required field from Mikrotik PPPoE account
        public string AdditionalName { get; set; }  // Optional
        public string ContactNumber { get; set; }   // Optional
        public string Email { get; set; }           // Optional
        public string Location { get; set; }        // Optional
        public bool IsArchived { get; set; }
        public string IPAddress { get; set; }
    }
}
