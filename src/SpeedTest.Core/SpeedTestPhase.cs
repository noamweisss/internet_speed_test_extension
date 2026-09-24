namespace SpeedTest.Core;

/// <summary>Phases of one measurement, in the order they run.</summary>
public enum SpeedTestPhase
{
    Idle,
    Connecting,
    Latency,
    Download,
    Upload,
    Complete,
    Failed,
}
