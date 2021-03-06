﻿using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Threading;
using NUnit.Framework;

namespace Tests.WebView {

    public class CommonTests : WebViewTestBase {

        [Test(Description = "Attached listeners are called")]
        public async Task ListenersAreCalled() {
            await Run(async () => {
                var listener1Counter = 0;
                var listener2Counter = 0;
                var taskCompletionSourceListener1 = new TaskCompletionSource<bool>();
                var taskCompletionSourceListener21 = new TaskCompletionSource<bool>();
                var taskCompletionSourceListener22 = new TaskCompletionSource<bool>();

                var listener1 = TargetView.AttachListener("event1_name");
                listener1.Handler += delegate {
                    listener1Counter++;
                    taskCompletionSourceListener1.SetResult(true);
                };

                var listener2 = TargetView.AttachListener("event2_name");
                listener2.Handler += delegate {
                    listener2Counter++;
                    taskCompletionSourceListener21.SetResult(true);
                };
                listener2.Handler += delegate {
                    listener2Counter++;
                    taskCompletionSourceListener22.SetResult(true);
                };

                await Load($"<html><script>{listener1}{listener2}</script><body></body></html>");
                Task.WaitAll(taskCompletionSourceListener1.Task, taskCompletionSourceListener21.Task, taskCompletionSourceListener22.Task);

                Assert.AreEqual(1, listener1Counter);
                Assert.AreEqual(2, listener2Counter);
            });
        }

        [Test(Description = "Attached listeners are called in Dispatcher thread")]
        public async Task ListenersAreCalledInDispatcherThread() {
            await Run(async () => {
                var taskCompletionSource = new TaskCompletionSource<bool>();

                var listener = TargetView.AttachListener("event_name");
                listener.UIHandler += delegate {
                    taskCompletionSource.SetResult(Dispatcher.UIThread.CheckAccess());
                };

                await Load($"<html><script>{listener}</script><body></body></html>");
                var canAccessDispatcher = await taskCompletionSource.Task;

                Assert.IsTrue(canAccessDispatcher, "Listeners are not being called in dispatcher thread!");
            });
        }

        [Test(Description = "Unhandled Exception event is called when an async unhandled error occurs inside a listener")]
        public async Task UnhandledExceptionEventIsCalledOnListenerError() {
            await Run(() => {
                var taskCompletionSource = new TaskCompletionSource<Exception>();
                const string ExceptionMessage = "hey";

                WithUnhandledExceptionHandling(async () => {
                    var listener = TargetView.AttachListener("event_name");
                    listener.Handler += delegate {
                        throw new Exception(ExceptionMessage);
                    };

                    await Load($"<html><script>{listener}</script><body></body></html>");
                    var exception = await taskCompletionSource.Task;
                    StringAssert.Contains(ExceptionMessage, exception.Message);
                },
                e => {
                    taskCompletionSource.SetResult(e);
                    return true;
                });
            });
        }

        [Test(Description = "Before navigate hook is called")]
        public async Task BeforeNavigateHookCalled() {
            await Run(async () => {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                TargetView.BeforeNavigate += (request) => {
                    taskCompletionSource.SetResult(true);
                    request.Cancel();
                };
                TargetView.Address = "https://www.google.com";
                var beforeNavigateCalled = await taskCompletionSource.Task;
                Assert.IsTrue(beforeNavigateCalled, "BeforeNavigate hook was not called!");
            });
        }

        [Test(Description = "Javascript evaluation on navigated event does not block")]
        public async Task JavascriptEvaluationOnNavigatedDoesNotBlock() {
            await Run(async () => {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                TargetView.Navigated += delegate {
                    TargetView.EvaluateScript<int>("1+1");
                    taskCompletionSource.SetResult(true);
                };
                await Load("<html><body></body></html>");
                var navigatedCalled = await taskCompletionSource.Task;
                Assert.IsTrue(navigatedCalled, "JS evaluation on navigated event is blocked!");
            });
        }

        [Test(Description = "Tests that the webview is disposed when host window is not shown")]
        public async Task WebViewIsDisposedWhenHostWindowIsNotShown() {
            await Run(async () => {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                var view = new WebViewControl.WebView();
                view.Disposed += delegate { 
                    taskCompletionSource.SetResult(true); 
                };

                var window = new Window { Title = CurrentTestName };

                try {
                    window.Content = view;
                    window.Close();

                    var disposed = await taskCompletionSource.Task;
                    Assert.IsTrue(disposed);
                } finally {
                    window.Close();
                }
            });
        }

        [Test(Description = "Tests that the webview is disposed when host window is not shown")]
        public async Task WebViewIsNotDisposedWhenUnloaded() {
            await Run(async () => {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                var view = new WebViewControl.WebView();
                view.Disposed += delegate { 
                    taskCompletionSource.SetResult(true); 
                };

                var window = new Window {
                    Title = CurrentTestName,
                    Content = view
                };

                try {
                    window.Show();
                    window.Content = null;
                    Assert.IsFalse(taskCompletionSource.Task.IsCompleted);

                    window.Content = view;
                    window.Close();
                    var disposed = await taskCompletionSource.Task;
                    Assert.IsTrue(disposed);
                } finally {
                    window.Close();
                }
            });
        }
    }
}
