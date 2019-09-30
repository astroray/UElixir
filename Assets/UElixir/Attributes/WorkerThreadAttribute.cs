using System;

namespace UElixir
{
    /// <summary>
    /// Marks the method will be called by worker thread.
    /// Target method will be called from worker thread. It should be push the task calling Unity API to the main thread queue.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WorkerThreadAttribute : Attribute
    {
    }
}