namespace SimulateCollision
{
    public interface ICollisionCoreSystem
    {
        int QueueLength { get; }
        SystemSnapshot SystemSnapshot { get; }
        Float SystemTime { get; }

        Float NextStep();
        void SnapshotAll();
    }
}