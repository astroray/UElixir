using System;

namespace UElixir
{
    /// <summary>
    /// Marks property as replicable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplicableAttribute : Attribute
    {
    }
}