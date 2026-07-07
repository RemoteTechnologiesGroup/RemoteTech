using System;

namespace RemoteTech.SimpleTypes
{
    public enum LinkType : byte
    {
        None,
        Dish,
        Omni,
    }

    public readonly struct BidirectionalEdge<T> : IEquatable<BidirectionalEdge<T>>
    {

        public readonly T A;
        public readonly T B;
        public readonly LinkType Type;

        public BidirectionalEdge(T a, T b, LinkType type)
        {
            A = a;
            B = b;
            Type = type;
        }

        public bool Equals(BidirectionalEdge<T> other)
        {
            return (A.Equals(other.A) || A.Equals(other.B)) &&
                   (B.Equals(other.A) || B.Equals(other.B));
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() + B.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("BidirectionalEdge(A: {0}, B: {1}, Type {2})", A, B, Type.ToString());
        }
    }
}