namespace SmartX.Client.Models
{
    public class SensorRecord
    {
        public int Id { get; set; }

        public string MacAddress { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = "Normal";

        public DateTime LastSeen { get; set; }
    }
}