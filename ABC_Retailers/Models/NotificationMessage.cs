namespace ABC_Retailers.Models
{
    public class NotificationMessage
    {
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int NewStock { get; set; }
    }
}

