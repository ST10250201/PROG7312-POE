
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartX.Api.Services
{
    public class TelemetryBackgroundWorker : BackgroundService
    {
        private readonly TelemetryProcessingService _processor;
        private readonly SensorRegistry _registry;
        private readonly ProcessingModeService _modeService;
        private readonly ILogger<TelemetryBackgroundWorker> _logger;

        public TelemetryBackgroundWorker(
            TelemetryProcessingService processor,
            SensorRegistry registry,
            ProcessingModeService modeService,
            ILogger<TelemetryBackgroundWorker> logger)
        {
            _processor = processor;
            _registry = registry;
            _modeService = modeService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromMilliseconds(500));

            try
            {
                while (await timer.WaitForNextTickAsync(
                    stoppingToken))
                {
                    // Manual mode pauses automatic queue processing.
                    if (!_modeService.AutomaticProcessingEnabled)
                    {
                        continue;
                    }

                    var processed =
                        _processor.ProcessPendingAndUpdateSensors(
                            _registry);

                    if (processed.Count > 0)
                    {
                        _logger.LogInformation(
                            "Automatically processed {Count} telemetry jobs.",
                            processed.Count);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown.
            }
        }
    }
}
