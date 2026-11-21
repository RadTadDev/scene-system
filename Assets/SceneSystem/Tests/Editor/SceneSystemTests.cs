using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RadTadDev.SceneSystem.Tests
{
    [TestFixture]
    public class SceneSystemTests
    {
        private static readonly string[] None = Array.Empty<string>();

        private FakeScenes _fakes;
        private SceneSystem _system;
        private CapturingLogSink _logs;

        [SetUp]
        public void SetUp()
        {
            // The system logs exceptions for failed transactions. Capture them rather than printing them.
            _logs = CapturingLogSink.Install();

            _fakes = new FakeScenes();
            _system = new SceneSystem(_fakes.Installer, _fakes.Uninstaller, CancellationToken.None);
        }

        [TearDown]
        public void TearDown()
        {
            CapturingLogSink.Restore();
            _fakes.Cleanup();
        }

        // ---- Helpers ----

        private async Task LoadAsync(params string[] names)
        {
            Assert.IsTrue(await WaitAsync(Start(None, names)), "Setup load failed.");
        }

        private SceneTransaction Start(string[] unloads, string[] loads, ISceneManager unloadManager = null,
            float delay = 0)
        {
            return StartOn(_system, unloads, loads, _fakes.Manager, unloadManager, delay);
        }

        private SceneTransaction StartOn(SceneSystem system, string[] unloads, string[] loads,
            ISceneManager loadManager, ISceneManager unloadManager = null, float delay = 0)
        {
            return system.Transition(
                TestScenes.Refs(unloads),
                TestScenes.Refs(loads),
                loadManager,
                unloadManager,
                delay);
        }

        private static async Task<bool> WaitAsync(SceneTransaction transaction)
        {
            var finished = await Task.WhenAny(transaction.Completion, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(transaction.Completion, finished, "Transaction did not finish in time.");
            return transaction.Completion.Result;
        }

        private bool Loaded(string name) => _system.IsLoaded(TestScenes.Ref(name));

        // ---- Loads and installs ----

        [Test]
        public async Task LoadsStartTogetherAndInstallsFollowDeclaredOrder()
        {
            var gateA = _fakes.Hold("load:A");
            var transaction = Start(None, new[] { "A", "B" });

            // Both loads start at once, before either finishes.
            CollectionAssert.AreEqual(new[] { "load:A", "load:B" }, _fakes.Events);

            // B finishes loading first, but it must still install after A.
            gateA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(
                new[] { "load:A", "load:B", "install:A", "installed:A", "install:B", "installed:B" }, _fakes.Events);
        }

        [Test]
        public async Task InstallsDoNotOverlap()
        {
            var gateInstallA = _fakes.Hold("install:A");
            var transaction = Start(None, new[] { "A", "B" });

            CollectionAssert.AreEqual(new[] { "load:A", "load:B", "install:A" }, _fakes.Events);

            gateInstallA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(
                new[] { "load:A", "load:B", "install:A", "installed:A", "install:B", "installed:B" }, _fakes.Events);
        }

        [Test]
        public async Task NextInstallWaitsForPreviousManagerInstalledHook()
        {
            var gateInstalledA = _fakes.Hold("installed:A");
            var transaction = Start(None, new[] { "A", "B" });

            CollectionAssert.AreEqual(new[] { "load:A", "load:B", "install:A", "installed:A" }, _fakes.Events);

            gateInstalledA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.Contains(_fakes.Events, "install:B");
            Assert.Less(_fakes.Events.IndexOf("installed:A"), _fakes.Events.IndexOf("install:B"));
        }

        [Test]
        public async Task SingleLoadRunsOnlyLoadAndInstallSteps()
        {
            Assert.IsTrue(await WaitAsync(Start(None, new[] { "A" })));

            CollectionAssert.AreEqual(new[] { "load:A", "install:A", "installed:A" }, _fakes.Events);
        }

        [Test]
        public async Task InstallerReceivesSceneAfterItIsRecordedAsLoaded()
        {
            var seen = new List<bool>();
            SceneSystem system = null;
            system = new SceneSystem(
                scene =>
                {
                    seen.Add(system.IsLoaded(TestScenes.Ref(FakeScenes.NameOf(scene))));
                    return Task.CompletedTask;
                },
                _fakes.Uninstaller,
                CancellationToken.None);

            Assert.IsTrue(await WaitAsync(StartOn(system, None, new[] { "A" }, _fakes.Manager)));
            CollectionAssert.AreEqual(new[] { true }, seen);
        }

        [Test]
        public async Task FailedLoadLeavesSceneNotLoaded()
        {
            _fakes.Fail("load:A");

            Assert.IsFalse(await WaitAsync(Start(None, new[] { "A" })));
            Assert.IsFalse(Loaded("A"));
        }

        [Test]
        public async Task InstallFailureStopsLaterInstalls()
        {
            _fakes.Fail("install:A");

            var transaction = Start(None, new[] { "A", "B" });

            Assert.IsFalse(await WaitAsync(transaction));
            CollectionAssert.DoesNotContain(_fakes.Events, "install:B");
        }

        [Test]
        public async Task InstallFailureLeavesFailedSceneLoaded()
        {
            // Documents the current contract: there is no rollback.
            _fakes.Fail("install:A");

            Assert.IsFalse(await WaitAsync(Start(None, new[] { "A" })));
            Assert.IsTrue(Loaded("A"));
        }

        [Test]
        public async Task LoadFailureAfterEarlierSceneKeepsThatSceneLoaded()
        {
            // Documents the current contract: there is no rollback.
            _fakes.Fail("load:B");

            Assert.IsFalse(await WaitAsync(Start(None, new[] { "A", "B" })));
            Assert.IsTrue(Loaded("A"));
            Assert.IsFalse(Loaded("B"));
            CollectionAssert.DoesNotContain(_fakes.Events, "install:B");
        }

        [Test]
        public async Task LoadFailureDoesNotInstallEarlierSceneThatIsStillLoading()
        {
            var gateA = _fakes.Hold("load:A");
            _fakes.Fail("load:B");

            var transaction = Start(None, new[] { "A", "B" });
            gateA.SetResult(true);

            Assert.IsFalse(await WaitAsync(transaction));
            // A finishes loading and installs first, because installs follow the declared order.
            CollectionAssert.AreEqual(
                new[] { "load:A", "load:B", "install:A", "installed:A" }, _fakes.Events);
        }

        [Test]
        public async Task FailedTransactionReportsZeroProgress()
        {
            _fakes.Fail("load:A");

            var transaction = Start(None, new[] { "A" });

            Assert.IsFalse(await WaitAsync(transaction));
            Assert.AreEqual(0f, transaction.Progress);
            Assert.IsTrue(transaction.IsDone);
        }

        [Test]
        public async Task SceneCanBeLoadedAgainAfterUnload()
        {
            await LoadAsync("A");
            Assert.IsTrue(await WaitAsync(Start(new[] { "A" }, None)));
            _fakes.Events.Clear();

            Assert.IsTrue(await WaitAsync(Start(None, new[] { "A" })));

            Assert.IsTrue(Loaded("A"));
            CollectionAssert.AreEqual(new[] { "load:A", "install:A", "installed:A" }, _fakes.Events);
        }

        [Test]
        public async Task SequentialTransactionsKeepLoadedStateConsistent()
        {
            await LoadAsync("A");
            await LoadAsync("B");
            Assert.IsTrue(await WaitAsync(Start(new[] { "A" }, new[] { "C" })));

            Assert.IsFalse(Loaded("A"));
            Assert.IsTrue(Loaded("B"));
            Assert.IsTrue(Loaded("C"));
        }

        [Test]
        public async Task LoadingManyScenesInOneTransactionInstallsEachOnce()
        {
            var names = new[] { "A", "B", "C", "D", "E" };

            Assert.IsTrue(await WaitAsync(Start(None, names)));

            foreach (var name in names)
            {
                Assert.IsTrue(Loaded(name));
                Assert.AreEqual(1, _fakes.Events.FindAll(e => e == "install:" + name).Count);
            }
        }

        // ---- Uninstall and unload ----

        [Test]
        public async Task UninstallsRunBeforeUnloadsInOrder()
        {
            await LoadAsync("A", "B");
            _fakes.Events.Clear();

            var transaction = Start(new[] { "A", "B" }, None);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(
                new[] { "uninstall:A", "uninstalled:A", "uninstall:B", "uninstalled:B", "unload:A", "unload:B" },
                _fakes.Events);
        }

        [Test]
        public async Task UninstallsRunOneAtATime()
        {
            await LoadAsync("A", "B");
            _fakes.Events.Clear();

            var gateUninstallA = _fakes.Hold("uninstall:A");
            var transaction = Start(new[] { "A", "B" }, None);

            CollectionAssert.AreEqual(new[] { "uninstall:A" }, _fakes.Events);

            gateUninstallA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(
                new[] { "uninstall:A", "uninstalled:A", "uninstall:B", "uninstalled:B", "unload:A", "unload:B" },
                _fakes.Events);
        }

        [Test]
        public async Task LoadsWaitForUninstalls()
        {
            await LoadAsync("A");
            _fakes.Events.Clear();

            var gateUninstallA = _fakes.Hold("uninstall:A");
            var transaction = Start(new[] { "A" }, new[] { "C" });

            // Nothing else starts until the uninstall finishes.
            CollectionAssert.AreEqual(new[] { "uninstall:A" }, _fakes.Events);

            gateUninstallA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.Contains(_fakes.Events, "load:C");
            Assert.Less(_fakes.Events.IndexOf("uninstalled:A"), _fakes.Events.IndexOf("load:C"));
            Assert.Less(_fakes.Events.IndexOf("uninstalled:A"), _fakes.Events.IndexOf("unload:A"));
        }

        [Test]
        public async Task UnloadWaitsForUninstalledHook()
        {
            await LoadAsync("A");
            _fakes.Events.Clear();

            var gateUninstalledA = _fakes.Hold("uninstalled:A");
            var transaction = Start(new[] { "A" }, None);

            CollectionAssert.AreEqual(new[] { "uninstall:A", "uninstalled:A" }, _fakes.Events);

            gateUninstalledA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(new[] { "uninstall:A", "uninstalled:A", "unload:A" }, _fakes.Events);
        }

        [Test]
        public async Task UnloadsAllStartTogether()
        {
            await LoadAsync("A", "B");
            _fakes.Events.Clear();

            var gateUnloadA = _fakes.Hold("unload:A");
            var gateUnloadB = _fakes.Hold("unload:B");
            var transaction = Start(new[] { "A", "B" }, None);

            CollectionAssert.AreEqual(
                new[] { "uninstall:A", "uninstalled:A", "uninstall:B", "uninstalled:B", "unload:A", "unload:B" },
                _fakes.Events);

            gateUnloadB.SetResult(true);
            gateUnloadA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
        }

        [Test]
        public async Task UninstallerSeesSceneStillLoaded()
        {
            var seen = new List<bool>();
            SceneSystem system = null;
            system = new SceneSystem(
                _fakes.Installer,
                scene =>
                {
                    seen.Add(system.IsLoaded(TestScenes.Ref(FakeScenes.NameOf(scene))));
                    return Task.CompletedTask;
                },
                CancellationToken.None);
            Assert.IsTrue(await WaitAsync(StartOn(system, None, new[] { "A" }, _fakes.Manager)));
            _fakes.Events.Clear();

            Assert.IsTrue(await WaitAsync(StartOn(system, new[] { "A" }, None, _fakes.Manager)));

            CollectionAssert.AreEqual(new[] { true }, seen);
            Assert.IsFalse(system.IsLoaded(TestScenes.Ref("A")));
        }

        [Test]
        public async Task UnloadFailureKeepsSceneLoaded()
        {
            await LoadAsync("A");
            _fakes.Events.Clear();
            _fakes.Fail("unload:A");

            Assert.IsFalse(await WaitAsync(Start(new[] { "A" }, None)));
            Assert.IsTrue(Loaded("A"));
        }

        [Test]
        public async Task UninstallFailureOnLaterSceneStopsBeforeAnyUnload()
        {
            await LoadAsync("A", "B");
            _fakes.Events.Clear();
            _fakes.Fail("uninstall:B");

            Assert.IsFalse(await WaitAsync(Start(new[] { "A", "B" }, None)));
            Assert.IsTrue(Loaded("A"));
            Assert.IsTrue(Loaded("B"));
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:A");
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:B");
        }

        [Test]
        public async Task UnloadingEverySceneEmptiesTheSystem()
        {
            await LoadAsync("A", "B", "C");

            Assert.IsTrue(await WaitAsync(Start(new[] { "A", "B", "C" }, None)));

            Assert.IsFalse(Loaded("A"));
            Assert.IsFalse(Loaded("B"));
            Assert.IsFalse(Loaded("C"));
        }

        [Test]
        public async Task UnloadOnlyTransactionRunsUninstallThenUnload()
        {
            await LoadAsync("A");
            _fakes.Events.Clear();

            Assert.IsTrue(await WaitAsync(Start(new[] { "A" }, None)));

            CollectionAssert.AreEqual(new[] { "uninstall:A", "uninstalled:A", "unload:A" }, _fakes.Events);
        }

        // ---- Busy state and validation ----

        [Test]
        public async Task TransitionThrowsWhileBusy()
        {
            var gateLoadA = _fakes.Hold("load:A");
            var first = Start(None, new[] { "A" });

            Assert.IsTrue(_system.IsBusy);
            Assert.Throws<InvalidOperationException>(() => Start(None, new[] { "B" }));

            gateLoadA.SetResult(true);

            Assert.IsTrue(await WaitAsync(first));
            Assert.IsFalse(_system.IsBusy);
        }

        [Test]
        public async Task IsBusyDuringDelay()
        {
            var transaction = Start(None, new[] { "A" }, delay: 0.2f);

            Assert.IsTrue(_system.IsBusy);
            Assert.IsFalse(transaction.IsDone);

            Assert.IsTrue(await WaitAsync(transaction));
            Assert.IsFalse(_system.IsBusy);
        }

        [Test]
        public async Task SystemAcceptsTransitionsAfterFinishing()
        {
            Assert.IsTrue(await WaitAsync(Start(None, new[] { "A" })));
            Assert.IsFalse(_system.IsBusy);

            Assert.IsTrue(await WaitAsync(Start(new[] { "A" }, new[] { "B" })));
            Assert.IsTrue(Loaded("B"));
        }

        [Test]
        public void TransitionThrowsWhenUnloadingSceneThatIsNotLoaded()
        {
            Assert.Throws<InvalidOperationException>(() => Start(new[] { "A" }, None));
        }

        [Test]
        public void ValidationErrorsDoNotCallAnyManager()
        {
            Assert.Throws<InvalidOperationException>(() => Start(new[] { "A" }, None));

            CollectionAssert.IsEmpty(_fakes.Events);
            Assert.IsFalse(_system.IsBusy);
        }

        [Test]
        public async Task SystemRecoversAfterValidationError()
        {
            Assert.Throws<InvalidOperationException>(() => Start(new[] { "A" }, None));

            Assert.IsTrue(await WaitAsync(Start(None, new[] { "B" })));
            Assert.IsTrue(Loaded("B"));
        }

        [Test]
        public async Task TransitionThrowsWhenLoadingSceneThatIsLoaded()
        {
            await LoadAsync("A");

            Assert.Throws<InvalidOperationException>(() => Start(None, new[] { "A" }));
        }

        [Test]
        public async Task SceneInBothUnloadsAndLoadsIsRejected()
        {
            await LoadAsync("A");

            Assert.Throws<InvalidOperationException>(() => Start(new[] { "A" }, new[] { "A" }));
        }

        [Test]
        public void TransitionThrowsForDuplicateLoads()
        {
            Assert.Throws<InvalidOperationException>(() => Start(None, new[] { "A", "A" }));
        }

        [Test]
        public async Task TransitionThrowsForDuplicateUnloads()
        {
            await LoadAsync("A");

            Assert.Throws<InvalidOperationException>(() => Start(new[] { "A", "A" }, None));
        }

        [Test]
        public void IsLoadedIsFalseForSceneNeverLoaded()
        {
            Assert.IsFalse(Loaded("NeverLoaded"));
        }

        // ---- Progress and timing ----

        [Test]
        public async Task ProgressCountsUnloadsAndLoadsTogether()
        {
            await LoadAsync("A");

            var gateUnloadA = _fakes.Hold("unload:A");
            var transaction = Start(new[] { "A" }, new[] { "B" });

            var progress = new List<float>();
            transaction.OnProgressChanged += progress.Add;

            gateUnloadA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(new[] { 0.5f, 1f }, progress);
        }

        [Test]
        public async Task ProgressReportsEachFinishedInstall()
        {
            var gateA = _fakes.Hold("load:A");
            var transaction = Start(None, new[] { "A", "B" });

            var progress = new List<float>();
            transaction.OnProgressChanged += progress.Add;
            Assert.AreEqual(0f, transaction.Progress);

            gateA.SetResult(true);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.AreEqual(new[] { 0.5f, 1f }, progress);
        }

        [Test]
        public async Task ProgressIsZeroBeforeAnyStepFinishes()
        {
            var gate = _fakes.Hold("load:A");
            var transaction = Start(None, new[] { "A" });

            Assert.AreEqual(0f, transaction.Progress);
            Assert.IsFalse(transaction.IsDone);

            gate.SetResult(true);
            Assert.IsTrue(await WaitAsync(transaction));
        }

        [Test]
        public async Task EmptyTransitionSucceedsWithFullProgress()
        {
            var transaction = Start(None, None);

            Assert.IsTrue(await WaitAsync(transaction));
            Assert.AreEqual(1f, transaction.Progress);
            Assert.IsTrue(transaction.IsDone);
        }

        [Test]
        public async Task CompletionReportsTrueOnSuccess()
        {
            var transaction = Start(None, new[] { "A" });

            Assert.IsTrue(await transaction.Completion);
            Assert.IsTrue(transaction.IsDone);
        }

        [Test]
        public async Task DelayPostponesTheStart()
        {
            var transaction = Start(None, new[] { "A" }, delay: 0.2f);

            Assert.AreEqual(0, _fakes.Events.Count);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.Contains(_fakes.Events, "load:A");
        }

        [Test]
        public async Task DelayIsAtLeastTheRequestedTime()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var transaction = Start(None, new[] { "A" }, delay: 0.2f);

            Assert.IsTrue(await WaitAsync(transaction));
            Assert.GreaterOrEqual(watch.Elapsed.TotalSeconds, 0.19);
        }

        [Test]
        public async Task AppClosingTokenCancelsDelayedTransition()
        {
            using var cancellation = new CancellationTokenSource();
            var system = new SceneSystem(_fakes.Installer, _fakes.Uninstaller, cancellation.Token);

            var transaction = StartOn(system, None, new[] { "A" }, _fakes.Manager, null, delay: 5f);
            cancellation.Cancel();

            Assert.IsFalse(await WaitAsync(transaction));
            CollectionAssert.IsEmpty(_fakes.Events);
            Assert.IsFalse(system.IsBusy);
            Assert.IsTrue(_logs.Contains("destroyed while a transaction was waiting"));
        }

        // ---- Managers ----

        [Test]
        public async Task LoadUsesOnlyTheGivenManager()
        {
            var other = new FakeScenes();

            Assert.IsTrue(await WaitAsync(StartOn(_system, None, new[] { "A" }, other.Manager)));

            CollectionAssert.Contains(other.Events, "load:A");
            CollectionAssert.DoesNotContain(_fakes.Events, "load:A");
        }

        [Test]
        public async Task UnloadUsesTheLoadingManagerByDefault()
        {
            var other = new FakeScenes();
            await LoadAsync("A");

            var transaction = _system.Transition(TestScenes.Refs("A"), TestScenes.Refs(), other.Manager);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.Contains(_fakes.Events, "unload:A");
            CollectionAssert.DoesNotContain(other.Events, "unload:A");
        }

        [Test]
        public async Task EachSceneUnloadsWithTheManagerThatLoadedIt()
        {
            var other = new FakeScenes();
            await LoadAsync("A");
            Assert.IsTrue(await WaitAsync(StartOn(_system, None, new[] { "B" }, other.Manager)));
            _fakes.Events.Clear();
            other.Events.Clear();

            Assert.IsTrue(await WaitAsync(Start(new[] { "A", "B" }, None)));

            CollectionAssert.Contains(_fakes.Events, "unload:A");
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:B");
            CollectionAssert.Contains(other.Events, "unload:B");
            CollectionAssert.DoesNotContain(other.Events, "unload:A");
        }

        // An explicit unload manager replaces the loading manager for every unload in the transaction.
        [Test]
        public async Task UnloadManagerOverrideIsUsedForUnload()
        {
            var other = new FakeScenes();
            var replacement = new FakeScenes();
            await LoadAsync("A");

            var transaction = _system.Transition(
                TestScenes.Refs("A"),
                TestScenes.Refs(),
                other.Manager,
                replacement.Manager);

            Assert.IsTrue(await WaitAsync(transaction));
            CollectionAssert.Contains(replacement.Events, "unload:A");
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:A");
        }

        [Test]
        public async Task UnloadManagerOverrideAppliesToEveryUnloadInTheTransaction()
        {
            var replacement = new FakeScenes();
            await LoadAsync("A", "B");
            _fakes.Events.Clear();

            Assert.IsTrue(await WaitAsync(_system.Transition(
                TestScenes.Refs("A", "B"), TestScenes.Refs(), _fakes.Manager, replacement.Manager)));

            CollectionAssert.Contains(replacement.Events, "unload:A");
            CollectionAssert.Contains(replacement.Events, "unload:B");
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:A");
            CollectionAssert.DoesNotContain(_fakes.Events, "unload:B");
        }
    }
}
