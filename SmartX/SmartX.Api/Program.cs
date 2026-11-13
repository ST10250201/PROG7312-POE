
using SmartX.Api.Models;
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// SERVICES — PARTS 1, 2 AND 3
// -------------------------------------------------------

builder.Services.AddOpenApi();
builder.Services.AddSingleton<FileUploadService>();
builder.Services.AddSingleton<SensorRegistry>();
builder.Services.AddSingleton<TelemetryProcessingService>();
builder.Services.AddSingleton<ProcessingModeService>();
builder.Services.AddSingleton<DeviceCommandService>();
builder.Services.AddSingleton<RecommendationEngineService>();
builder.Services.AddSingleton<SearchHistoryService>();
builder.Services.AddSingleton<DeploymentZoneService>();
builder.Services.AddSingleton<RecursiveValidationService>();

// Part 3: Shared, in-memory mesh network graph.
builder.Services.AddSingleton<MeshNetworkService>();

builder.Services.AddHostedService<TelemetryBackgroundWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSmartXWeb", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowSmartXWeb");

// -------------------------------------------------------
// SMART-X IN-MEMORY DATA
// -------------------------------------------------------

var telemetryHistory = new List<TelemetryInput>();
var telemetryPackets = new List<TelemetryPacket<double>>();
var attachments = new List<SensorAttachment>();

// -------------------------------------------------------
// ROOT
// -------------------------------------------------------

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Smart-X Minimal API is running."
    });
});

// -------------------------------------------------------
// PART 1 — SENSOR REGISTRATION
// -------------------------------------------------------

app.MapGet("/api/sensors", (SensorRegistry registry) =>
{
    foreach (var sensor in registry.GetAll())
    {
        if (DateTime.UtcNow - sensor.LastSeen >
            TimeSpan.FromMinutes(2))
        {
            sensor.Status = "Offline";
        }
    }

    return Results.Ok(registry.GetAll());
});

app.MapPost(
    "/api/sensors",
    (SensorRecord sensor, SensorRegistry registry) =>
    {
        if (string.IsNullOrWhiteSpace(sensor.MacAddress))
        {
            return Results.BadRequest(new
            {
                message = "MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(sensor.Location))
        {
            return Results.BadRequest(new
            {
                message = "Deployment location is required."
            });
        }

        if (string.IsNullOrWhiteSpace(sensor.Category))
        {
            return Results.BadRequest(new
            {
                message = "Sensor category is required."
            });
        }

        if (registry.ContainsMacAddress(sensor.MacAddress))
        {
            return Results.Conflict(new
            {
                message =
                    "A sensor with this MAC address is already registered."
            });
        }

        sensor.Id = registry.GenerateNextId();
        sensor.Status = "Normal";
        sensor.LastSeen = DateTime.UtcNow;

        try
        {
            registry.Add(sensor);
        }
        catch (InvalidOperationException)
        {
            return Results.Conflict(new
            {
                message =
                    "A sensor with this MAC address is already registered."
            });
        }

        return Results.Created(
            $"/api/sensors/{sensor.Id}",
            sensor);
    });

// -------------------------------------------------------
// PART 1 + PART 2 — TELEMETRY
// -------------------------------------------------------

app.MapPost(
    "/api/telemetry",
    (
        TelemetryInput telemetry,
        SensorRegistry registry,
        TelemetryProcessingService processor) =>
    {
        SensorRecord? sensor =
            registry.FindByMacAddress(telemetry.MacAddress);

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                message = "Sensor could not be found."
            });
        }

        if (double.IsNaN(telemetry.Value) ||
            double.IsInfinity(telemetry.Value))
        {
            return Results.BadRequest(new
            {
                message = "Telemetry value must be finite."
            });
        }

        DateTime receivedAt = DateTime.UtcNow;

        telemetryHistory.Add(telemetry);

        var packet = new TelemetryPacket<double>
        {
            MacAddress = telemetry.MacAddress,
            DataType = telemetry.DataType,
            Value = telemetry.Value,
            Unit = telemetry.Unit,
            Timestamp = receivedAt,
            IsValid = true,
            ValidationMessage = "Telemetry accepted"
        };

        telemetryPackets.Add(packet);

        processor.Enqueue(
            telemetry.MacAddress,
            telemetry.DataType,
            telemetry.Value,
            telemetry.Unit);

        return Results.Ok(new
        {
            message = "Telemetry received successfully.",
            sensorStatus = sensor.Status,
            telemetry,
            packet
        });
    });

