
# Smart-X IoT Data Ingestion and Real-Time Processing System

## Project Overview

Smart-X is an IoT sensor management and real-time telemetry processing application developed for the PROG7312 Portfolio of Evidence.

The application is built using .NET 10, C#, ASP.NET Core Minimal APIs and Blazor WebAssembly.

Smart-X provides a web-based interface for registering IoT sensors, validating incoming telemetry, monitoring sensor conditions, processing telemetry jobs, managing simulated devices and generating contextual recommendations.

The project was developed incrementally across Part 1 and Part 2.

### Part 1: Data Ingestion and Validation Gateway

Part 1 focuses on:

- IoT sensor registration and validation
- Telemetry ingestion
- Sensor monitoring and anomaly detection
- Sensor dropout detection
- Sensor searching and filtering
- Encrypted sensor attachment management
- REST API communication

### Part 2: Real-Time Processing and Device Management

Part 2 extends Smart-X with:

- A real-time Command Stream dashboard
- FIFO telemetry processing
- Priority-based processing of critical telemetry
- Automatic and manual processing modes
- Chronological telemetry history
- Active alert tracking
- Simulated pump and relay controls
- LIFO undo functionality
- Device command audit history
- Sensor searching and filtering
- Operator search history and frequency tracking
- A rule-based predictive recommendation engine

---

## Technologies Used

- .NET 10
- C#
- ASP.NET Core Minimal API
- Blazor WebAssembly
- Bootstrap 5
- REST APIs
- Git and GitHub
- Visual Studio 2026

---

## Solution Structure

The Smart-X solution contains three projects.

### SmartX.Api

The ASP.NET Core backend responsible for:

- Sensor registration and validation
- Telemetry ingestion and processing
- Sensor status monitoring
- Processing queue management
- Device commands and undo operations
- Search history recording
- Recommendation generation
- Encrypted attachment management

### SmartX.Client

The Blazor WebAssembly frontend responsible for:

- Sensor registration and monitoring
- Telemetry submission
- Command Stream visualisation
- Search and filtering controls
- Simulated device controls
- Displaying command history
- Displaying recommendations

### SmartX.Web

The supporting web project included in the Smart-X solution.

---

# Setup Instructions

## 1. Prerequisites

Install:

- Visual Studio 2026 with ASP.NET and web development support
- .NET 10 SDK
- Git

## 2. Clone the Repository

Clone the repository:

```bash
git clone https://github.com/ST10250201/PROG7312-POE.git
```

Open the Smart-X solution in Visual Studio.

## 3. Restore Dependencies

From the solution directory:

```bash
dotnet restore
```

## 4. Build the Solution

Build using:

**Build > Build Solution**

Alternatively, run:

```bash
dotnet build
```

The solution should compile without errors.

## 5. Start the Backend API

Start the `SmartX.Api` project using its HTTPS launch profile.

Development API address:

```text
https://localhost:7199
```

The API must remain running while the client is being used.

## 6. Start the Client

Start the `SmartX.Client` project.

Development client address:

```text
https://localhost:7239
```

For normal development, configure the API and client as multiple startup projects in Visual Studio.

## 7. Register a Sensor

Open the Sensor Dashboard.

Select **Register New Sensor** and provide:

- MAC address
- Deployment location
- Sensor category

Example:

```text
MAC Address: AA:BB:CC:DD:EE:02
Location: Hydroponics Zone B
Category: Temperature
```

Submit the registration.

The sensor will appear in the dashboard and become available for telemetry processing and device management.

---

# Part 1 Functionality

## Sensor Management

The Sensor Dashboard provides sensor registration, monitoring, searching and filtering.

It displays sensor information including:

- MAC address
- Location
- Category
- Current status
- Latest telemetry information

The dashboard also provides summary counts for sensor conditions.

## Telemetry Ingestion

Smart-X accepts telemetry through its backend API.

Incoming readings are associated with registered sensors and used to update sensor information.

The client also provides a form for submitting test telemetry directly through the website.

