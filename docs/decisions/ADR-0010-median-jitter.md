# ADR-0010: Jitter is the median of consecutive differences

Status: accepted · Date: 2026-09-28

## Context
On the owner's laptop the extension reports jitter above latency on every run (for example 53.6 ms jitter at
34.1 ms latency), while a VM on the same network showed 3.5 ms with the same code. Bisected on 2026-09-28:
`SpeedMeasurer` from `main`, run from a plain console process on the same laptop with the extension's `HttpClient`
settings, gave 6.1, 2.8 and 4.1 ms at 29 ms latency. Neither the network nor `SpeedTest.Core` adds the jitter; the
extension's process environment does. The packaged COM server has no foreground window, so Windows power
throttling (EcoQoS) on a laptop is the first suspect: it delays a few probes by scheduling alone. Opting the process
out would need `SetProcessInformation`, native interop, which AGENTS.md §2 and rule R5 prohibit.

Jitter was the mean absolute difference between consecutive samples (the Ookla definition). One isolated slow
sample moves two consecutive differences, and with ten samples those two dominate the mean.

## Decision
`Statistics.Jitter` is the median of the absolute differences between consecutive samples. `SpeedTestOptions.
LatencySamples` goes from 10 to 20, so the median has enough differences to settle. Nothing else changes: median
latency, the warm-up probe, and the Server-Timing subtraction stay as they were.

## Consequences
- Jitter no longer reports isolated spikes, whether from process scheduling or from the network. That is intended:
  a single slow packet is not what users mean by jitter.
- A link that is genuinely bursty for most of its samples still shows it: the median of the differences follows the
  bulk of the samples.
- The previous definition, the mean of consecutive differences, is the one Ookla publishes; the extension's figure
  is now lower than Ookla's on a link with occasional spikes and equal on a steady one.
- The latency phase sends ten more zero-byte probes: at most about one more second on a slow link.
