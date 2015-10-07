using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32.Kernel32
{
    static partial class Kernel32Dll
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
            FileAccess dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes,
            FileCreationDisposition dwCreationDisposition,
            FileFlags dwFlagsAndAttributes,
            IntPtr hTemplateFile,
            bool throwOnInvalid = true)
        {
            var result = NativeMethods.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes,
                dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            if (result.IsInvalid && throwOnInvalid)
            {
                throw new Win32Exception();
            }

            return result;
        }

        static bool DeviceIoControlCore(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned,
            NativeOverlapped? lpOverlapped)
        {
            using (var overlapped = new NullableStructPtr<NativeOverlapped>(lpOverlapped))
            {
                return NativeMethods.DeviceIoControl(hDevice, dwIoControlCode, inBuffer, nInBufferSize,
                    outBuffer, nOutBufferSize, out pBytesReturned, overlapped.Pointer);
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

        const int ERROR_IO_PENDING = 997;

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

        static Task<int> OverlappedAsync(SafeFileHandle handle, Func< IntPtr, bool> nativeMethod)
        {
            var evt = new ManualResetEvent(false);
            var overlapped = new NativeOverlapped
            {
                EventHandle = evt.SafeWaitHandle.DangerousGetHandle()
            }.ToPtr();

            
            var result = nativeMethod(overlapped.Pointer);

            var completionSource = new TaskCompletionSource<int>();

            if (result)
            {
                int pBytesReturned;
                NativeMethods.GetOverlappedResult(handle, overlapped.Pointer, out pBytesReturned, false);
                completionSource.SetResult(pBytesReturned);
                overlapped.Dispose();
                evt.Dispose();
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_IO_PENDING)
                {
                    SetFromLastWin32Exception(completionSource);
                    overlapped.Dispose();
                    evt.Dispose();
                }
                else
                {
                    WaitOrTimerCallback callback = (state, timedOut) =>
                    {
                        int pBytesReturned;
                        var overlappedResult = NativeMethods.GetOverlappedResult(handle, overlapped.Pointer,
                            out pBytesReturned, false);

                        overlapped.Dispose();
                        evt.Dispose();

                        if (overlappedResult)
                        {
                            completionSource.SetResult(pBytesReturned);
                            
                        }
                        else
                        {
                            SetFromLastWin32Exception(completionSource);
                        }
                    };

                    ThreadPool.RegisterWaitForSingleObject(evt, callback, null, -1, true);
                }
            }

            return completionSource.Task;
        }

        public static Task<int> DeviceIoControlAsync(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize)
        {
            return OverlappedAsync(hDevice, lpOverlapped =>
            {
                int pBytesReturned;
                return NativeMethods.DeviceIoControl(hDevice, dwIoControlCode, inBuffer, nInBufferSize, outBuffer,
                    nOutBufferSize, out pBytesReturned, lpOverlapped);
            });
        }

        public static Task<int> WriteFileAsync(
            SafeFileHandle handle,
            IntPtr buffer,
            int numberOfBytesToWrite)
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                int numberOfBytesWritten;
                return NativeMethods.WriteFile(handle, buffer, numberOfBytesToWrite, out numberOfBytesWritten,
                    lpOverlapped);
            });
        }

        public static Task<int> WriteFileAsync<T>(SafeFileHandle handle, ArraySegment<T> arraySegment)
            where T : struct
        {
            var asFixed = new FixedArraySegment<T>(arraySegment);

            var write = WriteFileAsync(handle, asFixed.Pointer, asFixed.SizeInBytes);
            write.ContinueWith(task => asFixed.Dispose());
            return write;
        }

        public static Task<int> ReadFileAsync(SafeFileHandle handle, IntPtr buffer, int numberOfBytesToRead)
        {
            return OverlappedAsync(handle, lpOverlapped =>
            {
                int numberOfBytesRead;
                return NativeMethods.ReadFile(handle, buffer, numberOfBytesToRead, out numberOfBytesRead,
                    lpOverlapped);
            });
        }

        public static Task<ArraySegment<T>> ReadFileAsync<T>(SafeFileHandle handle, int numberOfElementsToRead)
            where T : struct
        {
            var buffer = new T[numberOfElementsToRead];
            var segment = new ArraySegment<T>(buffer);
            var asFixed = new FixedArraySegment<T>(segment);

            var read = ReadFileAsync(handle, asFixed.Pointer, asFixed.SizeInBytes);
            read.ContinueWith(task => asFixed.Dispose());

            return read.ContinueWith(readTask =>
            {
                var elementsRead = readTask.Result / asFixed.ElementSize;

                return new ArraySegment<T>(buffer, 0, elementsRead);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}