## Anomaly and Dropout Monitoring

Smart-X identifies abnormal sensor conditions based on telemetry and monitors sensors for inactivity.

Sensor status information can include normal, warning, critical and offline conditions.

## Encrypted Sensor Attachments

Part 1 includes encrypted sensor attachment storage and attachment metadata management.

---

# Part 2: Real-Time Command Stream

## Command Stream Dashboard

The Command Stream provides a central interface for monitoring and managing real-time sensor activity.

The dashboard displays:

- Registered sensor count
- Normal processing queue count
- Critical processing queue count
- Active alert count
- Sensor telemetry information
- Processing status
- Simulated device controls
- Device command history
- Suggested actions and recommendations

The dashboard communicates with the backend API to retrieve updated information.

---

# Data Structures and Algorithms

Part 2 demonstrates the practical use of multiple data structures to manage IoT telemetry and device operations.

## 1. FIFO Queue

**Data structure:** `Queue<TelemetryJob>`

The normal telemetry queue processes standard telemetry jobs using First-In, First-Out ordering.

The first normal job added to the queue is the first normal job selected for processing, provided no critical job takes priority.

### Example

```text
Normal Job A arrives
Normal Job B arrives
Normal Job C arrives

Processing order:
A -> B -> C
```

### Purpose

FIFO ordering provides predictable processing for ordinary telemetry.

---

## 2. Priority Queue

**Data structure:** `PriorityQueue<TelemetryJob, (int, long)>`

Critical telemetry jobs are managed separately from normal telemetry jobs.

The processing service selects critical jobs before ordinary queued jobs.

Sequence information is used to preserve ordering between jobs with equivalent priority.

### Example

```text
Normal Job A arrives
Normal Job B arrives
Critical Job C arrives

Processing order:
C -> A -> B
```

### Purpose

Priority processing ensures that urgent telemetry can be handled before lower-priority work.

This is important for IoT monitoring because critical sensor conditions may require faster attention.

---

## 3. Dictionary

**Data structures:** `Dictionary<TKey, TValue>`

Smart-X uses dictionaries to manage information using unique keys.

Examples include:

- Sensor records indexed by MAC address
- Simulated device states
- Search term frequencies

### Sensor Registry

The sensor registry uses MAC addresses to identify individual sensors.

This supports efficient sensor lookup and helps prevent duplicate registrations.

### Device States

Simulated device states are associated with a sensor MAC address and device type.

Example:

```text
AA:BB:CC:DD:EE:02|PUMP -> ON
AA:BB:CC:DD:EE:02|RELAY -> OFF
```

### Search Frequency

The search history service counts repeated search terms.

Example:

```text
Zone B -> 3 searches
Temperature -> 5 searches
```

### Purpose

Dictionaries support key-based retrieval and frequency tracking.

---

## 4. Sorted Dictionary

**Data structure:** `SortedDictionary<DateTime, TelemetryJob>`

Smart-X maintains processed telemetry history in chronological order.

Telemetry jobs are organised using timestamp-based keys.

### Purpose

A sorted dictionary allows the system to retain and retrieve telemetry history in an ordered structure.

This supports the inspection of processing activity over time.

---

## 5. HashSet

**Data structure:** `HashSet<string>`

Smart-X uses a set to track active sensor alerts.

A sensor MAC address identifies an active alert.

### Example

```text
AA:BB:CC:DD:EE:02
AA:BB:CC:DD:EE:03
```

### Purpose

The set prevents duplicate entries for the same active sensor alert.

It also supports efficient membership checks when determining whether a sensor currently has an active alert.

---

## 6. Stack

**Data structure:** `Stack<DeviceCommand>`

Smart-X uses a stack to implement undo functionality for simulated device commands.

A stack follows Last-In, First-Out ordering.

### Example

```text
Command 1: Pump ON
Command 2: Relay ON
Command 3: Pump OFF

Undo order:
Command 3
Command 2
Command 1
```

### Purpose

The most recent reversible device command is undone first.

The system also records undo activity in its command history.

