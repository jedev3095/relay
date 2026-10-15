namespace relay;

internal sealed class MacroPlayer
{
	private Thread thread;

	private volatile bool stopRequested;

	public bool IsPlaying { get; private set; }

	public event Action<string> PlaybackStopped;

	public void Play(MacroDocument document, bool relativeMouse, bool restoreCursor, double speed, double mouseScale, bool continuous)
	{
		if (!IsPlaying)
		{
			stopRequested = false;
			IsPlaying = true;
			thread = new Thread((ThreadStart)delegate
			{
				Run(document, relativeMouse, restoreCursor, speed, mouseScale, continuous);
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	public void Stop()
	{
		stopRequested = true;
	}

	private void Run(MacroDocument document, bool relativeMouse, bool restoreCursor, double speed, double mouseScale, bool continuous)
	{
		string obj = "playback finished.";
		bool flag = false;
		try
		{
			List<MacroEvent> events = document.events;
			if (events == null || events.Count == 0)
			{
				return;
			}
			NativeMethods.BeginHighResolutionTiming();
			flag = true;
			do
			{
				if (MacroRecorder.HasMouseMovement(events) && document.startMouse != null && restoreCursor)
				{
					NativeMethods.SetCursorPos(document.startMouse.x, document.startMouse.y);
				}
				else if (restoreCursor)
				{
					if (document.startMouse != null)
					{
						NativeMethods.SetCursorPos(document.startMouse.x, document.startMouse.y);
					}
					else if (!relativeMouse)
					{
						MacroEvent macroEvent = events.Find((MacroEvent e) => e.type == "mouse_move" || e.type == "mouse_down" || e.type == "mouse_up");
						if (macroEvent != null)
						{
							NativeMethods.SetCursorPos(macroEvent.x, macroEvent.y);
						}
					}
				}
				Stopwatch clock = Stopwatch.StartNew();
				foreach (MacroEvent item in events)
				{
					if (stopRequested)
					{
						obj = "playback stopped.";
						break;
					}
					long targetMs = (long)Math.Max(0.0, (double)item.t / Math.Max(0.05, speed));
					WaitUntil(clock, targetMs);
					if (stopRequested)
					{
						obj = "playback stopped.";
						break;
					}
					PlayEvent(item, relativeMouse, mouseScale);
				}
				if (!stopRequested)
				{
					long durationMs = document.GetDurationMs();
					long targetMs2 = (long)Math.Max(0.0, (double)durationMs / Math.Max(0.05, speed));
					WaitUntil(clock, targetMs2);
				}
			}
			while (continuous && !stopRequested);
		}
		catch (Exception ex)
		{
			obj = "playback error: " + ex.Message;
		}
		finally
		{
			if (flag)
			{
				NativeMethods.EndHighResolutionTiming();
			}
			IsPlaying = false;
			if (this.PlaybackStopped != null)
			{
				this.PlaybackStopped(obj);
			}
		}
	}

	private void WaitUntil(Stopwatch clock, long targetMs)
	{
		while (!stopRequested)
		{
			long num = targetMs - clock.ElapsedMilliseconds;
			if (num > 0)
			{
				if (num > 3)
				{
					Thread.Sleep(1);
				}
				else
				{
					Thread.SpinWait(80);
				}
				continue;
			}
			break;
		}
	}

	private static void PlayEvent(MacroEvent item, bool relativeMouse, double mouseScale)
	{
		if (item.type == "mouse_move")
		{
			if (relativeMouse)
			{
				NativeMethods.SendRelativeMouseMove(ScaleDelta(item.dx, mouseScale), ScaleDelta(item.dy, mouseScale));
			}
			else
			{
				NativeMethods.SetCursorPos(item.x, item.y);
			}
		}
		else if (item.type == "mouse_down")
		{
			NativeMethods.SendMouseButton(item.button, down: true);
		}
		else if (item.type == "mouse_up")
		{
			NativeMethods.SendMouseButton(item.button, down: false);
		}
		else if (item.type == "mouse_wheel")
		{
			NativeMethods.SendMouseWheel(item.wheel);
		}
		else if (item.type == "key_down")
		{
			NativeMethods.SendKey((ushort)item.vk, down: true, (ushort)item.scanCode);
		}
		else if (item.type == "key_up")
		{
			NativeMethods.SendKey((ushort)item.vk, down: false, (ushort)item.scanCode);
		}
	}

	private static int ScaleDelta(int value, double scale)
	{
		return (int)Math.Round((double)value * Math.Max(0.01, scale));
	}
}
