
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX.Api.Services
{
    public class TelemetryProcessingService
    {
        private readonly object _lock = new();

        // Standard telemetry is processed in FIFO order.
        private readonly Queue<TelemetryJob> _normalQueue = new();

        // Critical telemetry bypasses the standard queue.
        // Sequence ensures FIFO order among critical jobs.
        private readonly PriorityQueue<TelemetryJob, (int, long)>
            _criticalQueue = new();

        // Timestamp-ordered processing history.
        private readonly SortedDictionary<DateTime, TelemetryJob>
            _history = new();

        // Prevent duplicate critical alerts per sensor.
        private readonly HashSet<string> _activeAlerts =
            new(StringComparer.OrdinalIgnoreCase);

        private long _sequence;

        public int NormalQueueCount
        {
            get
            {
                lock (_lock)
                {
                    return _normalQueue.Count;
                }
            }
        }

        public int CriticalQueueCount
        {
            get
            {
                lock (_lock)
                {
                    return _criticalQueue.Count;
                }
            }
        }

        // -------------------------------------------------------
        // ENQUEUE TELEMETRY
        // -------------------------------------------------------

        public void Enqueue(
            string macAddress,
            string dataType,
            double value,
            string unit)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                throw new ArgumentException(
                    "MAC address is required.");
            }

            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                throw new ArgumentException(
                    "Telemetry value must be finite.");
            }

            lock (_lock)
            {
                var job = new TelemetryJob
                {
                    Sequence = ++_sequence,
                    MacAddress = macAddress.Trim(),
                    DataType = dataType ?? string.Empty,
                    Value = value,
                    Unit = unit ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Priority = value >= 80 ? 0 : 1
                };

                if (job.Priority == 0)
                {
                    _criticalQueue.Enqueue(
                        job,
                        (job.Priority, job.Sequence));
                }
                else
                {
                    _normalQueue.Enqueue(job);
                }
            }
        }

        // -------------------------------------------------------
        // PROCESS NEXT TELEMETRY JOB
        // -------------------------------------------------------

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

                DateTime key = job.Timestamp;

                // SortedDictionary requires unique timestamp keys.
                while (_history.ContainsKey(key))
                {
                    key = key.AddTicks(1);
                }

                _history.Add(key, job);

                return job;
            }
        }

        // -------------------------------------------------------
        // UPDATE CRITICAL ALERT STATE
        // -------------------------------------------------------

        public void UpdateAlertState(
            string macAddress,
            double value)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return;
            }

            lock (_lock)
            {
                string key = macAddress.Trim();

                if (value >= 80)
                {
                    _activeAlerts.Add(key);
                }
                else
                {
                    // Recovery: remove an alert when the latest
                    // accepted reading is below the critical threshold.
                    _activeAlerts.Remove(key);
                }
            }
        }

        // -------------------------------------------------------
        // PROCESS ALL QUEUED JOBS
        // -------------------------------------------------------

        public IReadOnlyList<TelemetryJob> ProcessAll()
        {
            var processed = new List<TelemetryJob>();

            while (true)
            {
                TelemetryJob? job = ProcessNext();

                if (job == null)
                {
                    break;
                }

                processed.Add(job);
            }

            return processed;
        }

        // -------------------------------------------------------
        // PROCESS QUEUED JOBS AND UPDATE SENSOR REGISTRY
        // -------------------------------------------------------

        public IReadOnlyList<TelemetryJob>
            ProcessPendingAndUpdateSensors(
                SensorRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            var processed = new List<TelemetryJob>();

            while (true)
            {
                TelemetryJob? job = ProcessNext();

                if (job == null)
                {
                    break;
                }

                bool updated = registry.UpdateTelemetry(
                    job.MacAddress,
                    job.Value,
                    job.Unit,
                    job.Timestamp);

                // Do not let an older queued reading
                // change the current alert state.
                if (updated)
                {
                    UpdateAlertState(
                        job.MacAddress,
                        job.Value);
                }

                processed.Add(job);
            }

            return processed;
        }

        // -------------------------------------------------------
        // CHRONOLOGICAL TELEMETRY HISTORY
        // -------------------------------------------------------

        public IReadOnlyList<TelemetryJob> GetHistory()
        {
            lock (_lock)
            {
                return _history.Values.ToList();
            }
        }

        // -------------------------------------------------------
        // UNIQUE ACTIVE CRITICAL ALERTS
        // -------------------------------------------------------

        public IReadOnlyList<string> GetActiveAlerts()
        {
            lock (_lock)
            {
                return _activeAlerts
                    .OrderBy(mac => mac)
                    .ToList();
            }
        }

        public bool ClearAlert(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return false;
            }

            lock (_lock)
            {
                return _activeAlerts.Remove(
                    macAddress.Trim());
            }
        }
    }

    // -------------------------------------------------------
    // TELEMETRY JOB MODEL
    // -------------------------------------------------------

    public class TelemetryJob
    {
        public long Sequence { get; set; }

        public string MacAddress { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public double Value { get; set; }

        public string Unit { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public int Priority { get; set; }
    }
}
