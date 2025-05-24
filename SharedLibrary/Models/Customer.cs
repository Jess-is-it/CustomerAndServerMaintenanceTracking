namespace SharedLibrary.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string AccountName { get; set; }
        public string AdditionalName { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public bool IsArchived { get; set; }
        public string IPAddress { get; set; } // Or whatever type you are using

        // --- New Properties ---
        public int? RouterId { get; set; } // Nullable int
        public string MacAddress { get; set; }
        public string MikrotikSecretId { get; set; } // Stores the .id from Mikrotik

        // For display purposes, typically populated by a JOIN in the repository
        public string RouterName { get; set; }
    }
}
