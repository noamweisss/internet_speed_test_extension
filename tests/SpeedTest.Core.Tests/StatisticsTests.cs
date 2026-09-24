using System;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class StatisticsTests
{
    [Fact]
    public void Median_OddCount_ReturnsMiddle() => Assert.Equal(3, Statistics.Median(new[] { 5.0, 1, 3 }));

    [Fact]
    public void Median_EvenCount_ReturnsMeanOfMiddleTwo() => Assert.Equal(2.5, Statistics.Median(new[] { 4.0, 1, 3, 2 }));

    [Fact]
    public void Median_Empty_Throws() => Assert.Throws<ArgumentException>(() => Statistics.Median(Array.Empty<double>()));

    [Fact]
    public void Jitter_SingleSample_IsZero() => Assert.Equal(0, Statistics.Jitter(new[] { 7.0 }));

    [Fact]
    public void Jitter_ConsecutiveDifferences_Averaged() => Assert.Equal(2, Statistics.Jitter(new[] { 10.0, 12, 10, 12 }));

    [Fact]
    public void Mbps_OneMegabyteInOneSecond_IsEightMbps() => Assert.Equal(8, Statistics.Mbps(1_000_000, TimeSpan.FromSeconds(1)));

    [Fact]
    public void Mbps_ZeroElapsed_IsZero() => Assert.Equal(0, Statistics.Mbps(1_000_000, TimeSpan.Zero));
}
