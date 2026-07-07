using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace RemoteTech.Collections;

/// <summary>
/// A batched equivalent of <see cref="IJobParallelForDefer" />.
/// </summary>
[JobProducerType(typeof(IJobParallelForBatchDeferExtensions.JobData<>))]
internal interface IJobParallelForBatchDefer
{
    void Execute(int start, int count);
}

internal static class IJobParallelForBatchDeferExtensions
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    internal struct JobData<T>
        where T : struct, IJobParallelForBatchDefer
    {
        public delegate void ExecuteJobFunction(
            ref T jobData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex);

        public readonly static IntPtr JobReflectionData = JobsUtility.CreateJobReflectionData(
            typeof(T),
            typeof(T),
            JobType.ParallelFor,
            new ExecuteJobFunction(Execute));

        public static void Execute(
            ref T jobData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex)
        {
            while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var start, out var end))
            {
                jobData.Execute(start, end - start);
            }
        }
    }

    public unsafe static JobHandle Schedule<T, U>(
        this T job,
        NativeList<U> list,
        int innerloopBatchCount,
        JobHandle dependsOn = default
    )
        where T : struct, IJobParallelForBatchDefer
        where U : unmanaged
    {
        var parameters = new JobsUtility.JobScheduleParameters(
            UnsafeUtility.AddressOf(ref job),
            JobData<T>.JobReflectionData,
            dependsOn,
            ScheduleMode.Batched);

        return JobsUtility.ScheduleParallelForDeferArraySize(
            ref parameters,
            innerloopBatchCount,
            NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list),
            null);
    }
}