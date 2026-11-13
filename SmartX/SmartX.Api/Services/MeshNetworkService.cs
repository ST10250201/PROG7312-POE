
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
                {
                    _gateway = mac;
                }
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
                    first, second,
                    StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Self-links are not allowed.");

                if (!_graph.ContainsKey(first) ||
                    !_graph.ContainsKey(second))
                    throw new InvalidOperationException(
                        "Both nodes must exist before linking.");

                _graph[first][second] = weight;
                _graph[second][first] = weight;
            }
        }

        public void SetNodeOffline(string macAddress, bool offline)
        {
            lock (_lock)
            {
                if (!_graph.ContainsKey(macAddress))
                    throw new InvalidOperationException("Node not found.");

                if (offline)
                    _offline.Add(macAddress);
                else
                    _offline.Remove(macAddress);
            }
        }

        public IReadOnlyList<string> BreadthFirstSearch(
            string startMac)
        {
            lock (_lock)
            {
                if (!_graph.ContainsKey(startMac) ||
                    _offline.Contains(startMac))
                    return Array.Empty<string>();

                var visited = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                var queue = new Queue<string>();

                visited.Add(startMac);
                queue.Enqueue(startMac);

                var order = new List<string>();

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
                            mac, _gateway,
                            StringComparison.OrdinalIgnoreCase),
                        isOnline = !_offline.Contains(mac)
                    })
                    .ToList();

                var links = _graph
                    .SelectMany(entry => entry.Value.Select(link => new
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
                        edges = new List<object>(),
                        totalWeight = 0.0,
                        connected = false
                    };
                }

                var visited = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    _gateway
                };

                var edges = new List<object>();
                double totalWeight = 0;

                while (true)
                {
                    string? bestFrom = null;
                    string? bestTo = null;
                    double bestWeight = double.PositiveInfinity;

                    foreach (string from in visited)
                    {
                        foreach (var link in _graph[from])
                        {
                            if (visited.Contains(link.Key) ||
                                _offline.Contains(link.Key))
                                continue;

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

                    edges.Add(new
                    {
                        from = bestFrom,
                        to = bestTo,
                        weight = bestWeight
                    });

                    totalWeight += bestWeight;
                }

                int onlineCount = _graph.Keys
                    .Count(mac => !_offline.Contains(mac));

                return new
                {
                    message = "Prim's minimum spanning tree calculated.",
                    edges,
                    totalWeight,
                    connected = visited.Count == onlineCount
                };
            }
        }
    }
}
