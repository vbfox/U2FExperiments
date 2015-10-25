using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using static BlackFox.Win32.Kernel32.Kernell32DllNativeMethods;

namespace BlackFox.Win32.Kernel32
{
    public static class Kernel32Dll
    {
        public static IntPtr InvalidHandleValue = new IntPtr(-1);

        /// <summary>
        /// <para>Creates or opens a file or I/O device.</para>
        /// <para>The most commonly used I/O devices are as follows: file, file stream, directory, physical disk,
        /// volume, console buffer, tape drive, communications resource, mailslot, and pipe. The function returns a
        /// handle that can be used to access the file or device for various types of I/O depending on the file or
        /// device and the flags and attributes specified.</para>
        /// </summary>
        public static SafeFileHandle CreateFile(
            string lpFileName,
            Kernel32FileAccess dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes,
            FileCreationDisposition dwCreationDisposition,
            FileFlags dwFlagsAndAttributes,
            IntPtr hTemplateFile,
            bool throwOnInvalid = true)
        {
            var result = Kernell32DllNativeMethods.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes,
                dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            if (result.IsInvalid && throwOnInvalid)
            {
                throw new Win32Exception();
            }

            return result;
        }

        static unsafe bool DeviceIoControlCore(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned,
            NativeOverlapped? lpOverlapped)
        {
            using (var overlapped = lpOverlapped.Pin())
            {
                return Kernell32DllNativeMethods.DeviceIoControl(hDevice, dwIoControlCode, inBuffer, nInBufferSize,
                    outBuffer, nOutBufferSize, out pBytesReturned, (NativeOverlapped*)overlapped.Pointer);
            }
        }

        public static bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            NativeOverlapped lpOverlapped)
        {
            int pBytesReturned;
            return DeviceIoControlCore(hDevice, dwIoControlCode, inBuffer, nInBufferSize, outBuffer, nOutBufferSize,
                out pBytesReturned, lpOverlapped);
        }

        public static bool DeviceIoControl(
            SafeFileHandle hDevice,
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

        unsafe delegate bool OverlappedMethod(NativeOverlapped* overlapped);

        static unsafe Task<int> OverlappedAsync(SafeFileHandle handle, OverlappedMethod nativeMethod,
            CancellationToken cancellationToken)
        {
            var finishedEvent = new ManualResetEvent(false);

            var overlapped = new NativeOverlapped
            {
                EventHandle = finishedEvent.SafeWaitHandle.DangerousGetHandle()
            }.Pin();

            var result = nativeMethod((NativeOverlapped*)overlapped.Pointer);

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

        static unsafe void FinishOverlappedAsynchronously(SafeFileHandle handle, PinnedStruct<NativeOverlapped> overlapped,
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
                        CancelIoEx(handle, (NativeOverlapped*)overlapped.Pointer);
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        int bytesReturned;
                        var overlappedResult = GetOverlappedResult(handle, (NativeOverlapped*)overlapped.Pointer,
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

        static unsafe bool FinishOverlappedSynchronously(SafeFileHandle handle, CancellationToken cancellationToken,
            PinnedStruct<NativeOverlapped> overlapped, TaskCompletionSource<int> completionSource, bool nativeMethodResult)
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
            GetOverlappedResult(handle, (NativeOverlapped*)overlapped.Pointer,
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

        public unsafe static Task<int> DeviceIoControlAsync(
            SafeFileHandle hDevice,
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

        public unsafe static Task<int> WriteFileAsync(
            SafeFileHandle handle,
            IntPtr buffer,
            int numberOfBytesToWrite,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                int numberOfBytesWritten;
                return WriteFile(handle, buffer, numberOfBytesToWrite, out numberOfBytesWritten,
                    lpOverlapped);
            }, cancellationToken);
        }

        public static async Task<int> WriteFileAsync<T>(SafeFileHandle handle, ArraySegment<T> arraySegment,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : struct
        {
            using (var asFixed = new FixedArraySegment<T>(arraySegment))
            {
                return await WriteFileAsync(handle, asFixed.Pointer, asFixed.SizeInBytes, cancellationToken);
            }
        }

        public unsafe static Task<int> ReadFileAsync(SafeFileHandle handle, IntPtr buffer, int numberOfBytesToRead,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                int numberOfBytesRead;
                return ReadFile(handle, buffer, numberOfBytesToRead, out numberOfBytesRead,
                    lpOverlapped);
            }, cancellationToken);
        }

        public static async Task<ArraySegment<T>> ReadFileAsync<T>(SafeFileHandle handle, int numberOfElementsToRead,
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