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

        // Operator overload allows two numeric telemetry packets
        // to be aggregated into one combined value.
        public static TelemetryPacket<double> operator +(
            TelemetryPacket<T> first,
            TelemetryPacket<T> second)
        {
            double firstValue = Convert.ToDouble(first.Value);
            double secondValue = Convert.ToDouble(second.Value);

            return new TelemetryPacket<double>
            {
                MacAddress = $"{first.MacAddress} + {second.MacAddress}",
                Value = firstValue + secondValue,
                Timestamp = DateTime.Now,
                Unit = first.Unit,
                IsValid = first.IsValid && second.IsValid,
                ValidationMessage = "Aggregated telemetry value"
            };
        }
    }
}