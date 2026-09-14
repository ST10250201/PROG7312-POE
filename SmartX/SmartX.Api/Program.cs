using SmartX.Api.Models;
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// File encryption/upload service
builder.Services.AddSingleton<FileUploadService>();

// CORS allows the Blazor WebAssembly client to communicate with the API
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
// In-memory Smart-X data stores
// -------------------------------------------------------

var sensors = new List<SensorRecord>();

var telemetryHistory = new List<TelemetryInput>();

var attachments = new List<SensorAttachment>();

var telemetryPackets = new List<TelemetryPacket<double>>();

// -------------------------------------------------------
// Root endpoint
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

// Retrieve all registered sensors
app.MapGet("/api/sensors", () =>
{
    // A sensor that has not communicated for two minutes
    // is considered offline.
    foreach (var sensor in sensors)
    {
        if (DateTime.UtcNow - sensor.LastSeen >
            TimeSpan.FromMinutes(2))
        {
            sensor.Status = "Offline";
        }
    }

    return Results.Ok(sensors);
});

// Register a new sensor
app.MapPost("/api/sensors", (SensorRecord sensor) =>
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

    bool duplicateMac = sensors.Any(s =>
        s.MacAddress.Equals(
            sensor.MacAddress,
            StringComparison.OrdinalIgnoreCase));

    if (duplicateMac)
    {
        return Results.Conflict(new
        {
            message =
                "A sensor with this MAC address is already registered."
        });
    }

    sensor.Id = sensors.Count == 0
        ? 1
        : sensors.Max(s => s.Id) + 1;

    sensor.Status = "Normal";

    sensor.LastSeen = DateTime.UtcNow;

    sensors.Add(sensor);

    return Results.Created(
        $"/api/sensors/{sensor.Id}",
        sensor);
});

// -------------------------------------------------------
// TELEMETRY ENDPOINTS
// -------------------------------------------------------

// Receive telemetry from a registered sensor
app.MapPost("/api/telemetry", (TelemetryInput telemetry) =>
{
    SensorRecord? sensor = sensors.FirstOrDefault(s =>
        s.MacAddress.Equals(
            telemetry.MacAddress,
            StringComparison.OrdinalIgnoreCase));

    if (sensor == null)
    {
        return Results.NotFound(new
        {
            message = "Sensor could not be found."
        });
    }

    // Store the original telemetry request
    telemetryHistory.Add(telemetry);

    // Store the telemetry using the generic telemetry model
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

    // Update sensor activity timestamp
    sensor.LastSeen = DateTime.UtcNow;

    // Apply Smart-X anomaly status rules
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

// Retrieve telemetry history
app.MapGet("/api/telemetry", () =>
{
    return Results.Ok(telemetryHistory);
});

// Retrieve generic telemetry packets
app.MapGet("/api/telemetry/packets", () =>
{
    return Results.Ok(telemetryPackets);
});

// Demonstrates operator overloading by aggregating
// the first two telemetry packets.
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

// Upload and encrypt an attachment for a registered sensor
app.MapPost(
    "/api/attachments",
    async (
        IFormFile file,
        string sensorMacAddress,
        FileUploadService fileUploadService) =>
    {
        SensorRecord? sensor = sensors.FirstOrDefault(s =>
            s.MacAddress.Equals(
                sensorMacAddress,
                StringComparison.OrdinalIgnoreCase));

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

// Retrieve attachment metadata
app.MapGet("/api/attachments", () =>
{
    return Results.Ok(attachments);
});

// -------------------------------------------------------
// Start Smart-X API
// -------------------------------------------------------

app.Run();