
namespace SmartX.Api.Models
{
    public class SensorRecord
    {
        public int Id { get; set; }

        public string MacAddress { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = "Normal";

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        // Part 2: Latest telemetry information.
        public DateTime? LastTelemetryAt { get; set; }

        public double? LatestValue { get; set; }

        public string LatestUnit { get; set; } = string.Empty;
    }
}
