
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

## Part 2 Demonstration Video

The following video demonstrates the new Smart-X Part 2 functionality, including real-time telemetry processing, priority queues, device controls, command history, LIFO undo, sensor searching and filtering, and automated recommendations.

**YouTube Demonstration:** https://youtu.be/oJrowKRLNmM


---

# Part 3: Network Topology and Mesh Routing

## Overview

Part 3 completes the Smart-X IoT ecosystem by introducing historical telemetry indexing, wireless mesh network modelling, graph traversal, minimum spanning tree calculations and sensor-to-gateway routing.

The implementation integrates with the existing Part 1 sensor management system and Part 2 telemetry processing services.

The new functionality is available through the **Network Topology** page at:

`https://localhost:7239/network-topology`

The application models a simulated ESP32 mesh network. Sensor nodes are identified by their MAC addresses, wireless links have configurable weights, and a designated gateway acts as the central destination.

The implementation uses the following data structures and algorithms:

| Component | Implementation | Purpose |
|---|---|---|
| Historical telemetry | Self-balancing AVL tree | Efficient timestamp-based indexing and range queries |
| Mesh network | Weighted adjacency-list graph | Represent sensors and wireless connections |
| Connectivity | Breadth-First Search (BFS) | Identify reachable and disconnected nodes |
| Network optimisation | Prim's Minimum Spanning Tree | Minimise total selected wireless link weight |
| Gateway routing | Traversal of the calculated MST | Determine a sensor's route to the gateway |
| Node failure simulation | Offline-node tracking | Recalculate connectivity and routing when nodes fail |

## 1. AVL Tree for Historical Telemetry

### 1.1 Implementation

The historical telemetry index is implemented in:

`SmartX.Api/Services/TelemetryAvlTree.cs`

The AVL tree is integrated into:

`SmartX.Api/Services/TelemetryProcessingService.cs`

When the telemetry processing service processes a telemetry job, it inserts the processed reading into the AVL tree.

Each tree node uses a timestamp as its search key. Readings with identical timestamps can be stored together rather than overwriting one another.

This is important because multiple IoT sensors can generate telemetry at the same time.

### 1.2 Why an AVL Tree?

An ordinary binary search tree can become unbalanced when readings arrive in chronological order.

For example, inserting timestamps in increasing order can create a tree resembling a linked list.

This can degrade search performance to O(n).

An AVL tree maintains balance by checking the height difference between subtrees after insertion and performing rotations when necessary.

The supported balancing operations include:

- Left rotation
- Right rotation
- Left-right rotation
- Right-left rotation

Because the tree remains height-balanced, insertion and timestamp searches have O(log n) worst-case complexity.

### 1.3 Historical Range Queries

The AVL tree supports time-range retrieval through:

`RangeQuery(startUtc, endUtc)`

The algorithm uses the ordered timestamp keys to avoid exploring subtrees that cannot contain readings within the requested interval.

Conceptually:

```text
Search(node, start, end):

    If node is null:
        Return

    If node.timestamp >= start:
        Search(node.left, start, end)

    If start <= node.timestamp <= end:
        Include readings stored at node

    If node.timestamp <= end:
        Search(node.right, start, end)
```

The historical telemetry service then filters the returned readings by sensor MAC address.

The API endpoint is:

`GET /api/telemetry/history/range`

Example:

```text
GET /api/telemetry/history/range?macAddress=AA:BB:CC:DD:EE:02&startUtc=2026-11-13T00:00:00Z&endUtc=2026-11-14T00:00:00Z
```

The frontend provides an **AVL Historical Telemetry Search** section where users can enter:

- Sensor MAC address
- Start date and time in UTC
- End date and time in UTC

Matching readings are displayed in a table containing their timestamps, MAC addresses, data types, values and units.

### 1.4 Complexity Analysis

Let:

- n = number of distinct timestamp keys in the AVL tree
- k = number of readings returned within the requested time interval

| Operation | Complexity |
|---|---|
| AVL insertion | O(log n) |
| AVL balancing after insertion | O(log n) overall |
| Timestamp lookup | O(log n) |
| Timestamp range query | O(log n + k), assuming a balanced tree |
| Filtering returned readings by MAC | O(k) |

The current implementation uses a timestamp-based AVL index and filters by MAC address after retrieving the time interval.

Therefore, its efficiency depends on the number of readings in the selected interval, not only the number belonging to the requested sensor.

A possible future optimisation is to maintain a separate AVL index for each sensor MAC address.

### 1.5 AVL Verification

The AVL implementation was tested by submitting multiple telemetry readings through the API.

A development test produced:

```text
Total processed readings: 6
AVL tree height: 3
Structure: AVL self-balancing binary search tree
```

A historical query for sensor:

`AA:BB:CC:DD:EE:02`

returned four readings in the selected date range, including:

