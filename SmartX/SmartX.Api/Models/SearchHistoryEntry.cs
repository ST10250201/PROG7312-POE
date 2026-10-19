
namespace SmartX.Api.Models
{
    public class SearchHistoryEntry
    {
        public int Id { get; set; }

        public string SearchTerm { get; set; } = string.Empty;

        public string Category { get; set; } = "All";

        public bool ActiveAlertsOnly { get; set; }

        public int ResultCount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
