namespace Networking.Server
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal class ConcurrentSet<T> : IEnumerable<T>
    {
        private ConcurrentDictionary<T, object> dictionary = new ConcurrentDictionary<T, object>();

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in dictionary)
            {
                yield return item.Key;
            }
        }

        public bool TryAdd(T item)
        {
            return dictionary.TryAdd(item, null);
        }

        public bool TryRemove(T item)
        {
            object temp;
            return dictionary.TryRemove(item, out temp);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
