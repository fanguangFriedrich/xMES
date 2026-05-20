using OpenAuth.App.TriColorLamp.Response;

namespace OpenAuth.App.TriColorLamp
{
    public static class TriColorLampRealtimeCache
    {
        public static TriColorLampRealtimeSnapshotResponse Snapshot { get; private set; }

        public static void Set(TriColorLampRealtimeSnapshotResponse snapshot)
        {
            Snapshot = snapshot;
        }
    }
}
