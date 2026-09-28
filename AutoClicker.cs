using System;
using System.Threading;

namespace relay;

internal sealed class AutoClicker : IDisposable
{
	private Thread thread;
	private readonly ManualResetEventSlim stopped = new(false);
	public bool IsRunning { get; private set; }

	public void Start(int intervalMs, int button)
	{
		if (IsRunning) return;
		stopped.Reset();
		IsRunning = true;
		thread = new Thread(() =>
		{
			while (!stopped.IsSet)
			{
				NativeMethods.SendMouseButton(button, true);
				NativeMethods.SendMouseButton(button, false);
				if (stopped.Wait(intervalMs)) break;
			}
		}) { IsBackground = true };
		thread.Start();
	}

	public void Stop()
	{
		if (!IsRunning) return;
		stopped.Set();
		thread.Join();
		IsRunning = false;
	}

	public void Dispose()
	{
		Stop();
		stopped.Dispose();
	}
}
