using System;
using System.Collections.Generic;
using RemoteTech.Common.Interfaces.FlightComputer.Commands;
using RemoteTech.Common.Interfaces.SignalProcessor;

namespace RemoteTech.Common.Interfaces.FlightComputer
{
    /// <summary>Current state of the flight computer.</summary>
    [Flags]
    public enum State
    {
        /// <summary>Normal state.</summary>
        Normal = 0,
        /// <summary>The flight computer (and its vessel) are packed: vessels are only packed when they come within about 300m of the active vessel.</summary>
        Packed = 2,
        /// <summary>The flight computer (and its vessel) are out of power.</summary>
        OutOfPower = 4,
        /// <summary>The flight computer (and its vessel) have no connection.</summary>
        NoConnection = 8,
        /// <summary>The flight computer signal processor is not the vessel main signal processor (see <see cref="ModuleSPU.IsMaster"/>).</summary>
        NotMaster = 16,
    }

    public interface IFlightComputer : IDisposable
    {
        /// <summary>Gets whether or not it is possible to give input to the flight computer (and consequently, to the vessel).</summary>
        bool InputAllowed { get; }

        /// <summary>Gets the delay applied to a flight computer (and hence, its vessel).</summary>
        double Delay { get; }

        /// <summary>Gets the current status of the flight computer.</summary>
        State Status { get; }

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

        /// <summary>List of autopilots for this flight computer. Used by external mods to add their own autopilots (<see cref="API"/> class).</summary>
        List<Action<FlightCtrlState>> SanctionedPilots { get; }

        /// <summary>List of commands that are currently active (not queued).</summary>
        IEnumerable<ICommand> ActiveCommands { get; }

        /// <summary>List of queued commands in the flight computer.</summary>
        IEnumerable<ICommand> QueuedCommands { get; }

        /// <summary>Called when the flight computer is disposed. This happens when the <see cref="ModuleSPU"/> is destroyed.</summary>
        new void Dispose();

        /// <summary>Enqueue a command in the flight computer command queue.</summary>
        /// <param name="cmd">The command to be enqueued.</param>
        /// <param name="ignoreControl">If true the command is not enqueued.</param>
        /// <param name="ignoreDelay">If true, the command is executed immediately, otherwise the light speed delay is applied.</param>
        /// <param name="ignoreExtra">If true, the command is executed without manual delay (if any). The normal light speed delay still applies.</param>
        void Enqueue(ICommand cmd, bool ignoreControl = false, bool ignoreDelay = false, bool ignoreExtra = false);

        void EnqueueManeuverCommand(int nodeIndex, bool ignoreControl = false, bool ignoreDelay = false, bool ignoreExtra = false);

        void EnqueueActionGroupCommand(KSPActionGroup group);

        void EnqueuePartActionCommand(BaseField baseField, object newValue);

        void EnqueueEventCommand(BaseEvent baseEvent);

        /// <summary>Looks for the passed <paramref name="node"/> on the command queue and returns true if the node is already on the list.</summary>
        /// <param name="node">Node to search in the queued commands</param>
        bool HasManeuverCommandByNode(ManeuverNode node);

        /// <summary>Restores the flight computer from the persistent save.</summary>
        /// <param name="configNode">Node with the informations for the flight computer</param>
        void Load(ConfigNode configNode);

        /// <summary>Remove a command from the flight computer command queue.</summary>
        /// <param name="cmd">The command to be removed from the command queue.</param>
        void Remove(ICommand cmd);

        /// <summary>Triggers a <see cref="CancelCommand"/> for the given <paramref name="node"/></summary>
        /// <param name="node">Node to cancel from the queue</param>
        void RemoveManeuverCommandByNode(ManeuverNode node);

        /// <summary>Abort all active commands.</summary>
        void Reset();

        /// <summary>Saves all values for the flight computer to the persistent.</summary>
        /// <param name="n">Node to save in</param>
        void Save(ConfigNode n);

        /// <summary>Called by the <see cref="ModuleSPU.OnUpdate"/> method during the Update() "Game Logic" engine phase.</summary>
        /// <remarks>This checks if there are any commands that can be removed from the FC queue if their delay has elapsed.</remarks>
        void OnUpdate();

        /// <summary>Called by the <see cref="ModuleSPU.OnFixedUpdate"/> method during the "Physics" engine phase.</summary>
        void OnFixedUpdate();

        /* HACK */
        void ShowWindow();

        /*
         * MechJeb required
         * TODO: remove that on branch 2.x
         */
         
        /// <summary>Proportional Integral Derivative vessel controller.</summary>
        IPIDController pid { get; }

        Vector3d lastAct { get; set; }

    }

}