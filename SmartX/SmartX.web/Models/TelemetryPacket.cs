namespace SmartX.Web.Models
{
    public class TelemetryPacket<T>
    {
        public string MacAddress { get; set; } = string.Empty;

        public T? Value { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string Unit { get; set; } = string.Empty;

        public bool IsValid { get; set; } = true;

        public string ValidationMessage { get; set; } = "Valid";
    }
}