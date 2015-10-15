// Copyright (c) Julien Roncaglia.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlackFox.Binary
{
    internal static class EmptyArray<T>
    {
        public static T[] Value { get; } = new T[0];
    }
}
