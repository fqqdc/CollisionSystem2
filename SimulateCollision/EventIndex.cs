using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class EventIndex
    {
        public float Time { get; private set; }
        public int IndexA { get; private set; } = -1;
        public int IndexB { get; private set; } = -1;
        public int CountA { get; private set; } = -1;
        public int CountB { get; private set; } = -1;

        public EventIndex(float t, Particle a, int indexA, Particle b, int indexB)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t且a和b相关的新事件
            this.Time = t;

            if (a != null)
            {
                IndexA = indexA;
                CountA = a.Count;
            }

            if (b != null)
            {
                IndexB = indexB;
                CountB = b.Count;
            }
        }

        public bool IsValid(Particle[] arrParticle)
        {
            if (IndexA != -1 && arrParticle[IndexA].Count != CountA)
            {
                return false;
            }
            if (IndexB != -1 && arrParticle[IndexB].Count != CountB)
            {
                return false;
            }
            return true;
        }
    }

    public class EventIndexComparer : IComparer<EventIndex>
    {
        public int Compare(EventIndex x, EventIndex y)
        {
            if (x.Time < y.Time)
            {
                return -1;
            }
            else if (x.Time > y.Time)
            {
                return 1;
            }
            else
            {
                return x.GetHashCode() - y.GetHashCode();
            }
        }
    }
}
