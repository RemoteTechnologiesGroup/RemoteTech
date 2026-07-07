using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace RemoteTech.SimpleTypes;

[Serializable]
internal struct AntennaData() : StateManager.IStateItem
{
    public Guid Guid;
    public Guid Target;
    public bool Activated;
    public bool Powered;
    public bool Connected;
    public bool CanTarget;
    public float Dish;
    public double CosAngle = 1.0;
    public float Omni;
    public float Consumption;

    public int? Index { get; set; }
}

/// <summary>
/// Antenna properties needed by the network update.
/// </summary>
///
/// <remarks>
/// <para>
/// This class allows the network update to read your antenna state using
/// unmanaged code. You don't need to do anything special for this, just make
/// sure to call <see cref="Register"/> in <c>OnStart</c> and <see cref="Unregister"/>
/// or <see cref="Dispose"/> in <c>OnDestroy</c>.
/// </para>
/// 
/// <para>
/// You can even do this in <c>OnEnable</c> and <c>OnDisable</c>. It is safe to
/// call these multiple times as long as you don't try to register the same
/// instance multiple times at once.
/// </para>
/// </remarks>
[Serializable]
public sealed class AntennaState : IDisposable
{
    [SerializeField]
    AntennaData data;
    ulong gcHandle;

    public Guid Guid { get => data.Guid; set => data.Guid = value; }
    public Guid Target { get => data.Target; set => data.Target = value; }
    public bool Activated { get => data.Activated; set => data.Activated = value; }
    public bool Powered { get => data.Powered; set => data.Powered = value; }
    public bool Connected { get => data.Connected; set => data.Connected = value; }
    public bool CanTarget { get => data.CanTarget; set => data.CanTarget = value; }
    public float Dish { get => data.Dish; set => data.Dish = value; }
    public double CosAngle { get => data.CosAngle; set => data.CosAngle = value; }
    public float Omni { get => data.Omni; set => data.Omni = value; }
    public float Consumption { get => data.Consumption; set => data.Consumption = value; }

    public unsafe void Register()
    {
        if (gcHandle != 0)
            throw new InvalidOperationException("this state object is already registered");

        // Pin so &data stays put for the lifetime of the registration; the
        // network jobs read it through the raw pointer handed to StateManager.
        UnsafeUtility.PinGCObjectAndGetAddress(this, out gcHandle);
        fixed (AntennaData* data = &this.data)
            StateManager.Antennas.Register(data);
    }

    public unsafe void Unregister()
    {
        if (gcHandle == 0)
            return;

        fixed (AntennaData* data = &this.data)
            StateManager.Antennas.Unregister(data, gcHandle);
        gcHandle = 0;
    }

    public void Dispose() => Unregister();
}
