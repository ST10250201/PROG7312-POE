
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX.Api.Services
{
    public class TelemetryProcessingService
    {
        private readonly object _lock = new();

        // FIFO queue for standard telemetry.
        private readonly Queue<TelemetryJob> _normalQueue = new();

        // Lower priority numbers are processed first.
        private readonly PriorityQueue<TelemetryJob, int>
            _criticalQueue = new();

        // Timestamp-ordered history.
        private readonly SortedDictionary<DateTime, TelemetryJob>
            _history = new();

        // Unique active critical alerts.
        private readonly HashSet<string> _activeAlerts =
            new(StringComparer.OrdinalIgnoreCase);

        private long _sequence;

        public int NormalQueueCount
        {
            get
            {
                lock (_lock)
                    return _normalQueue.Count;
            }
        }

        public int CriticalQueueCount
        {
            get
            {
                lock (_lock)
                    return _criticalQueue.Count;
            }
        }

        public void Enqueue(
            string macAddress,
            string dataType,
            double value,
            string unit)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                throw new ArgumentException(
                    "MAC address is required.");

            lock (_lock)
            {
                var job = new TelemetryJob
                {
                    Sequence = ++_sequence,
                    MacAddress = macAddress.Trim(),
                    DataType = dataType,
                    Value = value,
                    Unit = unit,
                    Timestamp = DateTime.UtcNow
                };

                if (value >= 80)
                {
                    job.Priority = 0;

                    _criticalQueue.Enqueue(job, job.Priority);

                    _activeAlerts.Add(job.MacAddress);
                }
                else
                {
                    job.Priority = 1;

                    _normalQueue.Enqueue(job);
                }
            }
        }

        public TelemetryJob? ProcessNext()
        {
            lock (_lock)
            {
                TelemetryJob job;

                if (_criticalQueue.Count > 0)
                {
                    job = _criticalQueue.Dequeue();
                }
                else if (_normalQueue.Count > 0)
                {
                    job = _normalQueue.Dequeue();
                }
                else
                {
                    return null;
                }

                // Ensure unique timestamp keys.
                DateTime key = job.Timestamp;

                while (_history.ContainsKey(key))
                    key = key.AddTicks(1);

                _history.Add(key, job);

                return job;
            }
        }

        public IReadOnlyList<TelemetryJob> ProcessAll()
        {
            var processed = new List<TelemetryJob>();

            while (true)
            {
                var job = ProcessNext();

                if (job == null)
                    break;

                processed.Add(job);
            }

            return processed;
        }

        public IReadOnlyList<TelemetryJob> GetHistory()
        {
            lock (_lock)
            {
                return _history.Values.ToList();
            }
        }

        public IReadOnlyList<string> GetActiveAlerts()
        {
            lock (_lock)
            {
                return _activeAlerts.OrderBy(x => x).ToList();
            }
        }

        public bool ClearAlert(string macAddress)
        {
            lock (_lock)
            {
                return _activeAlerts.Remove(macAddress);
            }
        }
    }

    public class TelemetryJob
    {
        public long Sequence { get; set; }

        public string MacAddress { get; set; } = "";

        public string DataType { get; set; } = "";

        public double Value { get; set; }

        public string Unit { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public int Priority { get; set; }
    }
}
