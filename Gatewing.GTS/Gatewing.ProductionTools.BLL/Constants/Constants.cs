namespace Gatewing.ProductionTools.BLL
{
    public static class Constants
    {
        // severity constants
        public const int SEVERITY_NONE = 0;

        public const int SEVERITY_INFO = 1;
        public const int SEVERITY_WARN = 2;
        public const int SEVERITY_ERROR = 4;

        // state constants
        public const int STATE_NOK = 0;

        public const int STATE_BUSY = 1;
        public const int STATE_OK = 2;

        // device constants
        public const int DEVICE_EBOX = 0;

        public const int DEVICE_SERVO_LEFT = 1;
        public const int DEVICE_SERVO_RIGHT = 2;

        // action constants
        public const int ACTION_ADD = 1;

        public const int ACTION_REMOVE = 2;

        // gBox task constants

        public const int TSK_NONE = 0;
        public const int TSK_MODE = 1;
        public const int TSK_FW = 2;
        public const int TSK_OPTION = 4;
        public const int TSK_LOGGING = 8;
        public const int TSK_CLONE = 16;
        public const int TSK_TEST = 32;
        public const int TSK_START = 64;
        public const int TSK_END = 128;
        public const int TSK_PORTCHECK = 256;
        public const int TSK_RESET = 512;
        public const int TSK_FAIL = 1024;
    }
}