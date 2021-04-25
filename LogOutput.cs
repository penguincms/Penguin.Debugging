using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Debugging
{
    [Flags]
    public enum LogOutput
    {
        Debug= 1,
        Console = 2,
        File = 4,
        All = Debug | Console | File
    }
}
