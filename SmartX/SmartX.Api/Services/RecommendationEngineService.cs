
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class RecommendationEngineService
    {
        private readonly SensorRegistry _sensorRegistry;
        private readonly TelemetryProcessingService _telemetryService;
        private readonly DeviceCommandService _commandService;

        public RecommendationEngineService(
            SensorRegistry sensorRegistry,
            TelemetryProcessingService telemetryService,
            DeviceCommandService commandService)
        {
            _sensorRegistry = sensorRegistry;
            _telemetryService = telemetryService;
            _commandService = commandService;
        }

        public List<DeviceRecommendation> GenerateRecommendations()
        {
            var recommendations = new List<DeviceRecommendation>();

            var sensors = _sensorRegistry.GetAll();
            var commands = _commandService.GetHistory();
            var deviceStates = _commandService.GetDeviceStates();

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
                        StringComparison.OrdinalIgnoreCase);

                bool isDisconnected =
                    string.Equals(
                        sensor.Status,
                        "Disconnected",
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
                            $"Sensor {mac} is reporting a critical status.",
                        RecommendedAction =
                            "Investigate the sensor and its environment.",
                        Severity = "Critical",
                        Reason =
                            "The latest sensor status is Critical."
                    });
                }

                // RULE 2: Disconnected sensor
                if (isDisconnected)
                {
                    recommendations.Add(new DeviceRecommendation
                    {
                        Id = nextId++,
                        MacAddress = mac,
                        Title = "Disconnected Sensor",
                        Description =
                            $"Sensor {mac} is currently disconnected.",
                        RecommendedAction =
                            "Check the sensor connection and network availability.",
                        Severity = "Warning",
                        Reason =
                            "The sensor registry reports a Disconnected status."
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
