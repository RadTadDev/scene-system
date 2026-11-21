using System;
using System.Threading.Tasks;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// A running scene transaction. Progress is the share of finished steps, counting unloads and loads together.
    /// </summary>
    public sealed class SceneTransaction
    {
        private readonly TaskCompletionSource<bool> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly int _totalSteps;
        private int _finishedSteps;

        internal SceneTransaction(int totalSteps)
        {
            _totalSteps = totalSteps;
        }

        /// <summary>
        /// Completes once with the result of the transaction.
        /// </summary>
        public Task<bool> Completion => _completion.Task;

        /// <summary>
        /// Combined progress from 0 to 1 across every step in the transaction.
        /// </summary>
        public float Progress => _totalSteps == 0 ? 1f : (float)_finishedSteps / _totalSteps;

        /// <summary>
        /// True once the transaction has finished, whether it succeeded or not.
        /// </summary>
        public bool IsDone => _completion.Task.IsCompleted;

        /// <summary>
        /// Invoked when the value of progress changes.
        /// </summary>
        public event Action<float> OnProgressChanged;

        internal void StepFinished()
        {
            _finishedSteps++;
            OnProgressChanged?.Invoke(Progress);
        }

        internal void Finish(bool success)
        {
            _completion.TrySetResult(success);
        }
    }
}
