using System;

namespace Penguin.Debugging
{
    [Flags]
    public enum LogOutput
    {
        Debug = 1,
        Console = 2,
        File = 4,
        All = Debug | Console | File
    }
}