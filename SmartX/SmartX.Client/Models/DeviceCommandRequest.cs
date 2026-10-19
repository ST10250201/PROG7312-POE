
namespace SmartX.Client.Models
{
    public class DeviceCommandRequest
    {
        public string MacAddress { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
    }
}
