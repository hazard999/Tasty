﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xenial.Delicious.Utils
{
    internal static class PromiseHelper
    {
        internal static Task Promise(Action<Action, Action> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);
            Action reject = () => tsc.SetCanceled();

            functor(resolve, reject);

            return tsc.Task;
        }

        internal static async Task Promise(Func<Action, Action, Task> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);
            Action reject = () => tsc.SetCanceled();

            await functor(resolve, reject);

            await tsc.Task;
        }

        internal static Task<T> Promise<T>(Action<Action<T>, Action> functor)
        {
            var tsc = new TaskCompletionSource<T>();

            Action<T> resolve = (val) => tsc.SetResult(val);
            Action reject = () => tsc.SetCanceled();

            functor(resolve, reject);

            return tsc.Task;
        }

        internal static Task<T> Promise<T>(Action<Action<T>, Action<CancellationToken>> functor)
        {
            var tsc = new TaskCompletionSource<T>();

            Action<T> resolve = (val) => tsc.SetResult(val);
            Action<CancellationToken> reject = (token) => tsc.TrySetCanceled(token);

            functor(resolve, reject);

            return tsc.Task;
        }

        internal static Task Promise(Action<Action, Action<CancellationToken>> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);
            Action<CancellationToken> reject = (token) => tsc.TrySetCanceled(token);

            functor(resolve, reject);

            return tsc.Task;
        }

        public static Task<T> Promise<T>(Action<Action<T>> functor)
        {
            var tsc = new TaskCompletionSource<T>();

            Action<T> resolve = (val) => tsc.SetResult(val);

            functor(resolve);

            return tsc.Task;
        }

        internal static async Task Promise(Func<Action, Task> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);

            await functor(resolve);

            await tsc.Task;
        }

        internal static Task Promise(Action<Action> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);

            functor(resolve);

            return tsc.Task;
        }

        internal static Task Promise(Action<Action, Action<Exception>> functor)
        {
            var tsc = new TaskCompletionSource<bool>();

            Action resolve = () => tsc.SetResult(true);
            Action<Exception> reject = (ex) => tsc.SetException(ex);

            functor(resolve, reject);

            return tsc.Task;
        }
    }
}
