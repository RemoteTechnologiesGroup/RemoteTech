using System;

namespace RemoteTech
{
    public class UnorderedPair<T> : IEquatable<UnorderedPair<T>>
    {
        public readonly T A;
        public readonly T B;
        public UnorderedPair(T a, T b)
        {
            A = a;
            B = b;
        }
        public bool Equals(UnorderedPair<T> other)
        {
            return (A.Equals(other.A) || A.Equals(other.B)) &&
                   (B.Equals(other.A) || B.Equals(other.B));
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var pair = obj as UnorderedPair<T>;
            if (pair == null)
                return false;
            else
                return Equals(pair);
        }
        public override int GetHashCode()
        {
            return A.GetHashCode() + B.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("UnorderedPair(A: {0}, B: {1})", A, B);
        }
    }
}