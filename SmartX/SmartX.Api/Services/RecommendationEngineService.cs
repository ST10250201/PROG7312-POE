
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class RecommendationEngineService
    {
        private readonly SensorRegistry _sensorRegistry;
        private readonly TelemetryProcessingService _telemetryService;
        private readonly DeviceCommandService _commandService;
        private readonly SearchHistoryService _searchHistoryService;

        public RecommendationEngineService(
            SensorRegistry sensorRegistry,
            TelemetryProcessingService telemetryService,
            DeviceCommandService commandService,
            SearchHistoryService searchHistoryService)
        {
            _sensorRegistry = sensorRegistry;
            _telemetryService = telemetryService;
            _commandService = commandService;
            _searchHistoryService = searchHistoryService;
        }

        public List<DeviceRecommendation> GenerateRecommendations()
        {
            var recommendations = new List<DeviceRecommendation>();

            var sensors = _sensorRegistry.GetAll();
            var commands = _commandService.GetHistory();
            var deviceStates = _commandService.GetDeviceStates();

            // Actual operator searches recorded through the website.
            var searchHistory = _searchHistoryService.GetHistory();
            var searchFrequency = _searchHistoryService.GetSearchFrequency();

            // Use the active-alert set rather than relying only on
            // the sensor's displayed status.
            var activeAlerts = new HashSet<string>(
                _telemetryService.GetActiveAlerts(),
                StringComparer.OrdinalIgnoreCase);

            int nextId = 1;

            foreach (var sensor in sensors)
            {
                string mac = sensor.MacAddress;

                var sensorCommands = commands
                    .Where(command =>
                        string.Equals(
                            command.MacAddress,
                            mac,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                bool isCritical =
                    string.Equals(
                        sensor.Status,
                        "Critical",
                        StringComparison.OrdinalIgnoreCase)
                    || activeAlerts.Contains(mac);

                bool isDisconnected =
                    string.Equals(
                        sensor.Status,
                        "Disconnected",
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        sensor.Status,
                        "Offline",
                        StringComparison.OrdinalIgnoreCase);

                // RULE 1: Critical sensor condition
                if (isCritical)
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Critical Sensor Condition",
                        Description =
                            $"Sensor {mac} is reporting a critical condition.",
                        RecommendedAction =
                            "Investigate the sensor and its environment.",
                        Severity = "Critical",
                        Reason =
                            "The sensor registry reports Critical status " +
                            "or the active-alert set contains this MAC address."
                    });
                }

                // RULE 2: Disconnected or offline sensor
                if (isDisconnected)
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Disconnected Sensor",
                        Description =
                            $"Sensor {mac} is currently reported as " +
                            $"{sensor.Status}.",
                        RecommendedAction =
                            "Check the sensor connection, power supply " +
                            "and network availability.",
                        Severity = "Warning",
                        Reason =
                            $"The sensor registry reports a " +
                            $"{sensor.Status} status."
                    });
                }

                // RULE 3: Critical condition with pump OFF
                string pumpKey = $"{mac}|PUMP";

                bool pumpIsOff =
                    !deviceStates.TryGetValue(
                        pumpKey,
                        out var pumpState)
                    || string.Equals(
                        pumpState,
                        "OFF",
                        StringComparison.OrdinalIgnoreCase);

                if (isCritical &&
                    pumpIsOff &&
                    string.Equals(
                        sensor.Category,
                        "Temperature",
                        StringComparison.OrdinalIgnoreCase))
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Review Pump Operation",
                        Description =
                            "A temperature sensor is in a critical state " +
                            "while its associated pump is OFF.",
                        RecommendedAction =
                            "Inspect the temperature condition and determine " +
                            "whether activating the pump is appropriate.",
                        Severity = "Warning",
                        Reason =
                            "Critical temperature status combined with " +
                            "the current simulated pump state."
                    });
                }

                // RULE 4: Repeated manual pump activation
                int pumpOnCount = sensorCommands.Count(command =>
                    !command.IsUndo &&
                    string.Equals(
                        command.DeviceType,
                        "Pump",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        command.Action,
                        "ON",
                        StringComparison.OrdinalIgnoreCase));

                if (pumpOnCount >= 3)
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Repeated Pump Activation",
                        Description =
                            $"The pump has been manually activated " +
                            $"{pumpOnCount} times for this sensor.",
                        RecommendedAction =
                            "Review recurring pump usage and consider " +
                            "whether an automated control rule is appropriate.",
                        Severity = "Info",
                        Reason =
                            $"Command history contains {pumpOnCount} " +
                            "manual Pump ON commands."
                    });
                }

                // RULE 5: Frequent manual device adjustments
                int manualCommandCount = sensorCommands.Count(
                    command => !command.IsUndo);

                if (manualCommandCount >= 5)
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Frequent Manual Device Control",
                        Description =
                            $"This sensor has {manualCommandCount} " +
                            "recorded manual device commands.",
                        RecommendedAction =
                            "Review device activity for repeated adjustments " +
                            "or unstable operating conditions.",
                        Severity = "Info",
                        Reason =
                            $"Command history contains {manualCommandCount} " +
                            "manual commands for this sensor."
                    });
                }

                // -------------------------------------------------------
                // RULE 6: REPEATED SEARCHES FOR A SENSOR
                // -------------------------------------------------------

                // A recorded search is associated with this sensor
                // when its term matches the sensor's MAC address,
                // a MAC fragment, or its deployment location.
                //
                // Search terms must contain at least 2 characters
                // to avoid treating very broad single-character
                // searches as meaningful sensor-specific evidence.

                var matchingSearches = searchHistory
                    .Where(search =>
                    {
                        string term = search.SearchTerm?.Trim()
                            ?? string.Empty;

                        if (term.Length < 2)
                        {
                            return false;
                        }

                        return mac.Contains(
                                   term,
                                   StringComparison.OrdinalIgnoreCase)
                               || sensor.Location.Contains(
                                   term,
                                   StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                if (matchingSearches.Count >= 3)
                {
                    bool repeatedSearchWithCriticalCondition = isCritical;

                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Repeated Sensor Investigation",
                        Description =
                            $"Operators have recorded " +
                            $"{matchingSearches.Count} searches " +
                            $"matching sensor {mac} or its location.",
                        RecommendedAction =
                            repeatedSearchWithCriticalCondition
                                ? "Prioritise investigation of this sensor. " +
                                  "Repeated operator searches coincide " +
                                  "with an active critical condition."
                                : "Review this sensor's telemetry history " +
                                  "to determine why operators are " +
                                  "repeatedly searching for it.",
                        Severity =
                            repeatedSearchWithCriticalCondition
                                ? "Warning"
                                : "Info",
                        Reason =
                            $"Search history contains " +
                            $"{matchingSearches.Count} matching " +
                            "recorded searches. " +
                            $"Current critical condition: " +
                            $"{(isCritical ? "Yes" : "No")}."
                    });
                }

                // -------------------------------------------------------
                // RULE 7: FREQUENT ACTIVE-ALERT SEARCHES
                // -------------------------------------------------------

                // Only recommend alert investigation for sensors
                // that actually have an active critical alert.
                // This avoids unrelated recommendations.

                int activeAlertSearchCount = searchHistory.Count(
                    search => search.ActiveAlertsOnly);

                if (activeAlertSearchCount >= 3 &&
                    activeAlerts.Contains(mac))
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Repeated Critical Alert Monitoring",
                        Description =
                            "Operators have repeatedly searched for " +
                            "active alerts while this sensor has " +
                            "an active critical condition.",
                        RecommendedAction =
                            "Review the current alert, investigate its " +
                            "cause and confirm whether corrective " +
                            "action is required.",
                        Severity = "Warning",
                        Reason =
                            $"Search history contains " +
                            $"{activeAlertSearchCount} searches " +
                            "using the Active Alerts Only filter, " +
                            $"and sensor {mac} is in the active-alert set."
                    });
                }
            }

            // -------------------------------------------------------
            // RULE 8: FREQUENT SEARCH TERM
            // -------------------------------------------------------

            // A general recommendation is generated when operators
            // repeatedly search for the same meaningful term.
            // This is tied directly to dictionary frequency data.

            foreach (var frequentSearch in searchFrequency
                .Where(entry =>
                    entry.Key.Length >= 2 &&
                    entry.Value >= 5)
                .OrderByDescending(entry => entry.Value))
            {
                string term = frequentSearch.Key;

                // Avoid duplicating the per-sensor investigation
                // recommendation for a directly matching sensor.
                bool matchesKnownSensor = sensors.Any(sensor =>
                    sensor.MacAddress.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                    || sensor.Location.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase));

                if (matchesKnownSensor)
                {
                    continue;
                }

                recommendations.Add(new DeviceRecommendation
                {
                    Id = nextId++,
                    MacAddress = "Search Analytics",
                    Title = "Frequent Search Pattern Detected",
                    Description =
                        $"Operators searched for '{term}' " +
                        $"{frequentSearch.Value} times.",
                    RecommendedAction =
                        "Review the recurring search pattern and " +
                        "determine whether additional monitoring " +
                        "or dashboard visibility would be useful.",
                    Severity = "Info",
                    Reason =
                        $"The search-frequency dictionary records " +
                        $"{frequentSearch.Value} searches " +
                        $"for the term '{term}'."
                });
            }

            return recommendations
                .OrderBy(recommendation =>
                    recommendation.Severity == "Critical" ? 0 :
                    recommendation.Severity == "Warning" ? 1 : 2)
                .ThenBy(recommendation => recommendation.MacAddress)
                .ToList();
        }
    }
}
