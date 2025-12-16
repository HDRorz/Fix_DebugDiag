using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet
{
    /// <summary>
    /// Compatibility extensions for ClrMD 0.9.2 to 1.1.2 migration
    /// </summary>
    public static class ClrMDCompatibility
    {
        /// <summary>
        /// Provides backward compatibility for the renamed EnumerateFinalizerQueue method
        /// </summary>
        /// <param name="runtime">The ClrRuntime instance</param>
        /// <returns>Enumerable of finalizer queue object addresses</returns>
        public static IEnumerable<ulong> EnumerateFinalizerQueue(this ClrRuntime runtime)
        {
            return runtime.EnumerateFinalizerQueueObjectAddresses();
        }

        /// <summary>
        /// Provides backward compatibility for the GetThreadPool method (now a property)
        /// </summary>
        /// <param name="runtime">The ClrRuntime instance</param>
        /// <returns>ClrThreadPool instance</returns>
        public static ClrThreadPool GetThreadPool(this ClrRuntime runtime)
        {
            return runtime.ThreadPool;
        }
    }
}