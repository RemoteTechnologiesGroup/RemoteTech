using System;
using System.Collections.Generic;
using RemoteTech.Common.Interfaces.FlightComputer.Commands;

namespace RemoteTech.Common.Interfaces.FlightComputer
{
    public interface IFlightComputer : IDisposable
    {
        /// <summary>Gets whether or not it is possible to give input to the flight computer (and consequently, to the vessel).</summary>
        bool InputAllowed { get; }

        /// <summary>Gets the delay applied to a flight computer (and hence, its vessel).</summary>
        double Delay { get; }

        /// <summary>Gets the current status of the flight computer.</summary>
        FlightComputer.State Status { get; }

        /// <summary>Returns true to keep the throttle on the current position without a connection, otherwise false.</summary>
        bool KeepThrottleNoConnect { get; }

        /// <summary>Gets or sets the total delay which is the usual light speed delay + any manual delay.</summary>
        double TotalDelay { get; set; }

        /// <summary>The target (<see cref="TargetCommand.Target"/>) of a <see cref="TargetCommand"/>.</summary>
        ITargetable DelayedTarget { get; set; }

        /// <summary>The vessel owning this flight computer.</summary>
        Vessel Vessel { get; }

        /// <summary>The signal processor (<see cref="ISignalProcessor"/>; <seealso cref="ModuleSPU"/>) used by this flight computer.</summary>
        ISignalProcessor SignalProcessor { get; }

        /// <summary>List of commands that are currently active (not queued).</summary>
        IEnumerable<ICommand> ActiveCommands { get; }

        /// <summary>List of queued commands in the flight computer.</summary>
        IEnumerable<ICommand> QueuedCommands { get; }

        /// <summary>Get the active Flight mode as an (<see cref="AttitudeCommand"/>).</summary>
        AttitudeCommand CurrentFlightMode { get; }

        /// <summary>Called when the flight computer is disposed. This happens when the <see cref="ModuleSPU"/> is destroyed.</summary>
        new void Dispose();

        /// <summary>Enqueue a command in the flight computer command queue.</summary>
        /// <param name="cmd">The command to be enqueued.</param>
        /// <param name="ignoreControl">If true the command is not enqueued.</param>
        /// <param name="ignoreDelay">If true, the command is executed immediately, otherwise the light speed delay is applied.</param>
        /// <param name="ignoreExtra">If true, the command is executed without manual delay (if any). The normal light speed delay still applies.</param>
        void Enqueue(ICommand cmd, bool ignoreControl = false, bool ignoreDelay = false, bool ignoreExtra = false);

        /// <summary>Remove a command from the flight computer command queue.</summary>
        /// <param name="cmd">The command to be removed from the command queue.</param>
        void Remove(ICommand cmd);

        /// <summary>Called by the <see cref="ModuleSPU.OnUpdate"/> method during the Update() "Game Logic" engine phase.</summary>
        /// <remarks>This checks if there are any commands that can be removed from the FC queue if their delay has elapsed.</remarks>
        void OnUpdate();

        /// <summary>Called by the <see cref="ModuleSPU.OnFixedUpdate"/> method during the "Physics" engine phase.</summary>
        void OnFixedUpdate();
    }

}