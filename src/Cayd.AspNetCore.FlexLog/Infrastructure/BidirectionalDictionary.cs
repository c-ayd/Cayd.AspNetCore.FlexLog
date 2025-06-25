using System.Collections.Generic;
using System.Linq;

namespace Cayd.AspNetCore.FlexLog.Infrastructure
{
    internal class BidirectionalDictionary<T>
        where T : notnull
    {
        private Dictionary<T, T> _forward;
        private Dictionary<T, T> _backward;

        internal BidirectionalDictionary(Dictionary<T, T> dictionary, IEqualityComparer<T> comparer)
        {
            _forward = new Dictionary<T, T>(dictionary, comparer);
            _backward = dictionary.ToDictionary(kv => kv.Value, kv => kv.Key, comparer);
        }

        internal bool TryGetValue(T? lookUpValue, out T? value)
        {
            if (lookUpValue == null)
            {
                value = default;
                return false;
            }

            if (_forward.TryGetValue(lookUpValue, out value))
                return true;

            return _backward.TryGetValue(lookUpValue, out value);
        }
    }
}
