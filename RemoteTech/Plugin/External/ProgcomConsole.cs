using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class ProgcomConsole {
        private const int MAX_LINES = 20;

        private StringBuilder mBuffer = new StringBuilder();

        public void Log(String line) {
            mBuffer.AppendLine(line);
        }

        public void Log(String message, params Object[] param) {
            Log(String.Format(message, param));
        }

        public override string ToString() {
            return mBuffer.ToString();
        }
    }
}
