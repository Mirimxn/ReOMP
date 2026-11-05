namespace OMP.LSWTSS;

partial class Overlay1
{
    static class WinMacros
    {
        public static ushort LOWORD(uint value)
        {
            return unchecked((ushort)value);
        }

        public static ushort HIWORD(uint value)
        {
            return unchecked((ushort)(value >> 16));
        }

        public static ushort GET_KEYSTATE_PARAM(uint value)
        {
            return LOWORD(value);
        }

        public static ushort GET_WHEEL_DELTA_WPARAM(uint value)
        {
            return HIWORD(value);
        }
    }
}