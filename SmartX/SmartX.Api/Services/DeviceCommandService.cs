
using System;
using System.Collections.Generic;
using System.Linq;
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class DeviceCommandService
    {
        private readonly object _lock = new();

        // Tracks the latest state of each device.
        // Key format: MAC address + device type.
        private readonly Dictionary<string, string> _deviceStates =
            new(StringComparer.OrdinalIgnoreCase);

        // LIFO stack used to undo manual commands.
        private readonly Stack<DeviceCommand> _undoStack = new();

        // Complete chronological command history.
        private readonly List<DeviceCommand> _history = new();

        private int _nextCommandId = 1;

        public int UndoCount
        {
            get
            {
                lock (_lock)
                {
                    return _undoStack.Count;
                }
            }
        }

        public IReadOnlyList<DeviceCommand> GetHistory()
        {
            lock (_lock)
            {
                return _history
                    .OrderByDescending(command => command.Timestamp)
                    .ThenByDescending(command => command.Id)
                    .ToList();
            }
        }

        public IReadOnlyDictionary<string, string> GetDeviceStates()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(
                    _deviceStates,
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public string GetDeviceState(
            string macAddress,
            string deviceType)
        {
            string key = BuildDeviceKey(macAddress, deviceType);

            lock (_lock)
            {
                return _deviceStates.TryGetValue(
                    key,
                    out string? state)
                    ? state
                    : "OFF";
            }
        }

        public DeviceCommand ExecuteCommand(
            string macAddress,
            string deviceType,
            string action)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                throw new ArgumentException(
                    "A MAC address is required.");
            }

            string normalisedDeviceType =
                NormaliseDeviceType(deviceType);

            string normalisedAction =
                NormaliseAction(action);

            string key = BuildDeviceKey(
                macAddress,
                normalisedDeviceType);

            lock (_lock)
            {
                string previousState =
                    _deviceStates.TryGetValue(
                        key,
                        out string? currentState)
                        ? currentState
                        : "OFF";

                if (previousState == normalisedAction)
                {
                    throw new InvalidOperationException(
                        $"The {normalisedDeviceType} is already " +
                        $"{normalisedAction}.");
                }

                var command = new DeviceCommand
                {
                    Id = _nextCommandId++,
                    MacAddress = macAddress.Trim().ToUpperInvariant(),
                    DeviceType = normalisedDeviceType,
                    Action = normalisedAction,
                    PreviousState = previousState,
                    NewState = normalisedAction,
                    Timestamp = DateTime.UtcNow,
                    IsUndo = false
                };

                _deviceStates[key] = normalisedAction;

                _history.Add(command);

                _undoStack.Push(command);

                return command;
            }
        }

        public DeviceCommand? UndoLastCommand()
        {
            lock (_lock)
            {
                if (_undoStack.Count == 0)
                {
                    return null;
                }

                DeviceCommand originalCommand =
                    _undoStack.Pop();

                string key = BuildDeviceKey(
                    originalCommand.MacAddress,
                    originalCommand.DeviceType);

                string currentState =
                    _deviceStates.TryGetValue(
                        key,
                        out string? state)
                        ? state
                        : "OFF";

                var undoCommand = new DeviceCommand
                {
                    Id = _nextCommandId++,
                    MacAddress = originalCommand.MacAddress,
                    DeviceType = originalCommand.DeviceType,
                    Action = originalCommand.PreviousState,
                    PreviousState = currentState,
                    NewState = originalCommand.PreviousState,
                    Timestamp = DateTime.UtcNow,
                    IsUndo = true,
                    UndoesCommandId = originalCommand.Id
                };

                _deviceStates[key] = originalCommand.PreviousState;

                // Keep a permanent audit record of the undo.
                _history.Add(undoCommand);

                // Undo operations are not pushed back onto the stack.
                return undoCommand;
            }
        }

        private static string BuildDeviceKey(
            string macAddress,
            string deviceType)
        {
            return $"{macAddress.Trim().ToUpperInvariant()}|" +
                   $"{NormaliseDeviceType(deviceType)}";
        }

        private static string NormaliseDeviceType(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
            {
                throw new ArgumentException(
                    "A device type is required.");
            }

            string normalised =
                deviceType.Trim().ToUpperInvariant();

            if (normalised != "PUMP" &&
                normalised != "RELAY")
            {
                throw new ArgumentException(
                    "Device type must be Pump or Relay.");
            }

            return normalised;
        }

        private static string NormaliseAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentException(
                    "An action is required.");
            }

            string normalised =
                action.Trim().ToUpperInvariant();

            if (normalised != "ON" &&
                normalised != "OFF")
            {
                throw new ArgumentException(
                    "Action must be ON or OFF.");
            }

            return normalised;
        }
    }
}
