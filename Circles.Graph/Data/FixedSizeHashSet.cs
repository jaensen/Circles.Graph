namespace Circles.Graph.Data
{
    public class FixedSizeHashSet<T>
    {
        private readonly LinkedList<T> _list = new();
        private readonly HashSet<T> _set = new();
        private readonly int _capacity;

        public FixedSizeHashSet(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public bool Add(T item)
        {
            if (_set.Contains(item))
            {
                return false; // Item already exists
            }

            if (_list.Count >= _capacity)
            {
                // Remove the oldest item
                var oldest = _list.First!;
                _list.RemoveFirst();
                _set.Remove(oldest.Value);
            }

            _list.AddLast(item);
            _set.Add(item);
            return true;
        }
    }
}