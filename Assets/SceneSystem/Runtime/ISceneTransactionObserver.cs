namespace RadTadDev.SceneSystem
{
    /// <summary>
    /// Receives the state changes a transaction makes.
    /// </summary>
    internal interface ISceneTransactionObserver
    {
        /// <summary>
        /// The scene finished loading. It may not be installed yet.
        /// </summary>
        /// <param name="step">The step that was loaded.</param>
        void OnLoaded(SceneStep step);

        /// <summary>
        /// The scene finished unloading.
        /// </summary>
        /// <param name="step">The step that was unloaded.</param>
        void OnUnloaded(SceneStep step);
    }
}
