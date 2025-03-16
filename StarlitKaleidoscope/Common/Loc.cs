using System;
using System.Runtime.CompilerServices;

namespace StarlitKaleidoscope.Common {
    [Serializable]
    public readonly struct Loc : IEquatable<Loc>, IComparable<Loc> {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loc(int x, int y) {
            this.x = x;
            this.y = y;
        }
            
        public readonly int x, y;

        public bool Equals(Loc other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Loc other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);

        public static bool operator ==(Loc left, Loc right) => left.Equals(right);
        public static bool operator !=(Loc left, Loc right) => !left.Equals(right);
        
        public int CompareTo(Loc other) {
            var xComparison = x.CompareTo(other.x);
            if (xComparison != 0) return xComparison;
            return y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loc Add(int x, int y) => new Loc(this.x + x, this.y + y);
    }
}