using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateCollision.ECS
{
    public record struct Position(float X, float Y);
    public record struct Velocity(float X, float Y);

    public record struct Radius(float Value);
    public record struct Mass(float Value);
    public record struct Version(int Value);
}
