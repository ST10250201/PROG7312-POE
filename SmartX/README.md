# Smart-X Data Ingestion and Validation Gateway

## Project Overview

Smart-X is an IoT data ingestion and validation gateway developed using .NET 10, ASP.NET Core Minimal APIs and Blazor WebAssembly.

The application allows IoT sensors to be registered and monitored through a web-based interface. The system supports sensor validation, telemetry ingestion, anomaly status monitoring, sensor dropout detection, search and filtering, and encrypted sensor attachment storage.

## Technologies Used

- .NET 10
- C#
- ASP.NET Core Minimal API
- Blazor WebAssembly
- Bootstrap 5
- REST API
- Git and GitHub
- Visual Studio

## Solution Structure

The solution contains the following projects:

### SmartX.Api

Backend ASP.NET Core Minimal API responsible for sensor registration, telemetry ingestion, validation, sensor monitoring and encrypted attachment management.

### SmartX.Client

Blazor WebAssembly client application that provides the user interface for registering and monitoring Smart-X sensors.

### SmartX.Web

Supporting web project contained within the Smart-X solution.

---

# Setup Instructions

## 1. Prerequisites

Before running the Smart-X application, ensure the following are installed:

- Visual Studio with ASP.NET and web development support
- .NET 10 SDK
- Git

The project was developed using .NET 10.

---

## 2. Clone the Repository

Clone the GitHub repository to your computer.

Alternatively, download the repository as a ZIP file and extract it.

Open the Smart-X solution in Visual Studio.

---

## 3. Restore Dependencies

Visual Studio should automatically restore the required NuGet dependencies when the solution is opened.

Dependencies can also be restored manually.

From the repository directory, run:

```bash
dotnet restore
```

Wait for the restore process to complete successfully before compiling the application.

---

## 4. Compile the Project

The entire solution can be compiled from Visual Studio.

Select:

**Build > Build Solution**

The build should complete without errors.

Alternatively, from the repository directory run:

```bash
dotnet build
```

A successful build confirms that the API and client projects have compiled correctly.

---

## 5. Boot the Backend API

The Smart-X backend is provided by the `SmartX.Api` project.

In Visual Studio:

1. Locate `SmartX.Api` in Solution Explorer.
2. Ensure the API is included in the startup configuration.
3. Start the project using the HTTPS launch profile.

The API development addresses are configured in:

```text
SmartX.Api/Properties/launchSettings.json
```

The HTTPS API address used during development is:

```text
https://localhost:7199
```

The HTTP development address is:

```text
http://localhost:5203
```

Once the API has started, it will begin accepting requests from the Smart-X client.

The API must remain running while the client application is being used.

---

## 6. Run the Client User Interface

The user interface is provided by the `SmartX.Client` Blazor WebAssembly project.

Ensure both the API and client are configured to start.

Run the solution from Visual Studio.

The Smart-X client will open in the browser.

The development client address is:

```text
https://localhost:7239
```

The Smart-X landing page should now be displayed.

Select:

**Open Sensor Dashboard**

to access the sensor monitoring interface.

---

## 7. Register a Sensor

From the Sensor Dashboard:

1. Select **Register New Sensor**.
2. Enter the sensor MAC address.
3. Enter the deployment location.
4. Select the sensor category.
5. Submit the registration.

Example test data:

```text
MAC Address: AA:BB:CC:DD:EE:FF
Location: Greenhouse Zone A
Category: Temperature
```

After registration, the sensor will appear on the Smart-X Sensor Dashboard.

---

## 8. Sensor Dashboard

The dashboard provides:

- Total sensor count
- Normal sensor count
- Warning sensor count
- Critical sensor count
- Offline sensor count
- Sensor searching
- Category filtering
- Status filtering
- Encrypted sensor attachment information

Sensor status information is updated using telemetry received by the Smart-X API.

---

## 9. Main API Functionality

The backend provides functionality for:

- Registering IoT sensors
- Retrieving registered sensors
- Retrieving individual sensor information
- Receiving telemetry
- Maintaining telemetry history
- Detecting abnormal sensor conditions
- Detecting sensor dropouts
- Validating sensor information
- Uploading encrypted sensor attachments
- Retrieving attachment metadata

Example API routes include:

```text
GET  /api/sensors
POST /api/sensors
POST /api/telemetry
GET  /api/telemetry
GET  /api/attachments
```

---

## Troubleshooting

### Client cannot connect to the API

Confirm that `SmartX.Api` is running before attempting to use the client.

The development API should be available at:

```text
https://localhost:7199
```

### HTTPS certificate warning

Local development may require the ASP.NET Core development certificate to be trusted.

Run:

```bash
dotnet dev-certs https --trust
```

Then restart Visual Studio.

### Dependencies fail to restore

Run:

```bash
dotnet restore
```

and rebuild the solution.

### Application does not compile

Run:

```bash
dotnet clean
dotnet restore
dotnet build
```

Check the Visual Studio Error List for any remaining compilation errors.

---

## Version Control

Git and GitHub are used for source control.

The repository contains incremental commits documenting the development of the Smart-X gateway.

---

## Author

Developed for the PROG7312 Portfolio of Evidence.