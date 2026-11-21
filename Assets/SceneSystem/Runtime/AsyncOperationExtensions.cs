using System.Threading.Tasks;
using UnityEngine;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Extensions for <see cref="AsyncOperation" />s.
    /// </summary>
    internal static class AsyncOperationExtensions
    {
        /// <summary>
        /// Waits for a Unity async operation to finish.
        /// </summary>
        public static async Task AsTaskAsync(this AsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
    }
}