```text
25.5 C
21.5 C
23.8 C
27.2 C
```

The result demonstrates that multiple readings can be retrieved for the same sensor and that readings with identical displayed timestamps are retained.

The test also confirmed that the historical search is available through the Blazor frontend.

## 2. Graph-Based Mesh Network

### 2.1 Implementation

The mesh network is implemented in:

`SmartX.Api/Services/MeshNetworkService.cs`

The graph represents the simulated wireless network using an adjacency list.

Its primary structure is:

```csharp
Dictionary<string, Dictionary<string, double>>
```

The outer dictionary maps a sensor MAC address to its neighbouring nodes.

The inner dictionary maps each neighbouring MAC address to the weight of the wireless connection.

Conceptual example:

```text
Gateway:
    Sensor A -> weight 2
    Sensor B -> weight 4

Sensor A:
    Gateway  -> weight 2
    Sensor C -> weight 3

Sensor B:
    Gateway  -> weight 4
    Sensor C -> weight 1
```

Wireless links are stored in both directions, representing an undirected weighted graph.

The service prevents self-links and rejects non-positive, invalid link weights.

### 2.2 Why an Adjacency List?

An adjacency matrix requires O(V²) storage for V nodes, even when only a small proportion of nodes are directly connected.

An adjacency list requires approximately O(V + E) storage, where:

- V = number of network nodes
- E = number of wireless connections

This makes adjacency lists suitable for modelling sparse IoT networks.

Dictionary-based neighbour lookup also makes it practical to add or update wireless links.

### 2.3 Network Nodes and Gateway

Each mesh node has:

- A unique MAC address
- A gateway designation, where applicable
- An online or offline status

The designated gateway is the starting point for connectivity checks and minimum spanning tree construction.

Non-gateway mesh nodes are associated with registered Smart-X sensors.

The frontend allows users to add nodes, configure wireless links and simulate sensor failures.

## 3. Breadth-First Search (BFS)

### 3.1 Purpose

Breadth-First Search determines which sensors can currently communicate with the gateway through one or more wireless hops.

This is essential in a mesh network because a sensor does not necessarily need a direct connection to the gateway.

It may communicate through neighbouring sensors.

### 3.2 Implementation

The mesh service implements:

`BreadthFirstSearch(startMac)`

The algorithm uses:

```csharp
Queue<string>
HashSet<string>
```

The queue stores nodes awaiting exploration.

The HashSet prevents the same node from being visited repeatedly.

Conceptual pseudocode:

```text
BFS(start):

    Mark start as visited
    Add start to queue

    While queue is not empty:

        current = Remove first node from queue

        For each neighbour of current:

            If neighbour is online and unvisited:
                Mark neighbour as visited
                Add neighbour to queue
```

Offline nodes are excluded from traversal.

### 3.3 Connectivity Reporting

The connectivity API returns:

- Gateway MAC address
- Reachable nodes
- Disconnected nodes
- Overall connectivity status

Endpoint:

`GET /api/mesh/connectivity`

A node is considered reachable if BFS can find a path from the gateway through online nodes.

### 3.4 Complexity

BFS has time complexity:

**O(V + E)**

for a standard adjacency-list traversal.

The current implementation sorts neighbouring MAC addresses during traversal to produce predictable output ordering. This adds sorting overhead beyond the basic BFS complexity.

The visited set requires O(V) additional memory.

## 4. Prim's Minimum Spanning Tree

### 4.1 Purpose

A minimum spanning tree connects all reachable online nodes using a subset of available wireless links with the lowest possible total weight.

The Smart-X implementation uses Prim's algorithm.

Wireless link weights represent configurable routing costs.

Lower weights are preferred when constructing the MST.

This models a power-efficiency optimisation scenario, although actual physical energy consumption is not measured.

### 4.2 Implementation

The algorithm starts from the configured gateway.

It repeatedly selects the lowest-weight link connecting a visited node to an unvisited online node.

Conceptual pseudocode:

```text
Prim(gateway):

    Add gateway to visited nodes

    While an eligible connection exists:

        Find the minimum-weight link
        connecting a visited node
        to an unvisited online node

        Add the new node to visited

        Add the selected link to the MST

        Increase total MST weight
```

The resulting MST contains no cycles.

For a connected graph containing V online nodes, the MST contains V - 1 selected links.

### 4.3 Complexity

The current implementation scans candidate links from visited nodes to find the next minimum-weight edge.

It does not use a dedicated heap-based priority queue for Prim's algorithm.

For this implementation, a conservative upper bound is O(VE) time for the repeated edge scans, excluding additional collection overhead.

A future optimisation could use a priority queue to improve performance on large networks.

### 4.4 Gateway Routing

Smart-X also calculates a route from a selected sensor to the gateway using the edges of the current MST.

The result includes:

