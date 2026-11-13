
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX.Api.Services
{
    public sealed class MeshNetworkService
    {
        private readonly object _lock = new();

        private readonly Dictionary<string, Dictionary<string, double>>
            _graph = new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _offline =
            new(StringComparer.OrdinalIgnoreCase);

        private string? _gateway;

        public void AddNode(string macAddress, bool isGateway = false)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                throw new ArgumentException("MAC address is required.");

            lock (_lock)
            {
                string mac = macAddress.Trim();

                if (!_graph.ContainsKey(mac))
                {
                    _graph[mac] =
                        new Dictionary<string, double>(
                            StringComparer.OrdinalIgnoreCase);
                }

                if (isGateway)
                    _gateway = mac;
            }
        }

        public void AddLink(
            string firstMac,
            string secondMac,
            double weight)
        {
            if (string.IsNullOrWhiteSpace(firstMac) ||
                string.IsNullOrWhiteSpace(secondMac))
                throw new ArgumentException("Both MAC addresses are required.");

            if (double.IsNaN(weight) ||
                double.IsInfinity(weight) ||
                weight <= 0)
                throw new ArgumentException("Link weight must be positive.");

            lock (_lock)
            {
                string first = firstMac.Trim();
                string second = secondMac.Trim();

                if (string.Equals(
                    first,
                    second,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Self-links are not allowed.");
                }

                if (!_graph.ContainsKey(first) ||
                    !_graph.ContainsKey(second))
                {
                    throw new InvalidOperationException(
                        "Both nodes must exist before linking.");
                }

                _graph[first][second] = weight;
                _graph[second][first] = weight;
            }
        }

        public void SetNodeOffline(string macAddress, bool offline)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                throw new ArgumentException("MAC address is required.");

            lock (_lock)
            {
                string mac = macAddress.Trim();

                if (!_graph.ContainsKey(mac))
                    throw new InvalidOperationException("Node not found.");

                if (offline)
                    _offline.Add(mac);
                else
                    _offline.Remove(mac);
            }
        }

        public IReadOnlyList<string> BreadthFirstSearch(
            string startMac)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(startMac))
                    return Array.Empty<string>();

                string start = startMac.Trim();

                if (!_graph.ContainsKey(start) ||
                    _offline.Contains(start))
                {
                    return Array.Empty<string>();
                }

                var visited = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                var queue = new Queue<string>();
                var order = new List<string>();

                visited.Add(start);
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();
                    order.Add(current);

                    foreach (string neighbour in
                        _graph[current].Keys.OrderBy(x => x))
                    {
                        if (_offline.Contains(neighbour))
                            continue;

                        if (visited.Add(neighbour))
                            queue.Enqueue(neighbour);
                    }
                }

                return order;
            }
        }

        public object GetTopology()
        {
            lock (_lock)
            {
                var nodes = _graph.Keys
                    .OrderBy(x => x)
                    .Select(mac => new
                    {
                        macAddress = mac,
                        isGateway = string.Equals(
                            mac,
                            _gateway,
                            StringComparison.OrdinalIgnoreCase),
                        isOnline = !_offline.Contains(mac)
                    })
                    .ToList();

                var links = _graph
                    .SelectMany(entry =>
                        entry.Value.Select(link => new
                        {
                            from = entry.Key,
                            to = link.Key,
                            weight = link.Value
                        }))
                    .Where(link => string.Compare(
                        link.from,
                        link.to,
                        StringComparison.OrdinalIgnoreCase) < 0)
                    .OrderBy(link => link.from)
                    .ThenBy(link => link.to)
                    .ToList();

                return new
                {
                    gateway = _gateway,
                    nodes,
                    links
                };
            }
        }

        public object GetConnectivity()
        {
            lock (_lock)
            {
                var reachable = _gateway == null
                    ? new List<string>()
                    : BreadthFirstSearch(_gateway).ToList();

                var reachableSet = new HashSet<string>(
                    reachable,
                    StringComparer.OrdinalIgnoreCase);

                var disconnected = _graph.Keys
                    .Where(mac => !reachableSet.Contains(mac))
                    .OrderBy(mac => mac)
                    .ToList();

                return new
                {
                    gateway = _gateway,
                    reachable,
                    disconnected,
                    fullyConnected = disconnected.Count == 0 &&
                        _gateway != null
                };
            }
        }

        // -------------------------------------------------------
        // PRIM'S MINIMUM SPANNING TREE
        // -------------------------------------------------------

        private List<MeshTreeEdge> BuildMinimumSpanningTree()
        {
            var edges = new List<MeshTreeEdge>();

            if (_gateway == null ||
                !_graph.ContainsKey(_gateway) ||
                _offline.Contains(_gateway))
            {
                return edges;
            }

            var visited = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                _gateway
            };

            while (true)
            {
                string? bestFrom = null;
                string? bestTo = null;
                double bestWeight = double.PositiveInfinity;

                foreach (string from in visited.OrderBy(x => x))
                {
                    foreach (var link in
                        _graph[from].OrderBy(x => x.Key))
                    {
                        if (visited.Contains(link.Key) ||
                            _offline.Contains(link.Key))
                        {
                            continue;
                        }

                        if (link.Value < bestWeight)
                        {
                            bestFrom = from;
                            bestTo = link.Key;
                            bestWeight = link.Value;
                        }
                    }
                }

                if (bestFrom == null || bestTo == null)
                    break;

                visited.Add(bestTo);

                edges.Add(new MeshTreeEdge
                {
                    From = bestFrom,
                    To = bestTo,
                    Weight = bestWeight
                });
            }

            return edges;
        }

        public object GetMinimumSpanningTree()
        {
            lock (_lock)
            {
                if (_gateway == null ||
                    !_graph.ContainsKey(_gateway) ||
                    _offline.Contains(_gateway))
                {
                    return new
                    {
                        message = "An online gateway is required.",
                        edges = new List<MeshTreeEdge>(),
                        totalWeight = 0.0,
                        connected = false
                    };
                }

                var edges = BuildMinimumSpanningTree();

                double totalWeight =
                    edges.Sum(edge => edge.Weight);

                int onlineCount = _graph.Keys
                    .Count(mac => !_offline.Contains(mac));

                return new
                {
                    message = "Prim's minimum spanning tree calculated.",
                    edges,
                    totalWeight,
                    connected = edges.Count + 1 == onlineCount
                };
            }
        }

        // -------------------------------------------------------
        // PART 3 — ROUTE TO GATEWAY USING THE MST
        // -------------------------------------------------------

        public MeshRouteResult GetRouteToGateway(string sensorMac)
        {
            lock (_lock)
            {
                var result = new MeshRouteResult
                {
                    SourceMac = sensorMac?.Trim() ?? "",
                    GatewayMac = _gateway,
                    Algorithm = "MST path using Prim's algorithm"
                };

                if (string.IsNullOrWhiteSpace(sensorMac))
                {
                    result.Message = "Sensor MAC address is required.";
                    return result;
                }

                string source = sensorMac.Trim();

                if (!_graph.ContainsKey(source))
                {
                    result.Message = "Sensor node does not exist.";
                    return result;
                }

                if (_offline.Contains(source))
                {
                    result.Message = "Source sensor is offline.";
                    return result;
                }

                if (_gateway == null ||
                    !_graph.ContainsKey(_gateway) ||
                    _offline.Contains(_gateway))
                {
                    result.Message = "No online gateway is configured.";
                    return result;
                }

                if (string.Equals(
                    source,
                    _gateway,
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Reachable = true;
                    result.Path.Add(source);
                    result.Message = "Source is the gateway.";
                    return result;
                }

                var mstEdges = BuildMinimumSpanningTree();

                var tree = new Dictionary<
                    string,
                    List<(string Neighbour, double Weight)>>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var edge in mstEdges)
                {
                    if (!tree.ContainsKey(edge.From))
                        tree[edge.From] = new();

                    if (!tree.ContainsKey(edge.To))
                        tree[edge.To] = new();

                    tree[edge.From].Add((edge.To, edge.Weight));
                    tree[edge.To].Add((edge.From, edge.Weight));
                }

                if (!tree.ContainsKey(source))
                {
                    result.Message =
                        "No MST route from this sensor to the gateway.";
                    return result;
                }

                var queue = new Queue<string>();

                var visited = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                var previous = new Dictionary<
                    string,
                    (string Parent, double Weight)>(
                        StringComparer.OrdinalIgnoreCase);

                queue.Enqueue(source);
                visited.Add(source);

                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();

                    if (string.Equals(
                        current,
                        _gateway,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (!tree.TryGetValue(current, out var neighbours))
                        continue;

                    foreach (var neighbour in neighbours)
                    {
                        if (visited.Add(neighbour.Neighbour))
                        {
                            previous[neighbour.Neighbour] =
                                (current, neighbour.Weight);

                            queue.Enqueue(neighbour.Neighbour);
                        }
                    }
                }

                if (!visited.Contains(_gateway))
                {
                    result.Message =
                        "Gateway is unreachable through the MST.";
                    return result;
                }

                var reversedPath = new List<string>();
                double totalWeight = 0;

                string step = _gateway;
                reversedPath.Add(step);

                while (!string.Equals(
                    step,
                    source,
                    StringComparison.OrdinalIgnoreCase))
                {
                    var parent = previous[step];

                    totalWeight += parent.Weight;
                    step = parent.Parent;

                    reversedPath.Add(step);
                }

                reversedPath.Reverse();

                result.Reachable = true;
                result.Path = reversedPath;
                result.HopCount = reversedPath.Count - 1;
                result.TotalWeight = totalWeight;
                result.Message =
                    "Gateway route calculated successfully.";

                return result;
            }
        }
    }

    // -------------------------------------------------------
    // PART 3 — ROUTING RESPONSE MODELS
    // -------------------------------------------------------

    public sealed class MeshTreeEdge
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public double Weight { get; set; }
    }

    public sealed class MeshRouteResult
    {
        public string SourceMac { get; set; } = "";
        public string? GatewayMac { get; set; }

        public string Algorithm { get; set; } = "";

        public bool Reachable { get; set; }

        public List<string> Path { get; set; } = new();

        public int HopCount { get; set; }

        public double TotalWeight { get; set; }

        public string Message { get; set; } = "";
    }
}
