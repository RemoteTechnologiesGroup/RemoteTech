using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface ILogger
    {
        void Fatal(String message, params object[] objects);
        void Error(String message, params object[] objects);
        void Warning(String message, params object[] objects);
        void Info(String message, params object[] objects);
        void Debug(String message, params object[] objects);
    }
}
