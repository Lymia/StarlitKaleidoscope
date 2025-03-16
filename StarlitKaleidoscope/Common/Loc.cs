using System;
using System.Runtime.CompilerServices;
using XRL.World;

namespace StarlitKaleidoscope.Common {
    [Serializable]
    public struct Loc : IEquatable<Loc>, IComparable<Loc>, IComposite {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loc(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public int x { get; private set; }
        public int y { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Loc other) => x == other.x && y == other.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is Loc other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Loc left, Loc right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Loc left, Loc right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Loc other) {
            var xComparison = x.CompareTo(other.x);
            if (xComparison != 0) return xComparison;
            return y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loc Add(int x, int y) => new(this.x + x, this.y + y);

        public bool WantFieldReflection => false;

        public void Write(SerializationWriter Writer) {
            Writer.WriteOptimized(x);
            Writer.WriteOptimized(y);
        }

        public void Read(SerializationReader Reader) {
            x = Reader.ReadOptimizedInt32();
            y = Reader.ReadOptimizedInt32();
        }
    }
}