app.MapGet("/api/telemetry", () =>
{
    return Results.Ok(telemetryHistory);
});

app.MapGet("/api/telemetry/packets", () =>
{
    return Results.Ok(telemetryPackets);
});

app.MapGet("/api/telemetry/aggregate", () =>
{
    if (telemetryPackets.Count < 2)
    {
        return Results.BadRequest(new
        {
            message =
                "At least two telemetry packets are required for aggregation."
        });
    }

    TelemetryPacket<double> combined =
        telemetryPackets[0] + telemetryPackets[1];

    return Results.Ok(new
    {
        message =
            "Telemetry packets aggregated using the overloaded + operator.",
        combined
    });
});

// -------------------------------------------------------
// PART 1 — ATTACHMENTS
// -------------------------------------------------------

app.MapPost(
    "/api/attachments",
    async (
        IFormFile file,
        string sensorMacAddress,
        FileUploadService fileUploadService,
        SensorRegistry registry) =>
    {
        SensorRecord? sensor =
            registry.FindByMacAddress(sensorMacAddress);

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                message =
                    "The specified sensor could not be found."
            });
        }

        try
        {
            SensorAttachment attachment =
                await fileUploadService.SaveEncryptedAsync(
                    file,
                    sensorMacAddress);

            attachment.Id = attachments.Count == 0
                ? 1
                : attachments.Max(a => a.Id) + 1;

            attachments.Add(attachment);

            return Results.Ok(new
            {
                message =
                    "File uploaded and encrypted successfully.",
                attachment
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new
            {
                message = ex.Message
            });
        }
    })
    .DisableAntiforgery();

app.MapGet("/api/attachments", () =>
{
    return Results.Ok(attachments);
});

// -------------------------------------------------------
// PART 1 — DEPLOYMENT ZONES
// -------------------------------------------------------

app.MapGet(
    "/api/deployment-zones",
    (DeploymentZoneService zoneService) =>
    {
        string[][] deploymentZones =
        {
            new[] { "Greenhouse", "Hydroponics" },
            new[] { "Utility", "Storage" },
            new[] { "Production", "Testing" }
        };

        List<string> zoneList =
            zoneService.ConvertJaggedArrayToList(
                deploymentZones);

        return Results.Ok(new
        {
            message =
                "Jagged deployment-zone array converted to List<string>.",
            zones = zoneList
        });
    });

app.MapGet(
    "/api/validate-deployment",
    (
        DeploymentZoneService zoneService,
        RecursiveValidationService validationService) =>
    {
        string[][] deploymentZones =
        {
            new[] { "Greenhouse", "Hydroponics" },
            new[] { "Utility", "Storage" },
            new[] { "Production", "Testing" }
        };

        List<string> zones =
            zoneService.ConvertJaggedArrayToList(
                deploymentZones);

        bool isValid =
            validationService.ValidateHierarchy(zones);

        return Results.Ok(new
        {
            message =
                "Recursive deployment validation completed.",
            isValid,
            zones
        });
    });

// -------------------------------------------------------
// PART 2 — TELEMETRY PROCESSING
// -------------------------------------------------------

app.MapPost(
    "/api/processing/enqueue",
    (
        TelemetryInput telemetry,
        SensorRegistry registry,
        TelemetryProcessingService processor) =>
    {
        if (registry.FindByMacAddress(telemetry.MacAddress) == null)
        {
            return Results.NotFound(new
            {
                message = "Sensor is not registered."
            });
        }

        if (double.IsNaN(telemetry.Value) ||
            double.IsInfinity(telemetry.Value))
        {
            return Results.BadRequest(new
            {
                message = "Telemetry value must be finite."
            });
        }

        processor.Enqueue(
            telemetry.MacAddress,
            telemetry.DataType,
            telemetry.Value,
            telemetry.Unit);

        return Results.Accepted(
            "/api/processing/status",
            new
            {
                message = "Telemetry queued successfully.",
                normalQueueCount = processor.NormalQueueCount,
                criticalQueueCount = processor.CriticalQueueCount
            });
    });

