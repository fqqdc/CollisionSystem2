using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class SystemSnapshot
    {
        public bool IsEmpty { get; init; } = true;

        public SystemSnapshot()
        {
            SnapshotTime = new();
            SnapshotData = new();
        }

        public List<float> SnapshotTime { get; init; }
        public List<SnapshotData[]> SnapshotData { get; init; }
    }

    public record struct SnapshotData(int Index, float PosX, float PosY, float VecX, float VecY);

}