---

# Telemetry Processing

## Automatic Processing Mode

In automatic mode, a background worker processes queued telemetry jobs.

The worker checks for pending work and processes jobs according to the queue priority rules.

Critical jobs are selected before normal jobs.

## Manual Processing Mode

Manual mode allows telemetry processing to be controlled explicitly.

This is useful when demonstrating queue behaviour and verifying processing order.

## Critical Alert Tracking

When telemetry indicates a critical condition, the processing service tracks the sensor in its active alert set.

Alert recovery logic allows the system to update active alert information when sensor conditions change.

---

# Device Command Management

## Supported Simulated Devices

Smart-X supports:

- Pump
- Relay

The Command Stream allows users to issue simulated device commands.

## Device State Management

The backend maintains the current state of each simulated device.

Device states are associated with their sensor MAC addresses.

## LIFO Undo

The undo feature uses a stack to reverse the latest available device command.

## Command Audit History

Smart-X maintains a record of device command activity.

Recorded information includes:

- Sensor MAC address
- Device type
- Command action
- Previous state
- New state
- Timestamp
- Undo information

This provides an audit trail of simulated device operations.

---

# Sensor Search and Filtering

The Command Stream supports filtering sensors by:

- MAC address or location
- Sensor category
- Active alert status

Users can combine search and filter options to narrow the displayed sensor list.

## Search History

Smart-X provides a **Record Search** button.

When a user records a search, the system stores:

- Search term
- Selected category
- Active Alerts Only setting
- Number of matching results
- Timestamp

The search history service maintains recorded entries in memory.

## Search Frequency Tracking

A dictionary records how frequently each non-empty search term has been used.

This information is available to the recommendation engine.

---

# Predictive Recommendation Engine

Smart-X implements a rule-based recommendation engine.

The engine analyses current sensor conditions, device command history, simulated device states, active alerts and recorded search activity.

Recommendations include:

- Title
- Sensor or analytics identifier
- Description
- Recommended action
- Severity
- Supporting reason
- Generation timestamp

Recommendations are ordered by severity, with Critical recommendations displayed before Warning and Info recommendations.

## Recommendation Rules

### Rule 1: Critical Sensor Condition

**Trigger:** A sensor is in a critical condition or appears in the active-alert set.

**Recommendation:** Investigate the sensor and its environment.

### Rule 2: Disconnected Sensor

**Trigger:** A sensor is reported as disconnected or offline.

**Recommendation:** Check its connection, power supply and network availability.

### Rule 3: Review Pump Operation

**Trigger:** A temperature sensor is critical while its simulated pump is OFF.

**Recommendation:** Investigate the temperature condition and determine whether pump activation is appropriate.

### Rule 4: Repeated Pump Activation

**Trigger:** Three or more manual Pump ON commands have been recorded for a sensor.

**Recommendation:** Review recurring pump usage and consider whether automated control may be appropriate.

### Rule 5: Frequent Manual Device Control

**Trigger:** Five or more manual device commands have been recorded for a sensor.

**Recommendation:** Review repeated device adjustments and possible operating instability.

### Rule 6: Repeated Sensor Investigation

**Trigger:** Three or more recorded searches match a sensor's MAC address or deployment location.

Search terms must contain at least two characters.

**Recommendation:** Review the sensor's telemetry history and investigate repeated operator interest.

If the sensor is critical, the recommendation is assigned Warning severity.

### Rule 7: Repeated Critical Alert Monitoring

**Trigger:** At least three searches use the Active Alerts Only filter while a sensor has an active critical alert.

**Recommendation:** Investigate the active alert and determine whether corrective action is required.

### Rule 8: Frequent Search Pattern Detected

**Trigger:** A meaningful search term is recorded at least five times and does not match a known sensor's MAC address or location.

**Recommendation:** Review the recurring search pattern and consider additional monitoring or dashboard visibility.

## Recommendation Engine Design

The engine is deterministic and rule-based.

It does not randomly generate recommendations.

