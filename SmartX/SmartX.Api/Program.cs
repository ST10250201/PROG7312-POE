using SmartX.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<SmartX.Api.Services.FileUploadService>();

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

var sensors = new List<SensorRecord>();
var telemetryHistory = new List<TelemetryInput>();
var attachments = new List<SensorAttachment>();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Smart-X Minimal API is running."
    });
});

app.MapGet("/api/sensors", () =>
{
    foreach (var sensor in sensors)
    {
        if (DateTime.UtcNow - sensor.LastSeen > TimeSpan.FromMinutes(2))
        {
            sensor.Status = "Offline";
        }
    }

    return Results.Ok(sensors);
});

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
            message = "A sensor with this MAC address is already registered."
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

    telemetryHistory.Add(telemetry);

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
        telemetry
    });
});

app.MapGet("/api/telemetry", () =>
{
    return Results.Ok(telemetryHistory);
});

app.MapPost("/api/attachments",
    async (
        IFormFile file,
        string sensorMacAddress,
        SmartX.Api.Services.FileUploadService fileUploadService) =>
    {
        SensorRecord? sensor = sensors.FirstOrDefault(s =>
            s.MacAddress.Equals(
                sensorMacAddress,
                StringComparison.OrdinalIgnoreCase));

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                message = "The specified sensor could not be found."
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
                message = "File uploaded and encrypted successfully.",
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