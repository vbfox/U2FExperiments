using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static PInvoke.Kernel32;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace BlackFox.Win32.Kernel32
{
    public static class Kernel32Dll
    {
        static unsafe bool DeviceIoControlCore(
            SafeObjectHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned,
            OVERLAPPED? lpOverlapped)
        {
            using (var overlapped = lpOverlapped.Pin())
            {
                return Kernell32DllNativeMethods.DeviceIoControl(hDevice, dwIoControlCode, inBuffer, nInBufferSize,
                    outBuffer, nOutBufferSize, out pBytesReturned, (OVERLAPPED*)overlapped.Pointer);
            }
        }

        public static bool DeviceIoControl(
            SafeObjectHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            OVERLAPPED lpOverlapped)
        {
            int pBytesReturned;
            return DeviceIoControlCore(hDevice, dwIoControlCode, inBuffer, nInBufferSize, outBuffer, nOutBufferSize,
                out pBytesReturned, lpOverlapped);
        }

        public static bool DeviceIoControl(
            SafeObjectHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned)
        {
            return DeviceIoControlCore(hDevice, dwIoControlCode, inBuffer, nInBufferSize, outBuffer, nOutBufferSize,
                out pBytesReturned, null);
        }

        const int ErrorIoPending = 997;

        static void ThrowLastWin32Exception()
        {
            throw new Win32Exception();
        }

        static void SetFromLastWin32Exception<T>(TaskCompletionSource<T> tcs)
        {
            try
            {
                ThrowLastWin32Exception();
            }
            catch (Win32Exception exception)
            {
                tcs.SetException(exception);
            }
        }

        unsafe delegate bool OverlappedMethod(OVERLAPPED* overlapped);

        static unsafe Task<int> OverlappedAsync(SafeObjectHandle handle, OverlappedMethod nativeMethod,
            CancellationToken cancellationToken)
        {
            var finishedEvent = new ManualResetEvent(false);

            var overlapped = new OVERLAPPED
            {
                hEvent = finishedEvent.SafeWaitHandle.DangerousGetHandle()
            }.Pin();

            var result = nativeMethod((OVERLAPPED*)overlapped.Pointer);

            var completionSource = new TaskCompletionSource<int>();

            var finishedSynchronously = FinishOverlappedSynchronously(handle, cancellationToken,
                overlapped, completionSource, result);

            if (finishedSynchronously)
            {
                overlapped.Dispose();
                finishedEvent.Dispose();
            }
            else
            {
                FinishOverlappedAsynchronously(handle, overlapped, finishedEvent, completionSource, cancellationToken);
            }

            return completionSource.Task;
        }

        private class OverlappedState
        {
            public bool IsCancellation { get; }
            public RegisteredWaitHandle OtherRegistration { get; set; }

            public OverlappedState(bool isCancellation)
            {
                IsCancellation = isCancellation;
            }

            public void Unregister()
            {
                OtherRegistration?.Unregister(null);
            }
        }

        static unsafe void FinishOverlappedAsynchronously(SafeObjectHandle handle, PinnedStruct<OVERLAPPED> overlapped,
            ManualResetEvent finishedEvent, TaskCompletionSource<int> completionSource, CancellationToken cancellationToken)
        {
            var alreadyFinished = false;
            var lockObject = new object();
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                var overlappedState = (OverlappedState) state;
                lock (lockObject)
                {
                    if (alreadyFinished)
                    {
                        return;
                    }

                    overlappedState.Unregister();

                    if (overlappedState.IsCancellation || cancellationToken.IsCancellationRequested)
                    {
                        CancelIoEx(handle, (OVERLAPPED*) overlapped.Pointer);
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        int bytesReturned;
                        var overlappedResult = GetOverlappedResult(handle, (OVERLAPPED*)overlapped.Pointer,
                            out bytesReturned, false);

                        overlapped.Dispose();
                        finishedEvent.Dispose();

                        if (overlappedResult)
                        {
                            completionSource.SetResult(bytesReturned);
                        }
                        else
                        {
                            SetFromLastWin32Exception(completionSource);
                        }
                    }

                    alreadyFinished = true;
                }
            };

            lock (lockObject)
            {
                var finishedState = new OverlappedState(false);
                var cancelledState = new OverlappedState(true);
                var finishedWait = ThreadPool.RegisterWaitForSingleObject(finishedEvent, callback, finishedState, -1, true);
                cancelledState.OtherRegistration = finishedWait;
                if (cancellationToken != CancellationToken.None)
                {
                    var cancelledWait = ThreadPool.RegisterWaitForSingleObject(cancellationToken.WaitHandle,
                        callback,
                        cancelledState, -1, true);
                    finishedState.OtherRegistration = cancelledWait;
                }
            }
        }

        static unsafe bool FinishOverlappedSynchronously(SafeObjectHandle handle, CancellationToken cancellationToken,
            PinnedStruct<OVERLAPPED> overlapped, TaskCompletionSource<int> completionSource, bool nativeMethodResult)
        {
            if (!nativeMethodResult)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorIoPending)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        SetFromLastWin32Exception(completionSource);
                    }

                    return true;
                }

                // Async IO in progress
                return false;
            }

            int pBytesReturned;
            GetOverlappedResult(handle, (OVERLAPPED*)overlapped.Pointer,
                out pBytesReturned, false);
            if (cancellationToken.IsCancellationRequested)
            {
                completionSource.SetCanceled();
            }
            else
            {
                completionSource.SetResult(pBytesReturned);
            }

            return true;
        }

        public static unsafe Task<int> DeviceIoControlAsync(
            SafeObjectHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return OverlappedAsync(hDevice, lpOverlapped =>
            {
                int pBytesReturned;
                return Kernell32DllNativeMethods.DeviceIoControl(hDevice, dwIoControlCode, inBuffer, nInBufferSize, outBuffer,
                    nOutBufferSize, out pBytesReturned, lpOverlapped);
            }, cancellationToken);
        }

        public static unsafe Task<int> WriteFileAsync(
            SafeObjectHandle handle,
            IntPtr buffer,
            int numberOfBytesToWrite,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                return WriteFile(handle, buffer.ToPointer(), numberOfBytesToWrite, null, lpOverlapped);
            }, cancellationToken);
        }

        public static async Task<int> WriteFileAsync<T>(SafeObjectHandle handle, ArraySegment<T> arraySegment,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : struct
        {
            using (var asFixed = new FixedArraySegment<T>(arraySegment))
            {
                return await WriteFileAsync(handle, asFixed.Pointer, asFixed.SizeInBytes, cancellationToken);
            }
        }

        public static unsafe Task<int> ReadFileAsync(SafeObjectHandle handle, IntPtr buffer, int numberOfBytesToRead,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                return ReadFile(handle, buffer.ToPointer(), numberOfBytesToRead, null, lpOverlapped);
            }, cancellationToken);
        }

        public static async Task<ArraySegment<T>> ReadFileAsync<T>(SafeObjectHandle handle, int numberOfElementsToRead,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : struct
        {
            var buffer = new T[numberOfElementsToRead];
            var segment = new ArraySegment<T>(buffer);
            using (var asFixed = new FixedArraySegment<T>(segment))
            {

                var bytesRead = await ReadFileAsync(handle, asFixed.Pointer, asFixed.SizeInBytes, cancellationToken);
                var elementsRead = bytesRead / asFixed.ElementSize;

                return new ArraySegment<T>(buffer, 0, elementsRead);
            }
        }
    }
}