- Source sensor MAC address
- Gateway MAC address
- Algorithm description
- Reachability
- Ordered MAC-address path
- Hop count
- Total route weight
- Status message

Endpoint:

`GET /api/mesh/route?macAddress=AA:BB:CC:DD:EE:03`

Example route:

```text
AA:BB:CC:DD:EE:03
    ->
AA:BB:CC:DD:EE:01
    ->
AA:BB:CC:DD:EE:00
```

Example output:

```text
Reachable: True
Hop count: 2
Route weight: 5
Algorithm: MST path using Prim's algorithm
```

**Important distinction:** An MST minimises the total weight of the selected network links. The path between two nodes within an MST is not necessarily the shortest-weight path in the original graph.

For shortest individual paths, Dijkstra's algorithm would be a suitable future extension.

## 5. Sensor Failure and Mesh Recovery

The Network Topology page allows users to simulate taking sensor nodes offline and bringing them back online.

The backend maintains an offline-node set:

```csharp
HashSet<string>
```

When a sensor is marked offline:

1. Its node remains visible in the topology.
2. BFS excludes it from reachable paths.
3. The MST is recalculated using available online nodes.
4. The visualisation updates the node's status.
5. Remaining sensors can use alternative configured links where available.

This models the decision-making behaviour of a self-healing mesh network.

It does not configure real ESP32 hardware or physically re-establish wireless connections.

The system demonstrates logical recovery by recalculating available routes.

### Verified Failure Scenario

During development, a four-node mesh was configured with a gateway and three sensor nodes.

Before failure:

```text
Mesh nodes: 4
Reachable nodes: 4
Disconnected nodes: 0
MST total weight: 6
```

After taking one sensor offline:

```text
Mesh nodes: 4
Reachable nodes: 3
Disconnected nodes: 1
MST total weight: 5
```

The dashboard correctly displayed the failed node and updated its network statistics.

The simulation was also tested through the REST API.

## 6. Network Topology Dashboard

The Blazor WebAssembly frontend contains:

`SmartX.Client/Pages/NetworkTopology.razor`

The page integrates the backend's historical telemetry and mesh network features.

### Main Dashboard Features

- Total mesh node count
- Reachable node count
- Disconnected node count
- MST total weight
- Wireless mesh visualisation
- Gateway and sensor status indicators
- Weighted wireless link display
- Highlighted MST links
- BFS connectivity information
- Prim's MST edge listing
- Mesh node registration
- Wireless link configuration
- Sensor failure simulation
- Sensor-to-gateway routing
- AVL historical telemetry search

### Visualisation

The topology diagram distinguishes:

- Gateway nodes
- Online sensor nodes
- Offline sensor nodes
- Selected MST links
- Other configured wireless links

This allows users to observe how network connectivity and routing change when a node fails.

## 7. Part 3 REST API

The Part 3 backend exposes the following endpoint groups.

### Historical Telemetry

```http
GET /api/telemetry/history/range
GET /api/telemetry/history/tree-stats
```

### Mesh Network

```http
GET /api/mesh/topology
GET /api/mesh/connectivity
GET /api/mesh/mst
GET /api/mesh/route?macAddress=AA:BB:CC:DD:EE:03
```

The mesh API also supports operations for adding nodes, configuring wireless links and changing node status.

### API Testing

The backend was tested through PowerShell using `Invoke-RestMethod`.

Example:

```powershell
$base = "https://localhost:7199"

Invoke-RestMethod "$base/api/mesh/connectivity" |
    Format-List

Invoke-RestMethod "$base/api/mesh/mst" |
    Format-List

Invoke-RestMethod "$base/api/telemetry/history/tree-stats" |
    Format-List
```

The commands require the backend API to be running.

## 8. Part 3 Verification Summary

The following tests were successfully demonstrated during development.

| Test | Observed Result |
|---|---|
| Compile the updated Smart-X solution | Build succeeded |
| Add gateway and mesh sensor nodes | Nodes appeared in the topology |
| Add weighted wireless links | Links were saved and displayed |
| Calculate BFS connectivity | Reachable nodes identified |
| Calculate Prim's MST | Selected edges and total weight displayed |
| Simulate sensor failure | Offline node identified |
| Recalculate after failure | Connectivity and MST updated |
| Calculate gateway route | Ordered MAC path, hop count and weight returned |
| Submit multiple telemetry readings | Readings accepted and processed |
| Inspect AVL statistics | Six readings indexed; tree height three |
| Search historical readings by MAC and date | Four matching readings displayed |
| Navigate between application modules | Home, Sensor Telemetry, Command Stream and Network Topology accessible |

These checks confirm the core functionality tested in the local development environment.

They do not constitute a large-scale performance benchmark.

## 9. Architectural Integration

Smart-X uses a three-part architecture:

