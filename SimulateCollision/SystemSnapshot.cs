namespace SimulateCollision
{
    public class SystemSnapshot
    {
        private List<(Float, SnapshotData)> _SnapshotData = [];

        public bool IsEmpty { get => !_SnapshotData.Any(); }

        public SystemSnapshot() { }

        public void Add(Float time, SnapshotData snapshotData)
        {
            _SnapshotData.Add((time, snapshotData));
        }

        public (Float, SnapshotData) this[int index] { get => _SnapshotData[index]; }
        public int Count => _SnapshotData.Count;

        public void Reset()
        {
            _SnapshotData.Clear();
        }
    }

    public record SnapshotData(int Index, Float PosX, Float PosY, Float VecX, Float VecY);

}
