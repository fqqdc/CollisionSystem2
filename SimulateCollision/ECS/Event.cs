using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public record Event
    {
        public Float Time { get; init; }
        public Entity EntityA { get; init; }
        public Entity EntityB { get; init; }
        public int VersionA { get; init; }
        public int VersionB { get; init; }
    }
}
