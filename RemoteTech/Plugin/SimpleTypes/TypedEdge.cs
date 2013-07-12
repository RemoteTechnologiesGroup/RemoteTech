using System;

namespace RemoteTech {
    public class TypedEdge<T> : IEquatable<TypedEdge<T>> {
        public bool Equals(TypedEdge<T> other) {
            return (A.Equals(other.A) || A.Equals(other.B)) &&
                   (B.Equals(other.A) || B.Equals(other.B));
        }

        public readonly T A;
        public readonly T B;
        public readonly EdgeType Type;

        public TypedEdge(T a, T b, EdgeType type) {
            A = a;
            B = b;
            Type = type;
        }

        public override int GetHashCode() {
            return A.GetHashCode() + B.GetHashCode();
        }

        public override string ToString() {
            return "TypedEdge(" + A + ", " + B + ", " + Type.ToString() + ")";
        }
    }
}
