using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class Pair<T, U> {
        public Pair(T first, U second) {
            this.First = first;
            this.Second = second;
        }

        public readonly T First;
        public readonly U Second;
    }

    public class LoosePair<T, U> : IEquatable<LoosePair<T, U>> {
        public LoosePair(T first, U second) {
            this.First = first;
            this.Second = second;
        }

        public readonly T First;
        public readonly U Second;

        public bool Equals(LoosePair<T, U> other) {
            return (First.Equals(other.First) || First.Equals(other.Second)) &&
                   (Second.Equals(other.First) || Second.Equals(other.Second));
        }

        public override int GetHashCode() {
            return First.GetHashCode() + Second.GetHashCode();
        }
    }

    public class Pair {
        public static Pair<T, U> Instance<T, U>(T first, U second) {
            return new Pair<T, U>(first, second);
        }
    }

    public class LoosePair {
        public static LoosePair<T, U> Instance<T, U>(T first, U second) {
            return new LoosePair<T, U>(first, second);
        }
    }
}
