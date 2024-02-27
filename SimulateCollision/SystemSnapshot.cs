using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class SystemSnapshot
    {
        private List<(float, SnapshotData)> _SnapshotData = [];

        public bool IsEmpty { get => !_SnapshotData.Any(); }

        public SystemSnapshot() { }

        public void Add(float time, SnapshotData snapshotData)
        {
            _SnapshotData.Add((time, snapshotData));
        }

        public (float, SnapshotData) this[int index] { get => _SnapshotData[index]; }
        public int Count => _SnapshotData.Count;

        public void Reset()
        {
            _SnapshotData.Clear();
        }
    }

    public record SnapshotData(int Index, float PosX, float PosY, float VecX, float VecY);

}
