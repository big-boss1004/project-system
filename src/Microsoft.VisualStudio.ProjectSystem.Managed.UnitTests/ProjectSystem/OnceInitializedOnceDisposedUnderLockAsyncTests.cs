﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class OnceInitializedOnceDisposedUnderLockAsyncTests
    {
        [Fact]
        public void ExecuteUnderLockAsync_NullAsAction_ThrowsArgumentNullException()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>(() =>
            {
                instance.ExecuteUnderLockAsync((Func<CancellationToken, Task>)null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_PassesCancellationTokenToAction()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var instance = CreateInstance();

            bool result = default;
            await instance.ExecuteUnderLockAsync(ct => 
            {
                cancellationTokenSource.Cancel();

                result = ct.IsCancellationRequested;

                return Task.CompletedTask;

            }, cancellationTokenSource.Token);

            Assert.True(result);
        }

        [Fact]
        public void ExecuteUnderLockAsync_WhenPassedCancelledToken_DoesNotExecuteAction()
        {
            var cancellationToken = new CancellationToken(canceled: true);

            var instance = CreateInstance();

            bool called = false;
            var result = instance.ExecuteUnderLockAsync(ct => { called = true; return Task.CompletedTask; }, cancellationToken);

            Assert.ThrowsAsync<TaskCanceledException>(() => result);
            Assert.False(called);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_WithNoContention_ExecutesAction()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync((ct) => { callCount++; return Task.CompletedTask; }, CancellationToken.None);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_AvoidsOverlappingActions()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var secondEntered = new AsyncManualResetEvent();

            var instance = CreateInstance();

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease;

            }, CancellationToken.None);

            Task secondAction() => Task.Run(() => instance.ExecuteUnderLockAsync((ct) =>
            {
                secondEntered.Set();
                return Task.CompletedTask;

            }, CancellationToken.None));

            await AssertNoOverlap(firstAction, secondAction, firstEntered, firstRelease, secondEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_CanBeNested()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync(async (_) =>
            {
                await instance.ExecuteUnderLockAsync(async (__) =>
                {
                    await instance.ExecuteUnderLockAsync((___) =>
                    {
                        callCount++;
                        return Task.CompletedTask;
                    });
                });
            });

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_AvoidsOverlappingWithDispose()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var disposeEntered = new AsyncManualResetEvent();

            ConcreteOnceInitializedOnceDisposedUnderLockAsync instance = null;

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease.WaitAsync();

            }, CancellationToken.None);

            instance = CreateInstance(() =>
            {
                disposeEntered.Set();
                return Task.CompletedTask;
            });

            Task disposeAction() => Task.Run(instance.DisposeAsync);

            await AssertNoOverlap(firstAction, disposeAction, firstEntered, firstRelease, disposeEntered);
        }

        [Fact]
        public async Task DisposeAsync_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_WhenDisposed_ThrowsOperationCancellated()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return instance.ExecuteUnderLockAsync((ct) => { return Task.CompletedTask; }, CancellationToken.None);
            });
        }

        private async Task AssertNoOverlap(Func<Task> firstAction, Func<Task> secondAction, AsyncManualResetEvent firstEntered, AsyncManualResetEvent firstRelease, AsyncManualResetEvent secondEntered)
        {
            // Run first task and wait until we've entered it
            var firstTask = firstAction();
            await firstEntered.WaitAsync();

            // Run second task, we should never enter it
            var secondTask = secondAction();
            await Assert.ThrowsAsync<TimeoutException>(() => secondEntered.WaitAsync().WithTimeout(TimeSpan.FromMilliseconds(50)));

            // Now release first
            firstRelease.Set();

            // Now we should enter first one
            await secondEntered.WaitAsync();
            await Task.WhenAll(firstTask, secondTask);
        }

        private static ConcreteOnceInitializedOnceDisposedUnderLockAsync CreateInstance(Func<Task> disposed = null)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();

            return new ConcreteOnceInitializedOnceDisposedUnderLockAsync(threadingService.JoinableTaskContext, disposed);
        }

        private class ConcreteOnceInitializedOnceDisposedUnderLockAsync : OnceInitializedOnceDisposedUnderLockAsync
        {
            private readonly Func<Task> _disposed;

            public ConcreteOnceInitializedOnceDisposedUnderLockAsync(JoinableTaskContextNode joinableTaskContextNode, Func<Task> disposed) 
                : base(joinableTaskContextNode)
            {
                if (disposed == null)
                    disposed = () => Task.CompletedTask;

                _disposed = disposed;
            }

            public new Task ExecuteUnderLockAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
            {
                return base.ExecuteUnderLockAsync(action, cancellationToken);
            }

            protected override Task DisposeCoreUnderLockAsync(bool initialized)
            {
                return _disposed();
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