app.MapGet(
    "/api/processing/status",
    (TelemetryProcessingService processor) =>
    {
        return Results.Ok(new
        {
            normalQueueCount = processor.NormalQueueCount,
            criticalQueueCount = processor.CriticalQueueCount,
            activeAlerts = processor.GetActiveAlerts()
        });
    });

app.MapPost(
    "/api/processing/next",
    (
        TelemetryProcessingService processor,
        SensorRegistry registry) =>
    {
        TelemetryJob? job = processor.ProcessNext();

        if (job == null)
        {
            return Results.Ok(new
            {
                message = "No telemetry jobs are waiting.",
                job = (TelemetryJob?)null
            });
        }

        bool updated = registry.UpdateTelemetry(
            job.MacAddress,
            job.Value,
            job.Unit,
            job.Timestamp);

        if (updated)
        {
            processor.UpdateAlertState(
                job.MacAddress,
                job.Value);
        }

        return Results.Ok(new
        {
            message = "Telemetry job processed.",
            sensorUpdated = updated,
            job
        });
    });

app.MapPost(
    "/api/processing/all",
    (
        TelemetryProcessingService processor,
        SensorRegistry registry) =>
    {
        var processed =
            processor.ProcessPendingAndUpdateSensors(registry);

        return Results.Ok(new
        {
            message = "Queued telemetry processed and sensors updated.",
            count = processed.Count,
            jobs = processed
        });
    });

app.MapGet(
    "/api/processing/history",
    (TelemetryProcessingService processor) =>
    {
        return Results.Ok(processor.GetHistory());
    });

app.MapGet(
    "/api/processing/alerts",
    (TelemetryProcessingService processor) =>
    {
        return Results.Ok(processor.GetActiveAlerts());
    });

app.MapDelete(
    "/api/processing/alerts/{macAddress}",
    (
        string macAddress,
        TelemetryProcessingService processor) =>
    {
        bool removed = processor.ClearAlert(macAddress);

        return removed
            ? Results.Ok(new { message = "Alert cleared." })
            : Results.NotFound(new { message = "Alert not found." });
    });

// -------------------------------------------------------
// PART 3 — AVL TREE HISTORICAL TELEMETRY
// -------------------------------------------------------

app.MapGet(
    "/api/telemetry/history/range",
    (
        string macAddress,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        TelemetryProcessingService processor) =>
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return Results.BadRequest(new
            {
                message = "Sensor MAC address is required."
            });
        }

        if (startUtc > endUtc)
        {
            return Results.BadRequest(new
            {
                message = "Start time cannot exceed end time."
            });
        }

        var readings = processor.GetHistoryRange(
            macAddress,
            startUtc.UtcDateTime,
            endUtc.UtcDateTime);

        return Results.Ok(new
        {
            macAddress = macAddress.Trim(),
            startUtc = startUtc.UtcDateTime,
            endUtc = endUtc.UtcDateTime,
            count = readings.Count,
            readings
        });
    });

app.MapGet(
    "/api/telemetry/history/tree-stats",
    (TelemetryProcessingService processor) =>
    {
        return Results.Ok(new
        {
            totalReadings = processor.HistoricalTelemetryCount,
            treeHeight = processor.HistoricalTreeHeight,
            structure = "AVL self-balancing binary search tree",
            note = "Only processed telemetry is indexed."
        });
    });

// -------------------------------------------------------
// PART 2 — PROCESSING MODE
// -------------------------------------------------------

app.MapGet(
    "/api/processing/mode",
    (ProcessingModeService modeService) =>
    {
        return Results.Ok(new
        {
            mode = modeService.CurrentMode,
            automaticProcessingEnabled =
                modeService.AutomaticProcessingEnabled
        });
    });

