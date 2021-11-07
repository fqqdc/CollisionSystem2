using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class Event
    {
        public double Time { get; private set; }
        public Particle A { get; private set; }
        public Particle B { get; private set; }
        public int CountA { get; private set; }
        public int CountB { get; private set; }

        public Event(double t, Particle a, Particle b)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t且a和b相关的新事件
            this.Time = t;
            this.A = a;
            this.B = b;
            if (a != null)
            {
                CountA = a.Count;
            }
            else
            {
                CountA = -1;
            }
            if (b != null)
            {
                CountB = b.Count;
            }
            else
            {
                CountB = -1;
            }
        }

        public bool IsValid()
        {
            if (A != null && A.Count != CountA)
            {
                return false;
            }
            if (B != null && B.Count != CountB)
            {
                return false;
            }
            return true;
        }
    }

    public class EventComparer : IComparer<Event>
    {
        public int Compare(Event x, Event y)
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
