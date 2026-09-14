using System.ComponentModel.DataAnnotations;

namespace SmartX.Web.Models
{
    public class TelemetryRequest
    {
        [Required(ErrorMessage = "Sensor MAC address is required.")]
        public string MacAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telemetry type is required.")]
        public string DataType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telemetry value is required.")]
        public string Value { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;
    }
}