using SmartX.Api.Models;
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<FileUploadService>();

builder.Services.AddSingleton<SensorRegistry>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSmartXWeb", policy =>
    {
        policy
            .AllowAnyOrigin()
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
// Smart-X in-memory stores
// -------------------------------------------------------

var telemetryHistory = new List<TelemetryInput>();

var telemetryPackets = new List<TelemetryPacket<double>>();

var attachments = new List<SensorAttachment>();

// -------------------------------------------------------
// Root
// -------------------------------------------------------

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Smart-X Minimal API is running."
    });
});

// -------------------------------------------------------
// SENSOR ENDPOINTS
// -------------------------------------------------------

app.MapGet(
    "/api/sensors",
    (SensorRegistry registry) =>
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

        registry.Add(sensor);

        return Results.Created(
            $"/api/sensors/{sensor.Id}",
            sensor);
    });

// -------------------------------------------------------
// TELEMETRY ENDPOINTS
// -------------------------------------------------------

app.MapPost(
    "/api/telemetry",
    (
        TelemetryInput telemetry,
        SensorRegistry registry) =>
    {
        SensorRecord? sensor =
            registry.FindByMacAddress(
                telemetry.MacAddress);

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                message = "Sensor could not be found."
            });
        }

        telemetryHistory.Add(telemetry);

        var packet = new TelemetryPacket<double>
        {
            MacAddress = telemetry.MacAddress,
            DataType = telemetry.DataType,
            Value = telemetry.Value,
            Unit = telemetry.Unit,
            Timestamp = DateTime.UtcNow,
            IsValid = true,
            ValidationMessage = "Telemetry accepted"
        };

        telemetryPackets.Add(packet);

        sensor.LastSeen = DateTime.UtcNow;

        if (telemetry.Value >= 80)
        {
            sensor.Status = "Critical";
        }
        else if (telemetry.Value >= 60)
        {
            sensor.Status = "Warning";
        }
        else
        {
            sensor.Status = "Normal";
        }

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
// ATTACHMENT ENDPOINTS
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
            registry.FindByMacAddress(
                sensorMacAddress);

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

app.Run();