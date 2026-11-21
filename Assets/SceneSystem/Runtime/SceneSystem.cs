using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadTadDev.Logger;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Owns scene state. Runs one transaction at a time, rejects loads of loaded scenes and unloads of
    /// scenes that are not loaded, and reports every step to its observers. Scenes in a transaction load
    /// in parallel but install in declared order. Scenes uninstall in declared order before any unload starts.
    /// Holds no static state: pass the installer and uninstaller in.
    /// </summary>
    public sealed class SceneSystem : ISceneTransactionObserver
    {
        private readonly CancellationToken _appClosingToken;

        private readonly Func<Scene, Task> _installer;
        private readonly Dictionary<string, SceneStep> _loaded = new();
        private readonly Func<Scene, Task> _uninstaller;
        private SceneTransaction _current;

        /// <summary>
        /// Creates the scene system.
        /// </summary>
        /// <param name="installer">Installs a scene after it loads. Runs once per loaded scene, in order.</param>
        /// <param name="uninstaller">Uninstalls a scene before it unloads. Runs once per unloaded scene, in order.</param>
        /// <param name="appClosingToken">A token to cancel an in progress scene transition when the app closes.</param>
        public SceneSystem(Func<Scene, Task> installer, Func<Scene, Task> uninstaller,
            CancellationToken appClosingToken)
        {
            _installer = installer;
            _uninstaller = uninstaller;
            _appClosingToken = appClosingToken;
        }

        /// <summary>
        /// True while a transaction is running or waiting to start.
        /// </summary>
        public bool IsBusy => _current != null;

        void ISceneTransactionObserver.OnLoaded(SceneStep step)
        {
            _loaded[step.Scene.Name] = step;
        }

        void ISceneTransactionObserver.OnUnloaded(SceneStep step)
        {
            _loaded.Remove(step.Scene.Name);
        }

        /// <summary>
        /// Tells if a scene is currently loaded.
        /// </summary>
        public bool IsLoaded(SceneReference scene)
        {
            return _loaded.ContainsKey(scene.Name);
        }

        /// <summary>
        /// Runs one transaction: uninstalls and unloads first, then loads. Each unloaded scene is unloaded by the
        /// manager that loaded it.
        /// </summary>
        /// <param name="unloads">Scenes to unload. Each must be loaded. Listed in the order to run.</param>
        /// <param name="loads">Scenes to load, in install order. Each must not be loaded.</param>
        /// <param name="loadManager">The manager that loads every scene in <paramref name="loads" />.</param>
        /// <param name="unloadManager">
        /// An optional unload manager. If unspecified, the same manager used
        /// to load the scene will be used to unload it.
        /// </param>
        /// <param name="delaySeconds">Seconds to wait before the transaction starts.</param>
        /// <returns>The running transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown if busy, or if the request breaks a rule.</exception>
        public SceneTransaction Transition
        (
            IReadOnlyList<SceneReference> unloads,
            IReadOnlyList<SceneReference> loads,
            ISceneManager loadManager,
            ISceneManager unloadManager = null,
            float delaySeconds = 0
        )
        {
            if (IsBusy)
            {
                throw new InvalidOperationException("Scene system is busy. Only one transaction can run at a time.");
            }

            ValidateTransition(unloads, loads);

            var unloadSteps = unloads
                .Select(s => unloadManager == null ? _loaded[s.Name] : new SceneStep(s, unloadManager, _loaded[s.Name].LoadedScene))
                .ToList();
            var loadSteps = loads.Select(s => new SceneStep(s, loadManager)).ToList();

            var transaction = new SceneTransaction(unloadSteps.Count + loadSteps.Count);
            var job = new OrderedSceneJob(unloadSteps, loadSteps, _installer, _uninstaller, this, transaction);

            _current = transaction;
            _ = ExecuteAsync(job, transaction, delaySeconds);

            return transaction;
        }

        private async Task ExecuteAsync(OrderedSceneJob job, SceneTransaction transaction, float delaySeconds)
        {
            bool result;
            try
            {
                if (delaySeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _appClosingToken);
                }

                result = await job.RunAsync();
            }
            catch (OperationCanceledException)
            {
                Log.Error("Scene system destroyed while a transaction was waiting to start");
                result = false;
            }

            _current = null;
            transaction.Finish(result);
        }

        private void ValidateTransition(IReadOnlyList<SceneReference> unloads, IReadOnlyList<SceneReference> loads)
        {
            var unloadNames = new HashSet<string>();

            foreach (var scene in unloads)
            {
                if (!unloadNames.Add(scene.Name))
                {
                    throw new InvalidOperationException($"Scene '{scene.Name}' is listed twice in the unloads.");
                }

                if (!_loaded.ContainsKey(scene.Name))
                {
                    throw new InvalidOperationException(
                        $"Scene '{scene.Name}' is not loaded, so it cannot be unloaded.");
                }
            }

            var loadNames = new HashSet<string>();

            foreach (var scene in loads)
            {
                if (!loadNames.Add(scene.Name))
                {
                    throw new InvalidOperationException($"Scene '{scene.Name}' is listed twice in the loads.");
                }

                if (_loaded.ContainsKey(scene.Name))
                {
                    throw new InvalidOperationException($"Scene '{scene.Name}' is already loaded.");
                }
            }
        }
    }
}
