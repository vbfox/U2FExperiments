// Copyright (c) to owners found in https://github.com/AArnott/pinvoke/blob/master/COPYRIGHT.md. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace PInvoke
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Extension methods for commonly defined types.
    /// </summary>
    public static class PInvokeExtensions
    {
        /// <summary>
        /// Converts an HRESULT to <see cref="NTSTATUS.Code"/>
        /// </summary>
        /// <param name="hresult">The HRESULT.</param>
        /// <returns>The <see cref="NTSTATUS.Code"/>.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static NTSTATUS.Code ToNTStatus(int hresult)
        {
            return (NTSTATUS.Code)((hresult & 0xC0007FFF) | ((uint)NTSTATUS.FacilityCode.FACILITY_FILTER_MANAGER << 16) | 0x40000000);
        }

        /// <summary>
        /// Throws an exception if a P/Invoke failed.
        /// </summary>
        /// <param name="status">The result of the P/Invoke call.</param>
        public static void ThrowOnError(this NTSTATUS.Code status)
        {
            switch (status)
            {
                case NTSTATUS.Code.STATUS_SUCCESS:
                    break;
                case NTSTATUS.Code.STATUS_INVALID_HANDLE:
                    throw new ArgumentException("Invalid handle");
                case NTSTATUS.Code.STATUS_INVALID_PARAMETER:
                    throw new ArgumentException();
                case NTSTATUS.Code.STATUS_NOT_FOUND:
                    throw new ArgumentException("Not found");
                case NTSTATUS.Code.STATUS_NO_MEMORY:
                    throw new OutOfMemoryException();
                case NTSTATUS.Code.STATUS_NOT_SUPPORTED:
                    throw new NotSupportedException();
                default:
                    if ((int)status < 0)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    break;
            }
        }
    }
}
