using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class TypedEdge<T> : IEquatable<TypedEdge<T>> {
        public readonly T A;
        public readonly T B;
        public readonly EdgeType Type;

        public TypedEdge(T a, T b, EdgeType type) {
            this.A = a;
            this.B = b;
            this.Type = type;
        }

        public bool Equals(TypedEdge<T> other) {
            return (A.Equals(other.A) || A.Equals(other.B)) &&
                   (B.Equals(other.A) || B.Equals(other.B));
        }

        public override int GetHashCode() {
            return A.GetHashCode() + B.GetHashCode();
        }
    }
}
