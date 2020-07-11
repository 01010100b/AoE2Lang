using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler
{
    static class Log
    {
        public static void Debug(string message)
        {
            System.Diagnostics.Trace.WriteLine(message);
        }
    }
}
