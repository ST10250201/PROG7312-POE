using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class SensorRegistry
    {
        private readonly List<SensorRecord> _sensors = new();

        public int Count => _sensors.Count;

        public IReadOnlyList<SensorRecord> GetAll()
        {
            return _sensors.AsReadOnly();
        }

        public void Add(SensorRecord sensor)
        {
            _sensors.Add(sensor);
        }

        public SensorRecord? FindByMacAddress(string macAddress)
        {
            return _sensors.FirstOrDefault(sensor =>
                sensor.MacAddress.Equals(
                    macAddress,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool ContainsMacAddress(string macAddress)
        {
            return _sensors.Any(sensor =>
                sensor.MacAddress.Equals(
                    macAddress,
                    StringComparison.OrdinalIgnoreCase));
        }

        public int GenerateNextId()
        {
            return _sensors.Count == 0
                ? 1
                : _sensors.Max(sensor => sensor.Id) + 1;
        }
    }
}