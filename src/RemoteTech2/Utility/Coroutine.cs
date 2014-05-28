using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class Coroutine : IEnumerable
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(Coroutine));
        public bool Abort { get; set; }

        private IEnumerator routine;
        public Coroutine(IEnumerator routine)
        {
            this.routine = routine;
        }

        public IEnumerator GetEnumerator()
        {
            if (!Abort && routine.MoveNext())
            {
                yield return routine.Current;
            }
            if (Abort) Logger.Debug("Aborted.");
        }
    }
}
