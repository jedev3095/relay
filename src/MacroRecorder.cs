namespace relay;

internal sealed class MacroRecorder : IDisposable
{
	private const long MouseThrottleMs = 10L;

	private nint mouseHook = IntPtr.Zero;

	private nint keyboardHook = IntPtr.Zero;

	private NativeMethods.HookProc mouseProc;

	private NativeMethods.HookProc keyboardProc;

	private readonly Stopwatch stopwatch = new Stopwatch();

	private Point lastMouse;

	private Point recordingStartMouse;

	private MacroDocument document;

	private long lastMouseEventTime;

	private int accumulatedDx;

	private int accumulatedDy;

	private readonly HashSet<int> ignoredHotkeyKeys = new HashSet<int>();

	public Func<int, bool> IsHotkeyKey { get; set; }

	public bool IsRecording { get; private set; }

	public List<MacroEvent> Events { get; private set; }

	public event Action RecordingStopped;

	public void Start(MacroDocument activeDocument)
	{
		document = activeDocument;
		Events = document.events;
		Events.Clear();
		recordingStartMouse = Cursor.Position;
		lastMouse = recordingStartMouse;
		document.startMouse = null;
		document.recordingMs = 0L;
		stopwatch.Restart();
		lastMouseEventTime = 0L;
		accumulatedDx = 0;
		accumulatedDy = 0;
		ignoredHotkeyKeys.Clear();
		mouseProc = MouseHook;
		keyboardProc = KeyboardHook;
		nint baseAddress = Process.GetCurrentProcess().MainModule.BaseAddress;
		mouseHook = NativeMethods.SetWindowsHookEx(14, mouseProc, baseAddress, 0u);
		keyboardHook = NativeMethods.SetWindowsHookEx(13, keyboardProc, baseAddress, 0u);
		IsRecording = true;
	}

	public void RecordRawMouse(int dx, int dy)
	{
		if (IsRecording && (dx != 0 || dy != 0))
		{
			Point point = (lastMouse = Cursor.Position);
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
			accumulatedDx += dx;
			accumulatedDy += dy;
			if (elapsedMilliseconds - lastMouseEventTime >= 10)
			{
				Events.Add(new MacroEvent
				{
					t = elapsedMilliseconds,
					type = "mouse_move",
					x = point.X,
					y = point.Y,
					dx = accumulatedDx,
					dy = accumulatedDy
				});
				lastMouseEventTime = elapsedMilliseconds;
				accumulatedDx = 0;
				accumulatedDy = 0;
			}
		}
	}

	public void Stop()
	{
		if (IsRecording)
		{
			IsRecording = false;
			stopwatch.Stop();
			if (mouseHook != IntPtr.Zero)
			{
				NativeMethods.UnhookWindowsHookEx(mouseHook);
			}
			if (keyboardHook != IntPtr.Zero)
			{
				NativeMethods.UnhookWindowsHookEx(keyboardHook);
			}
			mouseHook = IntPtr.Zero;
			keyboardHook = IntPtr.Zero;
			document.recordingMs = stopwatch.ElapsedMilliseconds;
			FinalizeStartMouse();
			if (this.RecordingStopped != null)
			{
				this.RecordingStopped();
			}
		}
	}

	private void FinalizeStartMouse()
	{
		if (document != null)
		{
			if (HasMouseMovement(Events))
			{
				document.startMouse = new MousePosition
				{
					x = recordingStartMouse.X,
					y = recordingStartMouse.Y
				};
			}
			else
			{
				document.startMouse = null;
			}
		}
	}

	public static bool HasMouseMovement(List<MacroEvent> events)
	{
		if (events == null)
		{
			return false;
		}
		foreach (MacroEvent @event in events)
		{
			if (@event.type == "mouse_move")
			{
				return true;
			}
		}
		return false;
	}

	public void Dispose()
	{
		Stop();
	}

	private nint MouseHook(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && IsRecording)
		{
			NativeMethods.MSLLHOOKSTRUCT mSLLHOOKSTRUCT = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));
			int num = ((IntPtr)wParam).ToInt32();
			Point point = new Point(mSLLHOOKSTRUCT.pt.x, mSLLHOOKSTRUCT.pt.y);
			int dx = point.X - lastMouse.X;
			int dy = point.Y - lastMouse.Y;
			switch (num)
			{
			case 512:
				lastMouse = point;
				break;
			case 513:
			case 514:
			case 516:
			case 517:
			case 519:
			case 520:
				Events.Add(new MacroEvent
				{
					t = stopwatch.ElapsedMilliseconds,
					type = (IsMouseDown(num) ? "mouse_down" : "mouse_up"),
					x = point.X,
					y = point.Y,
					dx = dx,
					dy = dy,
					button = MouseButtonNumber(num)
				});
				lastMouse = point;
				break;
			case 522:
			{
				short wheel = (short)((mSLLHOOKSTRUCT.mouseData >> 16) & 0xFFFF);
				Events.Add(new MacroEvent
				{
					t = stopwatch.ElapsedMilliseconds,
					type = "mouse_wheel",
					wheel = wheel,
					x = point.X,
					y = point.Y
				});
				break;
			}
			}
		}
		return NativeMethods.CallNextHookEx(mouseHook, nCode, wParam, lParam);
	}

	private nint KeyboardHook(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && IsRecording)
		{
			int num = ((IntPtr)wParam).ToInt32();
			NativeMethods.KBDLLHOOKSTRUCT obj = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
			int vkCode = (int)obj.vkCode;
			int scanCode = (int)obj.scanCode;
			bool down = num == 256 || num == 260;
			bool up = num == 257 || num == 261;
			if (down && IsHotkeyKey != null && IsHotkeyKey(vkCode)) ignoredHotkeyKeys.Add(vkCode);
			if ((down || up) && !ignoredHotkeyKeys.Contains(vkCode))
			{
				Events.Add(new MacroEvent
				{
					t = stopwatch.ElapsedMilliseconds,
					type = ((num == 257 || num == 261) ? "key_up" : "key_down"),
					vk = vkCode,
					scanCode = scanCode,
					key = ((Keys)vkCode).ToString()
				});
			}
			if (up) ignoredHotkeyKeys.Remove(vkCode);
		}
		return NativeMethods.CallNextHookEx(keyboardHook, nCode, wParam, lParam);
	}

	private static bool IsMouseDown(int msg)
	{
		if (msg != 513 && msg != 516)
		{
			return msg == 519;
		}
		return true;
	}

	private static int MouseButtonNumber(int msg)
	{
		switch (msg)
		{
		case 513:
		case 514:
			return 1;
		case 516:
		case 517:
			return 2;
		case 519:
		case 520:
			return 3;
		default:
			return 0;
		}
	}
}
