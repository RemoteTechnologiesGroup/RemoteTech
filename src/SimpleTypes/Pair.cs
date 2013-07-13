using System;

namespace RemoteTech {
    public class Pair<T, U> {
        public readonly T First;
        public readonly U Second;

        public Pair(T first, U second) {
            First = first;
            Second = second;
        }
    }

    public class LoosePair<T, U> : IEquatable<LoosePair<T, U>> {
        public bool Equals(LoosePair<T, U> other) {
            return (First.Equals(other.First) || First.Equals(other.Second)) &&
                   (Second.Equals(other.First) || Second.Equals(other.Second));
        }

        public readonly T First;
        public readonly U Second;

        public LoosePair(T first, U second) {
            First = first;
            Second = second;
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
