
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

                    _criticalQueue.Enqueue(
                        job,
                        job.Priority);

                    _activeAlerts.Add(job.MacAddress);
                }
                else
                {
                    job.Priority = 1;

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

                // Prevent duplicate timestamp keys.
                DateTime key = job.Timestamp;

                while (_history.ContainsKey(key))
                {
                    key = key.AddTicks(1);
                }

                _history.Add(key, job);

                return job;
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
        // PART 2 — PROCESS JOBS AND UPDATE SENSOR REGISTRY
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

                // Update the sensor using its original timestamp.
                // Older readings cannot overwrite newer readings.
                registry.UpdateTelemetry(
                    job.MacAddress,
                    job.Value,
                    job.Unit,
                    job.Timestamp);

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
