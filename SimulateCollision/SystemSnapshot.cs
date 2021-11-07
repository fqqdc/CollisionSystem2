using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class SystemSnapshot
    {
        public SystemSnapshot()
        {
            SnapshotTime = new();
            SnapshotData = new();
        }

        public List<double> SnapshotTime { get; init; }
        public List<SnapshotData[]> SnapshotData { get; init; }
    }

    public struct SnapshotData
    {
        public int Index;
        public double PosX;
        public double PosY;
        public double VecX;
        public double VecY;
    }

}