Each recommendation is produced from actual information stored by the Smart-X application.

The engine does not automatically operate physical devices. Device commands remain simulated and user-controlled.

---

# API Functionality

The backend exposes REST endpoints for the main Smart-X features.

Examples include:

```text
GET    /api/sensors
POST   /api/sensors

POST   /api/telemetry
GET    /api/telemetry

GET    /api/commands/history
GET    /api/commands/states
GET    /api/commands/status
POST   /api/commands
POST   /api/commands/undo

GET    /api/recommendations

POST   /api/search-history
GET    /api/search-history
GET    /api/search-history/frequency
```

Part 1 also includes API functionality for encrypted attachment management.

---

# Testing and Verification

Smart-X functionality was verified incrementally during development.

The application was built using Visual Studio and tested through the web interface and relevant API endpoints.

## Manual Test Scenarios

| Test | Expected Behaviour |
|---|---|
| Register a valid sensor | Sensor appears in the registry |
| Submit sensor telemetry | Sensor information updates |
| Submit critical telemetry | Critical condition and alert are recorded |
| Enqueue normal jobs | Normal jobs follow FIFO ordering |
| Enqueue critical telemetry | Critical jobs take priority |
| Use manual processing mode | Processing is controlled manually |
| Issue Pump ON command | Simulated pump state changes to ON |
| Issue Pump OFF command | Simulated pump state changes to OFF |
| Undo device command | Latest reversible command is undone |
| View command history | Command and undo activity is displayed |
| Search by MAC or location | Matching sensors are displayed |
| Filter by category | Only matching categories are displayed |
| Filter active alerts | Only matching active-alert sensors are displayed |
| Record a search | Search history entry is created |
| Repeat a matching search three times | Repeated Sensor Investigation recommendation appears |
| Refresh recommendations | Recommendations reflect current recorded conditions |

These scenarios document the intended verification behaviour.

## Confirmed Development Checks

During development, the following behaviours were demonstrated:

- Successful solution builds
- Sensor telemetry submission through the website
- Critical sensor recommendations
- Device state changes through Pump commands
- LIFO undo and updated recommendations
- Sensor searching and filtering
- Search history recording and retrieval
- Repeated Sensor Investigation appearing after three matching recorded searches

---

# Data Storage and Limitations

The Part 2 processing services use in-memory data structures.

This includes:

- Queued telemetry jobs
- Processed telemetry history
- Active alerts
- Simulated device states
- Device command history
- Search history and frequency counts

Part 2 runtime data is not intended to persist across backend application restarts.

The system is a demonstration of IoT processing algorithms and simulated device management.

It does not connect to or operate physical pumps and relays.

---

# Troubleshooting

## Client Cannot Connect to the API

Confirm that `SmartX.Api` is running.

Development API address:

```text
https://localhost:7199
```

## HTTPS Certificate Warning

Trust the local ASP.NET Core development certificate:

```bash
dotnet dev-certs https --trust
```

Restart Visual Studio if necessary.

## Dependencies Fail to Restore

Run:

```bash
dotnet restore
```

## Application Does Not Compile

Run these commands individually:

```bash
dotnet clean
dotnet restore
dotnet build
```

Review the Visual Studio Error List if errors remain.

## Sensor or Search History Disappears

Some Smart-X services use in-memory storage.

Restarting the backend may clear runtime records.

Register sensors and repeat the required test actions after restarting.

## Recommendations Do Not Update Immediately

Use the **Refresh Insights** control in the Command Stream.

Recommendations are generated from the current application state.

---

# Quick Start Commands

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Run the backend:

```bash
dotnet run --project SmartX.Api
```

Run the client:

```bash
dotnet run --project SmartX.Client
```

For a normal demonstration, start both projects through Visual Studio.

---

# Version Control

Git and GitHub are used to track the incremental development of Smart-X.

Repository:

https://github.com/ST10250201/PROG7312-POE

Meaningful commits document the implementation and refinement of the application's features.

---

# Author

Developed for the PROG7312 Portfolio of Evidence.

