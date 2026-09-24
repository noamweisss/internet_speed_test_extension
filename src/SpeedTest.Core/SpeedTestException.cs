using System;

namespace SpeedTest.Core;

/// <summary>A measurement failed for a reason the user can act on. Message is safe to display.</summary>
public sealed class SpeedTestException : Exception
{
    public SpeedTestException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
