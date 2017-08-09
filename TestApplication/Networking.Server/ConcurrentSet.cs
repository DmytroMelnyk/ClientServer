namespace Networking.Server
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    internal class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, object> _dictionary = new ConcurrentDictionary<T, object>();

        public IEnumerator<T> GetEnumerator() => _dictionary.Select(item => item.Key).GetEnumerator();

        public bool TryAdd(T item) => _dictionary.TryAdd(item, null);

        public bool TryRemove(T item) => _dictionary.TryRemove(item, out object temp);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
