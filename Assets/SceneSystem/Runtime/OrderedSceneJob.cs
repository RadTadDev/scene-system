using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadTadDev.Logger;
using UnityEngine.SceneManagement;

namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Runs one transaction: the uninstalls, then the unloads, then the loads. Uninstalls run one at a time in
    /// declared order. Unloads and loads start at once, but results are consumed in declared order, so installs
    /// follow the order given.
    /// </summary>
    internal sealed class OrderedSceneJob
    {
        private readonly Func<Scene, Task> _installer;
        private readonly IReadOnlyList<SceneStep> _loads;
        private readonly ISceneTransactionObserver _observer;
        private readonly SceneTransaction _transaction;
        private readonly Func<Scene, Task> _uninstaller;
        private readonly IReadOnlyList<SceneStep> _unloads;

        public OrderedSceneJob(
            IReadOnlyList<SceneStep> unloads,
            IReadOnlyList<SceneStep> loads,
            Func<Scene, Task> installer,
            Func<Scene, Task> uninstaller,
            ISceneTransactionObserver observer,
            SceneTransaction transaction)
        {
            _unloads = unloads;
            _loads = loads;
            _installer = installer;
            _uninstaller = uninstaller;
            _observer = observer;
            _transaction = transaction;
        }

        /// <summary>
        /// Runs the transaction. Never throws; the result reports any failure.
        /// </summary>
        public async Task<bool> RunAsync()
        {
            try
            {
                foreach (var step in _unloads)
                {
                    await _uninstaller(step.LoadedScene);
                    await step.Manager.OnUninstalledAsync(step.LoadedScene);
                }

                var unloadTasks = _unloads.Select(s => s.Manager.UnloadAsync(s.Scene)).ToList();
                var loadTasks = _loads.Select(s => s.Manager.LoadAsync(s.Scene)).ToList();

                for (var i = 0; i < _unloads.Count; i++)
                {
                    await unloadTasks[i];
                    _observer.OnUnloaded(_unloads[i]);
                    _transaction.StepFinished();
                }

                for (var i = 0; i < _loads.Count; i++)
                {
                    var step = _loads[i];

                    var scene = await loadTasks[i];
                    var loaded = new SceneStep(step.Scene, step.Manager, scene);
                    _observer.OnLoaded(loaded);

                    await _installer(scene);
                    await step.Manager.OnInstalledAsync(scene);

                    _transaction.StepFinished();
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }
    }
}
