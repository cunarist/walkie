using System;
using System.Runtime.InteropServices;

namespace RhinoWASD
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData; // be careful, this must be ints, not uints (was wrong before I changed it...). regards, cmew.
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }

        internal static ushort HIWORD(int dwValue)
        {
            return (ushort)((((long)dwValue) >> 0x10) & 0xffff);
        }

        internal static MSLLHOOKSTRUCT GetData(IntPtr lParam)
        {
            return (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
        }

        internal static int GetDelta(IntPtr lParam)
        {
            MSLLHOOKSTRUCT data = GetData(lParam);
            return (short)HIWORD(data.mouseData);
        }
    }
}
