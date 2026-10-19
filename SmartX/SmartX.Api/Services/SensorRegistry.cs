
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class SensorRegistry
    {
        // Part 2: Hash-based device lookup using MAC addresses.
        private readonly Dictionary<string, SensorRecord> _sensors =
            new(StringComparer.OrdinalIgnoreCase);

        // Protects the shared singleton registry from concurrent requests.
        private readonly object _lock = new();

        private int _nextId = 1;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _sensors.Count;
                }
            }
        }

        public IReadOnlyList<SensorRecord> GetAll()
        {
            lock (_lock)
            {
                return _sensors.Values
                    .OrderBy(sensor => sensor.Id)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public void Add(SensorRecord sensor)
        {
            if (sensor == null)
            {
                throw new ArgumentNullException(nameof(sensor));
            }

            if (string.IsNullOrWhiteSpace(sensor.MacAddress))
            {
                throw new ArgumentException(
                    "A valid MAC address is required.");
            }

            lock (_lock)
            {
                string macAddress = sensor.MacAddress.Trim();

                if (_sensors.ContainsKey(macAddress))
                {
                    throw new InvalidOperationException(
                        "A sensor with this MAC address already exists.");
                }

                sensor.MacAddress = macAddress;

                _sensors.Add(macAddress, sensor);

                // Keep ID generation ahead of registered IDs.
                _nextId = Math.Max(_nextId, sensor.Id + 1);
            }
        }

        public SensorRecord? FindByMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return null;
            }

            lock (_lock)
            {
                _sensors.TryGetValue(
                    macAddress.Trim(),
                    out SensorRecord? sensor);

                return sensor;
            }
        }

        public bool ContainsMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return false;
            }

            lock (_lock)
            {
                return _sensors.ContainsKey(macAddress.Trim());
            }
        }

        public int GenerateNextId()
        {
            lock (_lock)
            {
                return _nextId++;
            }
        }



        public bool UpdateTelemetry(
            string macAddress,
            double value,
            string unit,
            DateTime receivedAt)
        {
            if (string.IsNullOrWhiteSpace(macAddress) ||
                double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return false;
            }

            lock (_lock)
            {
                if (!_sensors.TryGetValue(
                    macAddress.Trim(),
                    out SensorRecord? sensor))
                {
                    return false;
                }

                // Prevent an older reading from overwriting newer telemetry.
                if (sensor.LastTelemetryAt.HasValue &&
                    receivedAt < sensor.LastTelemetryAt.Value)
                {
                    return false;
                }

                sensor.LatestValue = value;
                sensor.LatestUnit = unit ?? string.Empty;
                sensor.LastTelemetryAt = receivedAt;
                sensor.LastSeen = receivedAt;

                sensor.Status = value switch
                {
                    >= 80 => "Critical",
                    >= 60 => "Warning",
                    _ => "Normal"
                };

                return true;
            }
        }



    }









}
