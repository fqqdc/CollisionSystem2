﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision
{
    public class EventIndex
    {
        public float Time { get; init; }
        public int IndexA { get; init; } = -1;
        public int IndexB { get; init; } = -1;
        public int CountA { get; init; } = -1;
        public int CountB { get; init; } = -1;
        public EventIndex() { Time = default; }

        public static EventIndex CreateEvent(float t, int countA, int indexA, int countB, int indexB)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t且a和b相关的新事件
            return new EventIndex { Time = t, IndexA = indexA, CountA = countA, IndexB = indexB, CountB = countB };
        }

        public static EventIndex CreateEventHitVertical(float t, int countA, int indexA)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new EventIndex { Time = t, IndexA = indexA, CountA = countA };
        }

        public static EventIndex CreateEventHitHorizontal(float t, int countB, int indexB)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new EventIndex { Time = t, IndexB = indexB, CountB = countB };
        }

        public static EventIndex CreateEventSnapshotAll(float t)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new EventIndex { Time = t };
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
}
