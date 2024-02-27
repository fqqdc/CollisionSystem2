using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public record Event
    {
        public float Time { get; init; }
        public Entity EntityA { get; init; }
        public Entity EntityB { get; init; }
        public Version VersionA { get; init; }
        public Version VersionB { get; init; }
    }
}
