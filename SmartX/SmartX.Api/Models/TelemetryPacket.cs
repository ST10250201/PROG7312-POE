namespace SmartX.Api.Models
{
    public class TelemetryPacket<T>
    {
        public string MacAddress { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public T? Value { get; set; }

        public string Unit { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsValid { get; set; } = true;

        public string ValidationMessage { get; set; } = "Valid";

        public static TelemetryPacket<double> operator +(
            TelemetryPacket<T> first,
            TelemetryPacket<T> second)
        {
            double firstValue =
                Convert.ToDouble(first.Value);

            double secondValue =
                Convert.ToDouble(second.Value);

            return new TelemetryPacket<double>
            {
                MacAddress = first.MacAddress,
                DataType = first.DataType,
                Value = firstValue + secondValue,
                Unit = first.Unit,
                Timestamp = DateTime.UtcNow,
                IsValid = first.IsValid && second.IsValid,
                ValidationMessage = "Aggregated telemetry packet"
            };
        }
    }
}