
using System;
using System.Collections.Generic;

namespace SmartX.Api.Services
{

    public sealed class TelemetryAvlTree
    {
        private sealed class Node
        {
            public DateTime Timestamp { get; }
            public List<TelemetryJob> Readings { get; } = new();

            public Node? Left { get; set; }
            public Node? Right { get; set; }
            public int Height { get; set; } = 1;

            public Node(TelemetryJob reading)
            {
                Timestamp = reading.Timestamp;
                Readings.Add(reading);
            }
        }

        private Node? _root;
        private int _count;

        public int Count => _count;

        private static int Height(Node? node)
            => node?.Height ?? 0;

        private static void UpdateHeight(Node node)
        {
            node.Height = 1 + Math.Max(
                Height(node.Left),
                Height(node.Right));
        }

        private static int BalanceFactor(Node node)
            => Height(node.Left) - Height(node.Right);

        private static Node RotateRight(Node y)
        {
            Node x = y.Left!;
            Node? middle = x.Right;

            x.Right = y;
            y.Left = middle;

            UpdateHeight(y);
            UpdateHeight(x);

            return x;
        }

        private static Node RotateLeft(Node x)
        {
            Node y = x.Right!;
            Node? middle = y.Left;

            y.Left = x;
            x.Right = middle;

            UpdateHeight(x);
            UpdateHeight(y);

            return y;
        }

        private static Node Balance(Node node)
        {
            UpdateHeight(node);

            int balance = BalanceFactor(node);

            if (balance > 1)
            {
                if (BalanceFactor(node.Left!) < 0)
                {
                    node.Left = RotateLeft(node.Left!);
                }

                return RotateRight(node);
            }

            if (balance < -1)
            {
                if (BalanceFactor(node.Right!) > 0)
                {
                    node.Right = RotateRight(node.Right!);
                }

                return RotateLeft(node);
            }

            return node;
        }

        public void Insert(TelemetryJob reading)
        {
            ArgumentNullException.ThrowIfNull(reading);

            _root = InsertNode(_root, reading);
            _count++;
        }

        private static Node InsertNode(
            Node? node,
            TelemetryJob reading)
        {
            if (node == null)
            {
                return new Node(reading);
            }

            int comparison =
                reading.Timestamp.CompareTo(node.Timestamp);

            if (comparison < 0)
            {
                node.Left = InsertNode(node.Left, reading);
            }
            else if (comparison > 0)
            {
                node.Right = InsertNode(node.Right, reading);
            }
            else
            {
                node.Readings.Add(reading);
                return node;
            }

            return Balance(node);
        }

        public IReadOnlyList<TelemetryJob> RangeQuery(
            DateTime startUtc,
            DateTime endUtc)
        {
            if (startUtc > endUtc)
            {
                throw new ArgumentException(
                    "Start time cannot exceed end time.");
            }

            var results = new List<TelemetryJob>();

            SearchRange(_root, startUtc, endUtc, results);

            return results;
        }

        private static void SearchRange(
            Node? node,
            DateTime startUtc,
            DateTime endUtc,
            List<TelemetryJob> results)
        {
            if (node == null)
            {
                return;
            }

            if (startUtc < node.Timestamp)
            {
                SearchRange(
                    node.Left,
                    startUtc,
                    endUtc,
                    results);
            }

            if (node.Timestamp >= startUtc &&
                node.Timestamp <= endUtc)
            {
                results.AddRange(node.Readings);
            }

            if (endUtc > node.Timestamp)
            {
                SearchRange(
                    node.Right,
                    startUtc,
                    endUtc,
                    results);
            }
        }

        public int HeightOfTree => Height(_root);
    }
}
