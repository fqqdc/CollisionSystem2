namespace SimulateCollision
{
    public interface ICollisionCoreSystem
    {
        int QueueLength { get; }
        SystemSnapshot SystemSnapshot { get; }
        float SystemTime { get; }

        float NextStep();
        void SnapshotAll();
    }
}