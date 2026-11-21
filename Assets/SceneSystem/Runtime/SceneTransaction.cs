using System;
using System.Threading.Tasks;
using RadTadDev.Logger;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// A running scene transaction. Progress is the share of finished steps, counting unloads and loads together.
    /// </summary>
    public sealed class SceneTransaction
    {
        /// <summary>
        /// Invoked only if the transaction was created with a delay.
        /// </summary>
        public event Action<TimeSpan> OnQueued;

        /// <summary>
        /// Invoked once when this transaction begins, before any scene operation occurs.
        /// </summary>
        /// <example>
        /// This is guaranteed not to be called inline when a scene transition is created, so
        /// the following pattern will safely capture the event:
        ///
        /// var transaction = SceneSystem.Transition(...);
        /// transaction.OnBegin += MyCallback;
        /// </example>
        public event Action OnBegin;

        /// <summary>
        /// Invoked once when this transaction ends, after all scene operations finish. Reports success or failure.
        /// </summary>
        public event Action<bool> OnFinish;

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

        private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly int _totalSteps;
        private int _finishedSteps;

        internal SceneTransaction(int totalSteps)
        {
            _totalSteps = totalSteps;
        }

        internal void NotifyQueued(TimeSpan timespan)
        {
            RaiseSafely(OnQueued, timespan, "OnQueued");
        }

        internal void Begin()
        {
            RaiseSafely(OnBegin, "OnBegin");
        }

        internal void StepFinished()
        {
            _finishedSteps++;
            RaiseSafely(OnProgressChanged, Progress, "OnProgressChanged");
        }

        internal void Finish(bool success)
        {
            _completion.TrySetResult(success);
            RaiseSafely(OnFinish, success, "OnFinish");
        }

        // Each handler runs in its own try/catch so one throwing handler cannot stop the others or the transaction.
        private static void RaiseSafely(Action handlers, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception e)
                {
                    Log.Error($"An {eventName} handler threw an exception. The transaction continues.\n{e}");
                }
            }
        }

        private static void RaiseSafely<T>(Action<T> handlers, T argument, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler)(argument);
                }
                catch (Exception e)
                {
                    Log.Error($"An {eventName} handler threw an exception. The transaction continues.\n{e}");
                }
            }
        }
    }
}
