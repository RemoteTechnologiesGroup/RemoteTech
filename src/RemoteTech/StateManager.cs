using System;
using RemoteTech.SimpleTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace RemoteTech;

internal static unsafe class StateManager
{
    public interface IStateItem
    {
        /// <summary>
        /// The index of this item within a <see cref="PinnedStateList{TData}" />,
        /// or <c>null</c> while unregistered.
        /// </summary>
        public int? Index { get; set; }
    }

    public struct NativePointerArray<T>(NativeArray<IntPtr> array) : IDisposable
        where T : unmanaged
    {
        NativeArray<IntPtr> array = array;

        public int Length => array.Length;
        public T* this[int index] => (T*)array[index];

        public void Dispose() => array.Dispose();
        public JobHandle Dispose(JobHandle dependsOn) => array.Dispose(dependsOn);
    }

    public unsafe class PinnedStateList<TData>()
        where TData : unmanaged, IStateItem
    {
        JobHandle handle;
        NativeList<IntPtr> state = new(Allocator.Persistent);

        /// <summary>
        /// Add a pinned state object to this list.
        /// </summary>
        /// <param name="data"></param>
        public void Register(TData* data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (data->Index is not null)
                throw new ArgumentException("state is already registered");

            data->Index = state.Length;
            state.Add((IntPtr)data);
        }

        /// <summary>
        /// Remove a pinned state object from the list and release its GC handle
        /// once all current users are finished.
        /// </summary>
        public void Unregister(TData* data, ulong gcHandle)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data->Index is not int index
                || index < 0
                || index >= state.Length
                || state[index] != (IntPtr)data)
            {
                throw new InvalidOperationException("data object was not contained within this PinnedStateList");
            }

            new ReleaseGCObjectJob(gcHandle).Schedule(handle);
            state.RemoveAtSwapBack(index);
            if (index < state.Length)
                ((TData*)state[index])->Index = index;
            data->Index = null;
        }

        /// <summary>
        /// Get a snapshot of the current state array. You must call
        /// <see cref="AddUseHandle" /> with your job handle if you want to
        /// use this in a job.
        /// </summary>
        /// <param name="allocator">The allocator to use for the copy array.</param>
        /// <returns></returns>
        public NativePointerArray<TData> GetStateArray(Allocator allocator)
        {
            var array = new NativeArray<IntPtr>(
                state.Length, allocator, NativeArrayOptions.UninitializedMemory);
            array.CopyFrom(state);
            return new(array);
        }

        /// <summary>
        /// Add a <see cref="JobHandle" /> that must complete before state items
        /// can be deallocated.
        /// </summary>
        /// <param name="dependsOn"></param>
        public void AddUseHandle(JobHandle dependsOn)
        {
            if (!handle.IsCompleted)
                dependsOn = JobHandle.CombineDependencies(handle, dependsOn);
            handle = dependsOn;
        }
    }

    /// <summary>
    /// Releases a GC handle pinned via <see cref="UnsafeUtility.PinGCObjectAndGetAddress" />
    /// once the jobs still reading its pinned memory have completed.
    /// </summary>
    struct ReleaseGCObjectJob(ulong gcHandle) : IJob
    {
        public ulong gcHandle = gcHandle;

        public void Execute() => UnsafeUtility.ReleaseGCObject(gcHandle);
    }

    public static readonly PinnedStateList<AntennaData> Antennas = new();
}