```text
Blazor WebAssembly Client
          |
          | HTTPS / REST
          v
ASP.NET Core Minimal API
          |
          +-- Sensor Registry
          |
          +-- Telemetry Processing Service
          |       |
          |       +-- FIFO Queue
          |       +-- Priority Queue
          |       +-- AVL Historical Index
          |
          +-- Device Command Service
          |       |
          |       +-- Command History
          |       +-- Undo Stack
          |
          +-- Recommendation Engine
          |
          +-- Mesh Network Service
                  |
                  +-- Weighted Graph
                  +-- BFS Connectivity
                  +-- Prim's MST
                  +-- Gateway Routing
```

The frontend communicates with the API rather than directly modifying backend data structures.

This separates user interface responsibilities from processing and algorithmic logic.

The service-based design also makes the individual algorithms easier to test and extend.

## 10. Performance and Scalability Considerations

### AVL Tree

Balanced timestamp indexing avoids the worst-case linear tree height associated with an unbalanced binary search tree.

Time-range queries prune irrelevant branches.

For future scaling, per-sensor indexes and persistent historical storage would reduce filtering overhead and preserve readings across restarts.

### Graph

An adjacency list is memory-efficient for relatively sparse networks.

It supports flexible node and link configuration.

### BFS

BFS efficiently discovers all reachable nodes from a starting gateway.

It is suitable for connectivity monitoring and failure detection.

### Prim's MST

Prim's algorithm minimises total configured link weight.

The current scanning implementation is suitable for the project's small simulated network.

A priority-queue-based implementation would be preferable for much larger deployments.

### Runtime Storage

The mesh graph and AVL history are held in application memory.

Restarting the backend clears these runtime structures.

The system therefore requires nodes, links and telemetry readings to be recreated after a restart.

A production system would require persistent storage and recovery procedures.

## 11. Limitations

Smart-X is an educational IoT management and mesh routing simulation.

Current limitations include:

- No direct communication with physical ESP32 devices
- No physical wireless signal-strength measurement
- No automatic discovery of real neighbouring devices
- No persistent storage for the Part 3 graph or AVL index
- No large-scale network performance benchmark
- MST-based routing rather than shortest-path routing
- Simulated node failures rather than hardware-level failure detection

These limitations do not prevent the implemented algorithms from demonstrating the required data structures and routing concepts.

## 12. Future Improvements

Potential improvements include:

1. Persistent telemetry storage using PostgreSQL or Azure SQL.
2. Per-sensor AVL indexes for faster sensor-specific queries.
3. A priority-queue implementation of Prim's algorithm.
4. Dijkstra's algorithm for shortest-weight individual routes.
5. Live ESP32 connectivity through MQTT.
6. Real wireless link-cost metrics such as signal quality or estimated energy usage.
7. Automated integration tests for graph and AVL edge cases.
8. A CI/CD pipeline using GitHub Actions.
9. Containerised deployment using Docker.
10. Historical telemetry charts and alert trend analysis.

## 13. Running the Complete Part 3 Application

### Requirements

- Windows with Visual Studio 2026
- .NET 10 SDK
- ASP.NET and web development workload
- Git

### Start the Projects

1. Open the Smart-X solution in Visual Studio.
2. Restore NuGet packages.
3. Build the complete solution.
4. Configure multiple startup projects.
5. Set `SmartX.Api` to **Start**.
6. Set `SmartX.Client` to **Start**.
7. Start debugging or run without debugging.

Development addresses:

```text
API:    https://localhost:7199
Client: https://localhost:7239
```

The API must remain running for the client to retrieve live data.

### Demonstration Procedure

1. Open the Smart-X home dashboard.
2. Register several sensors through Sensor Management.
3. Submit telemetry and inspect the Command Stream.
4. Open Network Topology.
5. Add a gateway node.
6. Add registered sensors as mesh nodes.
7. Configure weighted wireless links.
8. Inspect BFS connectivity and Prim's MST.
9. Calculate a selected sensor's route to the gateway.
10. Take a sensor offline and observe the updated network.
11. Restore the sensor.
12. Search historical telemetry using a sensor MAC address and UTC date range.

### Important Runtime Note

The Part 3 services use in-memory storage.

If the backend is stopped and restarted, the mesh network must be configured again and new telemetry must be submitted for historical queries.

## 14. GitHub and Submission

The Smart-X source code is maintained in GitHub:

https://github.com/ST10250201/PROG7312-POE

The repository contains the application source code and development history.

The final submission should include:

- Complete functioning Smart-X source code
- Updated `README.md`
- Final project completion report in PDF format
- Lecturer feedback incorporation record
- Required GitHub repository information
- Demonstration video if requested by the lecturer

The existing Part 2 demonstration video is available at:

https://youtu.be/oJrowKRLNmM

Part 3 demonstration video is available at: https://youtu.be/Y9Nj46hrvFQ?si=BI_k79T8lP93ZJWt 
