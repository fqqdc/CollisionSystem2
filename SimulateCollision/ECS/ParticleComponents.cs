namespace SimulateCollision.ECS
{
    //public record struct Position(Float X, Float Y);
    //public record struct Velocity(Float X, Float Y);

    //public record struct Radius(Float Value);
    //public record struct Mass(Float Value);
    //public record struct Version(int Value);

    public record struct Particle(Float PositionX, Float PositionY, Float VelocityX, Float VelocityY, Float Radius, Float Mass, int Version);
}