app.MapPost(
    "/api/processing/mode/automatic",
    (ProcessingModeService modeService) =>
    {
        modeService.SetAutomaticProcessing(true);

        return Results.Ok(new
        {
            message = "Automatic telemetry processing enabled.",
            mode = modeService.CurrentMode
        });
    });

app.MapPost(
    "/api/processing/mode/manual",
    (ProcessingModeService modeService) =>
    {
        modeService.SetAutomaticProcessing(false);

        return Results.Ok(new
        {
            message = "Manual telemetry processing enabled.",
            mode = modeService.CurrentMode
        });
    });

// -------------------------------------------------------
// PART 2 — DEVICE COMMANDS AND UNDO
// -------------------------------------------------------

app.MapPost(
    "/api/commands",
    (
        DeviceCommandRequest request,
        DeviceCommandService commandService,
        SensorRegistry registry) =>
    {
        if (string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return Results.BadRequest(new
            {
                error = "MAC address is required."
            });
        }

        if (registry.FindByMacAddress(request.MacAddress) == null)
        {
            return Results.NotFound(new
            {
                error = "Sensor/device MAC address not registered."
            });
        }

        try
        {
            var command = commandService.ExecuteCommand(
                request.MacAddress,
                request.DeviceType,
                request.Action);

            return Results.Ok(new
            {
                message = "Device command executed successfully.",
                command,
                undoAvailable = commandService.UndoCount > 0
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new
            {
                error = ex.Message
            });
        }
    });

app.MapPost(
    "/api/commands/undo",
    (DeviceCommandService commandService) =>
    {
        var undone = commandService.UndoLastCommand();

        if (undone == null)
        {
            return Results.Conflict(new
            {
                error = "No commands available to undo."
            });
        }

        return Results.Ok(new
        {
            message = "Most recent command undone.",
            command = undone,
            remainingUndoCount = commandService.UndoCount
        });
    });

app.MapGet(
    "/api/commands/history",
    (DeviceCommandService commandService) =>
    {
        return Results.Ok(commandService.GetHistory());
    });

app.MapGet(
    "/api/commands/states",
    (DeviceCommandService commandService) =>
    {
        return Results.Ok(commandService.GetDeviceStates());
    });

app.MapGet(
    "/api/commands/status",
    (DeviceCommandService commandService) =>
    {
        return Results.Ok(new
        {
            undoCount = commandService.UndoCount,
            undoAvailable = commandService.UndoCount > 0,
            historyCount = commandService.GetHistory().Count
        });
    });

// -------------------------------------------------------
// PART 2 — RECOMMENDATIONS
// -------------------------------------------------------

app.MapGet(
    "/api/recommendations",
    (RecommendationEngineService recommendationEngine) =>
    {
        var recommendations =
            recommendationEngine.GenerateRecommendations();

        return Results.Ok(new
        {
            count = recommendations.Count,
            generatedAt = DateTime.UtcNow,
            recommendations
        });
    });

// -------------------------------------------------------
// PART 2 — SEARCH HISTORY
// -------------------------------------------------------

app.MapPost(
    "/api/search-history",
    (
        SearchHistoryRequest request,
        SearchHistoryService searchHistoryService) =>
    {
        if (request.ResultCount < 0)
        {
            return Results.BadRequest(new
            {
                message = "Result count cannot be negative."
            });
        }

        var entry = searchHistoryService.RecordSearch(
            request.SearchTerm,
            request.Category,
            request.ActiveAlertsOnly,
            request.ResultCount);

        return Results.Created(
            $"/api/search-history/{entry.Id}",
            entry);
    });

app.MapGet(
    "/api/search-history",
    (SearchHistoryService searchHistoryService) =>
    {
        return Results.Ok(searchHistoryService.GetHistory());
    });

app.MapGet(
    "/api/search-history/frequency",
    (SearchHistoryService searchHistoryService) =>
    {
        return Results.Ok(new
        {
            totalSearches = searchHistoryService.GetTotalSearches(),
            searchFrequency = searchHistoryService.GetSearchFrequency()
        });
    });

// -------------------------------------------------------
// PART 3 — MESH NETWORK TOPOLOGY
// -------------------------------------------------------

app.MapPost(
    "/api/mesh/nodes",
    (
        MeshNodeRequest request,
        MeshNetworkService mesh,
        SensorRegistry registry) =>
    {
        if (string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return Results.BadRequest(new
            {
                message = "MAC address is required."
            });
        }

        if (!request.IsGateway &&
            registry.FindByMacAddress(request.MacAddress) == null)
        {
            return Results.NotFound(new
            {
                message =
                    "Register this sensor before adding it to the mesh."
            });
        }

        try
        {
            mesh.AddNode(
                request.MacAddress,
                request.IsGateway);

            return Results.Ok(new
            {
                message = "Mesh node added successfully.",
                topology = mesh.GetTopology()
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                message = ex.Message
            });
        }
    });

app.MapPost(
    "/api/mesh/links",
    (
        MeshLinkRequest request,
        MeshNetworkService mesh) =>
    {
        try
        {
            mesh.AddLink(
                request.FromMac,
                request.ToMac,
                request.Weight);

            return Results.Ok(new
            {
                message = "Wireless link saved successfully.",
                topology = mesh.GetTopology()
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new
            {
                message = ex.Message
            });
        }
    });

app.MapGet(
    "/api/mesh/topology",
    (MeshNetworkService mesh) =>
    {
        return Results.Ok(mesh.GetTopology());
    });

app.MapGet(
    "/api/mesh/bfs",
    (
        string startMac,
        MeshNetworkService mesh) =>
    {
        if (string.IsNullOrWhiteSpace(startMac))
        {
            return Results.BadRequest(new
            {
                message = "Starting MAC address is required."
            });
        }

        var traversal = mesh.BreadthFirstSearch(startMac);

        return Results.Ok(new
        {
            startMac,
            algorithm = "Breadth-first search",
            visitedCount = traversal.Count,
            traversal
        });
    });

app.MapGet(
    "/api/mesh/connectivity",
    (MeshNetworkService mesh) =>
    {
        return Results.Ok(mesh.GetConnectivity());
    });

app.MapGet(
    "/api/mesh/mst",
    (MeshNetworkService mesh) =>
    {
        return Results.Ok(mesh.GetMinimumSpanningTree());
    });

app.MapPut(
    "/api/mesh/nodes/status",
    (
        MeshNodeStatusRequest request,
        MeshNetworkService mesh) =>
    {
        if (string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return Results.BadRequest(new
            {
                message = "MAC address is required."
            });
        }

        try
        {
            mesh.SetNodeOffline(
                request.MacAddress,
                !request.IsOnline);

            return Results.Ok(new
            {
                message = request.IsOnline
                    ? "Mesh node restored."
                    : "Mesh node marked offline.",
                topology = mesh.GetTopology(),
                connectivity = mesh.GetConnectivity(),
                spanningTree = mesh.GetMinimumSpanningTree()
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new
            {
                message = ex.Message
            });
        }
    });

// -------------------------------------------------------
// PART 3 — GATEWAY ROUTING THROUGH PRIM'S MST
// -------------------------------------------------------

app.MapGet(
    "/api/mesh/route",
    (
        string macAddress,
        MeshNetworkService mesh) =>
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return Results.BadRequest(new
            {
                message = "Sensor MAC address is required."
            });
        }

        var route = mesh.GetRouteToGateway(macAddress);

        return Results.Ok(route);
    });

// -------------------------------------------------------
// START API
// -------------------------------------------------------

app.Run();

// -------------------------------------------------------
// REQUEST MODELS
// -------------------------------------------------------

public record DeviceCommandRequest(
    string MacAddress,
    string DeviceType,
    string Action);

public record SearchHistoryRequest(
    string SearchTerm,
    string Category,
    bool ActiveAlertsOnly,
    int ResultCount);

public record MeshNodeRequest(
    string MacAddress,
    bool IsGateway);

public record MeshLinkRequest(
    string FromMac,
    string ToMac,
    double Weight);

public record MeshNodeStatusRequest(
    string MacAddress,
    bool IsOnline);
