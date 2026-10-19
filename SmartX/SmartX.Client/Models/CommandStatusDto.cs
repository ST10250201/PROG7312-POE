
namespace SmartX.Client.Models
{
    public class CommandStatusDto
    {
        public int UndoCount { get; set; }

        public bool UndoAvailable { get; set; }

        public int HistoryCount { get; set; }
    }
}
