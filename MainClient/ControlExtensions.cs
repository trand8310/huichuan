

namespace MainClient
{
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(control, enable, null);
        }
        /// <summary>
        /// Executes the Action asynchronously on the UI thread, does not block execution on the calling thread.
        /// </summary>
        /// <param name="control">the control for which the update is required</param>
        /// <param name="action">action to be performed on the control</param>
        public static void InvokeOnUiThreadIfRequired(this Control control, Action action)
        {
            //If you are planning on using a similar function in your own code then please be sure to
            //have a quick read over https://stackoverflow.com/questions/1874728/avoid-calling-invoke-when-the-control-is-disposed
            //No action
            if (control.Disposing || control.IsDisposed || !control.IsHandleCreated)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                control.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }


        public static Task UiInvokeAsync(
 this Control control,
 Action action,
 CancellationToken cancellationToken = default)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var tcs = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationTokenRegistration ctr = default;

            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            void CompleteWithDispose()
            {
                ctr.Dispose();
                tcs.TrySetException(new ObjectDisposedException(control.Name));
            }

            void Execute()
            {
                try
                {
                    if (control.IsDisposed || control.Disposing)
                    {
                        CompleteWithDispose();
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ctr.Dispose();
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    action();

                    ctr.Dispose();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    ctr.Dispose();
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (control.IsDisposed || control.Disposing)
                {
                    CompleteWithDispose();
                    return tcs.Task;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    ctr.Dispose();
                    tcs.TrySetCanceled(cancellationToken);
                    return tcs.Task;
                }

                if (!control.IsHandleCreated)
                {
                    ctr.Dispose();
                    tcs.TrySetException(
                        new InvalidOperationException("控件句柄尚未创建，不能调用 BeginInvoke。"));
                    return tcs.Task;
                }

                if (control.InvokeRequired)
                {
                    control.BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                ctr.Dispose();
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public static Task<T> UiInvokeAsync<T>(
            this Control control,
            Func<T> func,
            CancellationToken cancellationToken = default)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (func == null) throw new ArgumentNullException(nameof(func));

            var tcs = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationTokenRegistration ctr = default;

            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            void CompleteWithDispose()
            {
                ctr.Dispose();
                tcs.TrySetException(new ObjectDisposedException(control.Name));
            }

            void Execute()
            {
                try
                {
                    if (control.IsDisposed || control.Disposing)
                    {
                        CompleteWithDispose();
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ctr.Dispose();
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    var result = func();

                    ctr.Dispose();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    ctr.Dispose();
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (control.IsDisposed || control.Disposing)
                {
                    CompleteWithDispose();
                    return tcs.Task;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    ctr.Dispose();
                    tcs.TrySetCanceled(cancellationToken);
                    return tcs.Task;
                }

                if (!control.IsHandleCreated)
                {
                    ctr.Dispose();
                    tcs.TrySetException(
                        new InvalidOperationException("控件句柄尚未创建，不能调用 BeginInvoke。"));
                    return tcs.Task;
                }

                if (control.InvokeRequired)
                {
                    control.BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                ctr.Dispose();
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public static async Task<bool> TryUiInvokeAsync(
            this Control control,
            Action action,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await control.UiInvokeAsync(action, cancellationToken)
                             .ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
