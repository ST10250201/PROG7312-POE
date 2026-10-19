
namespace SmartX.Client.Models
{
    public class DeviceCommandDto
    {
        public int Id { get; set; }

        public string MacAddress { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string PreviousState { get; set; } = string.Empty;

        public string NewState { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public bool IsUndo { get; set; }

        public int? UndoesCommandId { get; set; }
    }
}
