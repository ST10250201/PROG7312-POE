
namespace SmartX.Api.Services
{
    public class ProcessingModeService
    {
        private readonly object _lock = new();

        private bool _automaticProcessingEnabled = true;

        public bool AutomaticProcessingEnabled
        {
            get
            {
                lock (_lock)
                {
                    return _automaticProcessingEnabled;
                }
            }
        }

        public string CurrentMode
        {
            get
            {
                return AutomaticProcessingEnabled
                    ? "Automatic"
                    : "Manual";
            }
        }

        public void SetAutomaticProcessing(bool enabled)
        {
            lock (_lock)
            {
                _automaticProcessingEnabled = enabled;
            }
        }
    }
}
