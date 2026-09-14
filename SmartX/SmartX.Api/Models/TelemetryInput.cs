namespace SmartX.Api.Models
{
    public class TelemetryInput
    {
        public string MacAddress { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public double Value { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}