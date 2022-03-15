using System;

namespace GDrivePoC.Helpers
{
    public static class Extensions
    {
        public static int GetIndexOf<T>(this T[] array, T item)
        {
            return Array.IndexOf(array, item);
        }
    }
}
