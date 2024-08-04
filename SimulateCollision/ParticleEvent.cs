using System.Diagnostics;

namespace SimulateCollision
{
    internal class ParticleEvent
    {
        public Float Time { get; init; }
        public int IndexA { get; init; } = -1;
        public int IndexB { get; init; } = -1;
        public int VersionA { get; init; } = -1;
        public int VersionB { get; init; } = -1;
        public ParticleEvent() { Time = default; }

        public static ParticleEvent CreateEvent(Float t, int countA, int indexA, int verB, int indexB)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t且a和b相关的新事件
            return new ParticleEvent { Time = t, IndexA = indexA, VersionA = countA, IndexB = indexB, VersionB = verB };
        }

        public static ParticleEvent CreateEventHitVertical(Float t, int verA, int indexA)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new ParticleEvent { Time = t, IndexA = indexA, VersionA = verA };
        }

        public static ParticleEvent CreateEventHitHorizontal(Float t, int verB, int indexB)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new ParticleEvent { Time = t, IndexB = indexB, VersionB = verB };
        }

        public static ParticleEvent CreateEventSnapshotAll(Float t)
        {
            Debug.Assert(t > 0);

            //创造一个发生在时间t与a相关的新事件
            return new ParticleEvent { Time = t };
        }


        public bool IsValid(Particle[] arrParticle)
        {
            if (IndexA != -1 && arrParticle[IndexA].Version != VersionA)
            {
                return false;
            }
            if (IndexB != -1 && arrParticle[IndexB].Version != VersionB)
            {
                return false;
            }
            return true;
        }
    }
}
