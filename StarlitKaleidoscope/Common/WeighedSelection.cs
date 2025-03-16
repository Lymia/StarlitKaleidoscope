using System;
using System.Linq;
using XRL.Rules;

namespace StarlitKaleidoscope.Common {

    public interface IWeight {
        int Weight { get; }
    }

    public class WeighedSelection<T> where T : IWeight {
        readonly Tuple<T, int>[] values;
        readonly T[] originalValues;
        readonly int totalWeight;

        public WeighedSelection(T[] values) {
            this.values = values.Select(x => new Tuple<T, int>(x, x.Weight)).ToArray();
            originalValues = values;
            totalWeight = values.Select(x => x.Weight).Sum();
        }

        public T[] select(int count) {
            if (count > values.Length) return originalValues;

            var result = new T[count];
            bool[] selected = new bool[values.Length];
            int availableWeight = totalWeight;

            for (int i = 0; i < count; i++) {
                int weight = Stat.Rnd.Next(availableWeight);
                int j;
                int currentWeight = 0;
                for (j = 0; j < values.Length; j++) {
                    if (selected[j]) continue;
                    currentWeight += values[j].Item2;
                    if (weight < currentWeight) break;
                }
                if (j == values.Length) throw new Exception("inconsistent state!");

                selected[j] = true;
                availableWeight -= values[j].Item2;
                result[i] = values[j].Item1;
            }

            return result;
        }

        public T selectOne() {
            return select(1)[0];
        }
    }
}