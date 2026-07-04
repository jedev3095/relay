// Combined relay source file

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System;

namespace relay;

// ==========================================
// File: MousePosition.cs
// ==========================================
public sealed class MousePosition
{
	public int x { get; set; }

	public int y { get; set; }
}

// ==========================================
// File: ScreenInfo.cs
// ==========================================
public sealed class ScreenInfo
{
	public int width { get; set; }

	public int height { get; set; }

	public int left { get; set; }

	public int top { get; set; }
}

// ==========================================
// File: MacroEvent.cs
// ==========================================
public sealed class MacroEvent
{
	public long t { get; set; }

	public string type { get; set; }

	public int x { get; set; }

	public int y { get; set; }

	public int dx { get; set; }

	public int dy { get; set; }

	public int button { get; set; }

	public int wheel { get; set; }

	public int vk { get; set; }

	public int scanCode { get; set; }

	public string key { get; set; }
}

// ==========================================
// File: MacroDocument.cs
// ==========================================
public sealed class MacroDocument
{
	public string format { get; set; }

	public string app { get; set; }

	public string createdUtc { get; set; }

	public ScreenInfo recordingScreen { get; set; }

	public MousePosition startMouse { get; set; }

	public long recordingMs { get; set; }

	public List<MacroEvent> events { get; set; }

	public long GetDurationMs()
	{
		if (recordingMs > 0)
		{
			return recordingMs;
		}
		if (events == null || events.Count == 0)
		{
			return 0L;
		}
		return events[events.Count - 1].t;
	}
}

// ==========================================
// File: MacroJson.cs
// ==========================================
internal static class MacroJson
{
	private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		WriteIndented = true
	};

	public static string Serialize(MacroDocument document)
	{
		return JsonSerializer.Serialize(document, jsonOptions);
	}

	public static MacroDocument Deserialize(string json)
	{
		return JsonSerializer.Deserialize<MacroDocument>(json, jsonOptions);
	}
}

// ==========================================
// File: NativeMethods.cs
// ==========================================
internal static class NativeMethods
{
	public delegate nint HookProc(int nCode, nint wParam, nint lParam);

	public struct POINT
	{
		public int x;

		public int y;
	}

	public struct MSLLHOOKSTRUCT
	{
		public POINT pt;

		public uint mouseData;

		public uint flags;

		public uint time;

		public nint dwExtraInfo;
	}

	public struct KBDLLHOOKSTRUCT
	{
		public uint vkCode;

		public uint scanCode;

		public uint flags;

		public uint time;

		public nint dwExtraInfo;
	}

	private struct RAWINPUTDEVICE
	{
		public ushort usUsagePage;

		public ushort usUsage;

		public uint dwFlags;

		public nint hwndTarget;
	}

	private struct RAWINPUTHEADER
	{
		public uint dwType;

		public uint dwSize;

		public nint hDevice;

		public nint wParam;
	}

	private struct RAWMOUSE
	{
		public ushort usFlags;

		public uint ulButtons;

		public uint ulRawButtons;

		public int lLastX;

		public int lLastY;

		public uint ulExtraInformation;
	}

	private struct RAWINPUT
	{
		public RAWINPUTHEADER header;

		public RAWMOUSE mouse;
	}

	private struct INPUT
	{
		public int type;

		public InputUnion u;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct InputUnion
	{
		[FieldOffset(0)]
		public MOUSEINPUT mi;

		[FieldOffset(0)]
		public KEYBDINPUT ki;
	}

	private struct MOUSEINPUT
	{
		public int dx;

		public int dy;

		public uint mouseData;

		public uint dwFlags;

		public uint time;

		public nint dwExtraInfo;
	}

	private struct KEYBDINPUT
	{
		public ushort wVk;

		public ushort wScan;

		public uint dwFlags;

		public uint time;

		public nint dwExtraInfo;
	}

	private delegate bool EnumChildWindowsCallback(nint hWnd, nint lParam);

	public const int WH_MOUSE_LL = 14;

	public const int WH_KEYBOARD_LL = 13;

	public const int WM_MOUSEMOVE = 512;

	public const int WM_LBUTTONDOWN = 513;

	public const int WM_LBUTTONUP = 514;

	public const int WM_RBUTTONDOWN = 516;

	public const int WM_RBUTTONUP = 517;

	public const int WM_MBUTTONDOWN = 519;

	public const int WM_MBUTTONUP = 520;

	public const int WM_MOUSEWHEEL = 522;

	public const int WM_KEYDOWN = 256;

	public const int WM_KEYUP = 257;

	public const int WM_SYSKEYDOWN = 260;

	public const int WM_SYSKEYUP = 261;

	public const int WM_INPUT = 255;

	public const int SB_BOTH = 3;

	private const int RID_INPUT = 268435459;

	private const int RIM_TYPEMOUSE = 0;

	private const int RIDEV_INPUTSINK = 256;

	private const int INPUT_MOUSE = 0;

	private const int INPUT_KEYBOARD = 1;

	private const uint MOUSEEVENTF_MOVE = 1u;

	private const uint MOUSEEVENTF_LEFTDOWN = 2u;

	private const uint MOUSEEVENTF_LEFTUP = 4u;

	private const uint MOUSEEVENTF_RIGHTDOWN = 8u;

	private const uint MOUSEEVENTF_RIGHTUP = 16u;

	private const uint MOUSEEVENTF_MIDDLEDOWN = 32u;

	private const uint MOUSEEVENTF_MIDDLEUP = 64u;

	private const uint MOUSEEVENTF_WHEEL = 2048u;

	private const uint KEYEVENTF_KEYUP = 2u;

	private const uint KEYEVENTF_SCANCODE = 8u;

	[DllImport("user32.dll")]
	public static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll")]
	public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

	[DllImport("user32.dll")]
	public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, int vk);

	[DllImport("user32.dll")]
	public static extern bool UnregisterHotKey(nint hWnd, int id);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ShowScrollBar(nint hWnd, int wBar, bool bShow);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumChildWindows(nint hwndParent, EnumChildWindowsCallback lpEnumFunc, nint lParam);

	public static void HideAllScrollBars(nint hwnd)
	{
		ShowScrollBar(hwnd, 3, bShow: false);
		EnumChildWindows(hwnd, delegate(nint child, nint lParam)
		{
			ShowScrollBar(child, 3, bShow: false);
			return true;
		}, IntPtr.Zero);
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetRawInputData(nint hRawInput, uint uiCommand, nint pData, ref uint pcbSize, uint cbSizeHeader);

	[DllImport("winmm.dll")]
	private static extern uint timeBeginPeriod(uint uPeriod);

	[DllImport("winmm.dll")]
	private static extern uint timeEndPeriod(uint uPeriod);

	public static void RegisterRawMouseInput(nint hwnd)
	{
		RAWINPUTDEVICE[] array = new RAWINPUTDEVICE[1];
		array[0].usUsagePage = 1;
		array[0].usUsage = 2;
		array[0].dwFlags = 256u;
		array[0].hwndTarget = hwnd;
		RegisterRawInputDevices(array, 1u, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
	}

	public static bool TryGetRawMouseDelta(nint rawInputHandle, out int dx, out int dy)
	{
		dx = 0;
		dy = 0;
		uint pcbSize = 0u;
		uint cbSizeHeader = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
		GetRawInputData(rawInputHandle, 268435459u, IntPtr.Zero, ref pcbSize, cbSizeHeader);
		if (pcbSize == 0)
		{
			return false;
		}
		nint num = Marshal.AllocHGlobal((int)pcbSize);
		try
		{
			uint rawInputData = GetRawInputData(rawInputHandle, 268435459u, num, ref pcbSize, cbSizeHeader);
			if (rawInputData == 0 || rawInputData == uint.MaxValue)
			{
				return false;
			}
			RAWINPUT rAWINPUT = (RAWINPUT)Marshal.PtrToStructure(num, typeof(RAWINPUT));
			if (rAWINPUT.header.dwType != 0)
			{
				return false;
			}
			dx = rAWINPUT.mouse.lLastX;
			dy = rAWINPUT.mouse.lLastY;
			return dx != 0 || dy != 0;
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	public static void BeginHighResolutionTiming()
	{
		timeBeginPeriod(1u);
	}

	public static void EndHighResolutionTiming()
	{
		timeEndPeriod(1u);
	}

	public static void SendRelativeMouseMove(int dx, int dy)
	{
		SendMouse(dx, dy, 0u, 1u);
	}

	public static void SendMouseWheel(int delta)
	{
		SendMouse(0, 0, (uint)delta, 2048u);
	}

	public static void SendMouseButton(int button, bool down)
	{
		uint num = 0u;
		if (button == 1)
		{
			num = (down ? 2u : 4u);
		}
		if (button == 2)
		{
			num = (down ? 8u : 16u);
		}
		if (button == 3)
		{
			num = (down ? 32u : 64u);
		}
		if (num != 0)
		{
			SendMouse(0, 0, 0u, num);
		}
	}

	public static void SendKey(ushort vk, bool down, ushort scanCode = 0)
	{
		INPUT[] array = new INPUT[1];
		array[0].type = 1;
		array[0].u.ki.wVk = vk;
		array[0].u.ki.wScan = scanCode;
		array[0].u.ki.dwFlags = ((scanCode != 0) ? 8u : 0u) | ((!down) ? 2u : 0u);
		SendInput(1u, array, Marshal.SizeOf(typeof(INPUT)));
	}

	private static void SendMouse(int dx, int dy, uint data, uint flags)
	{
		INPUT[] array = new INPUT[1];
		array[0].type = 0;
		array[0].u.mi.dx = dx;
		array[0].u.mi.dy = dy;
		array[0].u.mi.mouseData = data;
		array[0].u.mi.dwFlags = flags;
		SendInput(1u, array, Marshal.SizeOf(typeof(INPUT)));
	}
}

// ==========================================
// File: MacroPlayer.cs
// ==========================================
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

// ==========================================
// File: MacroRecorder.cs
// ==========================================
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
			if (vkCode != 119 && vkCode != 120 && vkCode != 121 && (num == 256 || num == 260 || num == 257 || num == 261))
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

// ==========================================
// File: StyledScrollPanel.cs
// ==========================================
internal sealed class StyledScrollPanel : Panel
{
	private const int BarWidth = 7;

	private const int BarMargin = 3;

	private bool thumbDragging;

	private int thumbDragOffset;

	private int ContentHeight
	{
		get
		{
			int num = 0;
			foreach (Control control in base.Controls)
			{
				num = Math.Max(num, control.Bottom);
			}
			return num + base.Padding.Bottom;
		}
	}

	private bool NeedsVerticalScroll => ContentHeight > base.ClientSize.Height;

	public StyledScrollPanel()
	{
		AutoScroll = true;
		DoubleBuffered = true;
		base.Padding = new Padding(0, 0, 13, 0);
	}

	private Rectangle GetThumbRect()
	{
		int height = base.ClientSize.Height;
		int num = Math.Max(ContentHeight, DisplayRectangle.Height);
		if (num <= height)
		{
			return Rectangle.Empty;
		}
		int num2 = height - 6;
		int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
		int num4 = num - height;
		int num5 = Math.Max(0, -base.AutoScrollPosition.Y);
		int y = ((num4 == 0) ? 3 : (3 + (int)((double)num5 / (double)num4 * (double)(num2 - num3))));
		return new Rectangle(base.ClientSize.Width - 7 - 3, y, 7, num3);
	}

	private void SyncChildWidths()
	{
		int width = base.ClientSize.Width;
		int width2 = Math.Max(0, width - base.Padding.Horizontal);
		foreach (Control control in base.Controls)
		{
			control.Width = width2;
		}
	}

	private void HideSystemScrollBars()
	{
		if (base.IsHandleCreated)
		{
			NativeMethods.ShowScrollBar(base.Handle, 3, bShow: false);
		}
	}

	protected override void OnControlAdded(ControlEventArgs e)
	{
		base.OnControlAdded(e);
		if (e.Control.AutoSize)
		{
			e.Control.Dock = DockStyle.Top;
		}
		SyncChildWidths();
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		SyncChildWidths();
		HideSystemScrollBars();
		Invalidate();
	}

	protected override void OnScroll(ScrollEventArgs se)
	{
		base.OnScroll(se);
		if (base.AutoScrollPosition.X != 0)
		{
			base.AutoScrollPosition = new Point(0, base.AutoScrollPosition.Y);
		}
		HideSystemScrollBars();
		Invalidate();
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		Invalidate();
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		if (m.Msg == 277 || m.Msg == 276 || m.Msg == 522 || m.Msg == 5)
		{
			HideSystemScrollBars();
			Invalidate();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (!NeedsVerticalScroll)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int x = base.ClientSize.Width - 7 - 3;
		Rectangle rect = new Rectangle(x, 3, 7, base.ClientSize.Height - 6);
		using (SolidBrush solidBrush = new SolidBrush(Color.FromArgb(28, 33, 44)))
		{
			graphics.FillRectangle(solidBrush, rect);
		}
		Rectangle thumbRect = GetThumbRect();
		if (thumbRect.IsEmpty)
		{
			return;
		}
		using SolidBrush solidBrush2 = new SolidBrush(Color.FromArgb(88, 98, 118));
		graphics.FillRectangle(solidBrush2, thumbRect);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (!NeedsVerticalScroll)
		{
			return;
		}
		Rectangle thumbRect = GetThumbRect();
		if (thumbRect.Contains(e.Location))
		{
			thumbDragging = true;
			thumbDragOffset = e.Y - thumbRect.Y;
			base.Capture = true;
			return;
		}
		int x = base.ClientSize.Width - 7 - 3;
		Rectangle rectangle = new Rectangle(x, 3, 7, base.ClientSize.Height - 6);
		if (rectangle.Contains(e.Location))
		{
			int num = Math.Max(ContentHeight, DisplayRectangle.Height);
			int height = base.ClientSize.Height;
			int num2 = height - 6;
			int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
			int num4 = e.Y - num3 / 2;
			int num5 = num2 - num3;
			int num6 = Math.Max(0, Math.Min(num5, num4 - 3));
			int num7 = num - height;
			int y = ((num5 > 0) ? ((int)((double)num6 / (double)num5 * (double)num7)) : 0);
			base.AutoScrollPosition = new Point(0, y);
			Invalidate();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (thumbDragging)
		{
			int num = Math.Max(ContentHeight, DisplayRectangle.Height);
			int height = base.ClientSize.Height;
			int num2 = height - 6;
			int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
			int num4 = num2 - num3;
			int val = e.Y - thumbDragOffset - 3;
			val = Math.Max(0, Math.Min(num4, val));
			int num5 = num - height;
			int y = ((num4 > 0) ? ((int)((double)val / (double)num4 * (double)num5)) : 0);
			base.AutoScrollPosition = new Point(0, y);
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		thumbDragging = false;
		base.Capture = false;
	}
}

// ==========================================
// File: StyledEventGridHost.cs
// ==========================================
internal sealed class StyledEventGridHost : Panel
{
	private const int BarWidth = 7;

	private const int BarMargin = 3;

	private static readonly Color TrackColor = Color.FromArgb(28, 33, 44);

	private static readonly Color ThumbColor = Color.FromArgb(88, 98, 118);

	private bool thumbDraggingV;

	private bool thumbDraggingH;

	private int thumbDragOffsetV;

	private int thumbDragOffsetH;

	public DataGridView Grid { get; private set; }

	private int Gutter => 13;

	private DataGridView GridView => Grid;

	private int TotalContentHeight => GridView.ColumnHeadersHeight + GridView.Rows.Count * GridView.RowTemplate.Height;

	private int TotalContentWidth
	{
		get
		{
			int num = 0;
			foreach (DataGridViewColumn column in GridView.Columns)
			{
				if (column.Visible)
				{
					num += column.Width;
				}
			}
			return num;
		}
	}

	private bool NeedsVerticalScroll => TotalContentHeight > GridView.ClientSize.Height;

	private bool NeedsHorizontalScroll => TotalContentWidth > GridView.ClientSize.Width;

	private int MaxFirstDisplayedRow
	{
		get
		{
			if (GridView.Rows.Count == 0)
			{
				return 0;
			}
			int num = GridView.DisplayedRowCount(includePartialRow: false);
			if (num <= 0)
			{
				num = Math.Max(1, (GridView.ClientSize.Height - GridView.ColumnHeadersHeight) / Math.Max(1, GridView.RowTemplate.Height));
			}
			return Math.Max(0, GridView.Rows.Count - num);
		}
	}

	private int MaxHorizontalOffset => Math.Max(0, TotalContentWidth - GridView.ClientSize.Width);

	private int VerticalTrackHeight
	{
		get
		{
			int num = base.ClientSize.Height - 6;
			if (NeedsHorizontalScroll)
			{
				num -= Gutter;
			}
			return Math.Max(0, num);
		}
	}

	private int HorizontalTrackWidth
	{
		get
		{
			int num = base.ClientSize.Width - 6;
			if (NeedsVerticalScroll)
			{
				num -= Gutter;
			}
			return Math.Max(0, num);
		}
	}

	public StyledEventGridHost()
	{
		DoubleBuffered = true;
		base.Padding = new Padding(0, 0, 13, 13);
		BackColor = Color.FromArgb(13, 16, 22);
		Grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.None,
			BorderStyle = BorderStyle.None
		};
		base.Controls.Add(Grid);
		DataGridView grid = Grid;
		ScrollEventHandler value = delegate
		{
			InvalidateScrollbarArea();
		};
		grid.Scroll += value;
		Grid.RowsAdded += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.RowsRemoved += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.SizeChanged += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.ColumnWidthChanged += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.MouseWheel += Grid_MouseWheel;
	}

	private Rectangle GetVerticalTrackRect()
	{
		return new Rectangle(base.ClientSize.Width - 7 - 3, 3, 7, VerticalTrackHeight);
	}

	private Rectangle GetHorizontalTrackRect()
	{
		int y = base.ClientSize.Height - 7 - 3;
		return new Rectangle(3, y, HorizontalTrackWidth, 7);
	}

	private Rectangle GetVerticalThumbRect()
	{
		if (!NeedsVerticalScroll || VerticalTrackHeight <= 0)
		{
			return Rectangle.Empty;
		}
		int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
		int maxFirstDisplayedRow = MaxFirstDisplayedRow;
		int num2 = Math.Max(0, GridView.FirstDisplayedScrollingRowIndex);
		int y = ((maxFirstDisplayedRow == 0) ? 3 : (3 + (int)((double)num2 / (double)maxFirstDisplayedRow * (double)(VerticalTrackHeight - num))));
		return new Rectangle(base.ClientSize.Width - 7 - 3, y, 7, num);
	}

	private Rectangle GetHorizontalThumbRect()
	{
		if (!NeedsHorizontalScroll || HorizontalTrackWidth <= 0)
		{
			return Rectangle.Empty;
		}
		int num = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
		int maxHorizontalOffset = MaxHorizontalOffset;
		int num2 = Math.Max(0, GridView.HorizontalScrollingOffset);
		int x = ((maxHorizontalOffset == 0) ? 3 : (3 + (int)((double)num2 / (double)maxHorizontalOffset * (double)(HorizontalTrackWidth - num))));
		int y = base.ClientSize.Height - 7 - 3;
		return new Rectangle(x, y, num, 7);
	}

	private void InvalidateScrollbarArea()
	{
		if (base.IsHandleCreated)
		{
			int gutter = Gutter;
			Invalidate(new Rectangle(base.ClientSize.Width - gutter, 0, gutter, base.ClientSize.Height));
			Invalidate(new Rectangle(0, base.ClientSize.Height - gutter, base.ClientSize.Width, gutter));
		}
	}

	private void Grid_MouseWheel(object sender, MouseEventArgs e)
	{
		if (HandleMouseWheel(e.Delta, (Control.ModifierKeys & Keys.Shift) == Keys.Shift))
		{
			InvalidateScrollbarArea();
		}
	}

	private bool HandleMouseWheel(int delta, bool horizontal)
	{
		if (delta == 0)
		{
			return false;
		}
		int num = Math.Max(1, Math.Abs(delta) / 120);
		if (horizontal && NeedsHorizontalScroll)
		{
			int num2 = ((delta <= 0) ? 1 : (-1));
			int val = GridView.HorizontalScrollingOffset + num2 * 40 * num;
			GridView.HorizontalScrollingOffset = Math.Max(0, Math.Min(MaxHorizontalOffset, val));
			return true;
		}
		if (!horizontal && NeedsVerticalScroll)
		{
			int num3 = ((delta <= 0) ? 1 : (-1));
			int val2 = GridView.FirstDisplayedScrollingRowIndex + num3 * num;
			GridView.FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(MaxFirstDisplayedRow, val2));
			return true;
		}
		return false;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (NeedsVerticalScroll)
		{
			using (SolidBrush solidBrush = new SolidBrush(TrackColor))
			{
				graphics.FillRectangle(solidBrush, GetVerticalTrackRect());
			}
			Rectangle verticalThumbRect = GetVerticalThumbRect();
			if (!verticalThumbRect.IsEmpty)
			{
				using SolidBrush solidBrush2 = new SolidBrush(ThumbColor);
				graphics.FillRectangle(solidBrush2, verticalThumbRect);
			}
		}
		if (!NeedsHorizontalScroll)
		{
			return;
		}
		using (SolidBrush solidBrush3 = new SolidBrush(TrackColor))
		{
			graphics.FillRectangle(solidBrush3, GetHorizontalTrackRect());
		}
		Rectangle horizontalThumbRect = GetHorizontalThumbRect();
		if (horizontalThumbRect.IsEmpty)
		{
			return;
		}
		using SolidBrush solidBrush4 = new SolidBrush(ThumbColor);
		graphics.FillRectangle(solidBrush4, horizontalThumbRect);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (!TryHandleScrollbarMouseDown(e.Location))
		{
			base.OnMouseDown(e);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (thumbDraggingV)
		{
			int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
			int num2 = VerticalTrackHeight - num;
			int val = e.Y - thumbDragOffsetV - 3;
			val = Math.Max(0, Math.Min(num2, val));
			int maxFirstDisplayedRow = MaxFirstDisplayedRow;
			GridView.FirstDisplayedScrollingRowIndex = ((num2 > 0) ? ((int)((double)val / (double)num2 * (double)maxFirstDisplayedRow)) : 0);
			InvalidateScrollbarArea();
		}
		else if (thumbDraggingH)
		{
			int num3 = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
			int num4 = HorizontalTrackWidth - num3;
			int val2 = e.X - thumbDragOffsetH - 3;
			val2 = Math.Max(0, Math.Min(num4, val2));
			int maxHorizontalOffset = MaxHorizontalOffset;
			GridView.HorizontalScrollingOffset = ((num4 > 0) ? ((int)((double)val2 / (double)num4 * (double)maxHorizontalOffset)) : 0);
			InvalidateScrollbarArea();
		}
		else
		{
			base.OnMouseMove(e);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		thumbDraggingV = false;
		thumbDraggingH = false;
		base.Capture = false;
	}

	private bool TryHandleScrollbarMouseDown(Point location)
	{
		if (NeedsVerticalScroll)
		{
			Rectangle verticalThumbRect = GetVerticalThumbRect();
			if (verticalThumbRect.Contains(location))
			{
				thumbDraggingV = true;
				thumbDragOffsetV = location.Y - verticalThumbRect.Y;
				base.Capture = true;
				return true;
			}
			if (GetVerticalTrackRect().Contains(location))
			{
				int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
				int num2 = location.Y - num / 2;
				int num3 = VerticalTrackHeight - num;
				int num4 = Math.Max(0, Math.Min(num3, num2 - 3));
				int maxFirstDisplayedRow = MaxFirstDisplayedRow;
				GridView.FirstDisplayedScrollingRowIndex = ((num3 > 0) ? ((int)((double)num4 / (double)num3 * (double)maxFirstDisplayedRow)) : 0);
				InvalidateScrollbarArea();
				return true;
			}
		}
		if (NeedsHorizontalScroll)
		{
			Rectangle horizontalThumbRect = GetHorizontalThumbRect();
			if (horizontalThumbRect.Contains(location))
			{
				thumbDraggingH = true;
				thumbDragOffsetH = location.X - horizontalThumbRect.X;
				base.Capture = true;
				return true;
			}
			if (GetHorizontalTrackRect().Contains(location))
			{
				int num5 = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
				int num6 = location.X - num5 / 2;
				int num7 = HorizontalTrackWidth - num5;
				int num8 = Math.Max(0, Math.Min(num7, num6 - 3));
				int maxHorizontalOffset = MaxHorizontalOffset;
				GridView.HorizontalScrollingOffset = ((num7 > 0) ? ((int)((double)num8 / (double)num7 * (double)maxHorizontalOffset)) : 0);
				InvalidateScrollbarArea();
				return true;
			}
		}
		return false;
	}
}

// ==========================================
// File: MainForm.cs
// ==========================================
internal sealed class MainForm : Form
{
	private const int HotkeyRecord = 1;

	private const int HotkeyPlay = 2;

	private const int HotkeyStop = 3;

	private const int WM_HOTKEY = 786;

	private const int ActionButtonHeight = 44;

	private readonly MacroRecorder recorder = new MacroRecorder();

	private readonly MacroPlayer player = new MacroPlayer();

	private StyledEventGridHost eventGridHost;

	private DataGridView eventGrid;

	private readonly Button editEventsButton = new Button();

	private readonly Label statusLabel = new Label();

	private readonly Label statsLabel = new Label();

	private readonly TextBox fileBox = new TextBox();

	private readonly TextBox speedBox = new TextBox();

	private readonly Label speedLabel = new Label();

	private readonly TextBox mouseScaleBox = new TextBox();

	private readonly Label mouseScaleLabel = new Label();

	private readonly CheckBox relativeMouseBox = new CheckBox();

	private readonly CheckBox restoreCursorBox = new CheckBox();

	private readonly CheckBox continuousPlaybackBox = new CheckBox();

	private readonly Button recordButton = new Button();

	private readonly Button playButton = new Button();

	private readonly Button stopButton = new Button();

	private readonly Button saveButton = new Button();

	private readonly Button loadButton = new Button();

	private MacroDocument document;

	private string currentFile;

	private StyledScrollPanel optionsScroll;

	private FlowLayoutPanel optionsPanel;

	private bool eventsEditMode;

	private static string MacrosDirectory
	{
		get
		{
			string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "..", "macros"));
			Directory.CreateDirectory(fullPath);
			return fullPath;
		}
	}

	private int SidebarContentWidth
	{
		get
		{
			if (optionsScroll == null || optionsScroll.ClientSize.Width <= 0)
			{
				return 286;
			}
			int width = optionsScroll.ClientSize.Width;
			return Math.Max(200, width - optionsScroll.Padding.Horizontal);
		}
	}

	public MainForm()
	{
		Text = "relay";
		base.Width = 900;
		base.Height = 580;
		MinimumSize = new Size(760, 480);
		BackColor = Color.FromArgb(15, 18, 24);
		ForeColor = Color.FromArgb(230, 234, 242);
		Font = new Font("Segoe UI", 10f);
		base.StartPosition = FormStartPosition.CenterScreen;
		try
		{
			string text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "relayico.ico");
			if (File.Exists(text))
			{
				base.Icon = new Icon(text);
			}
		}
		catch
		{
		}
		document = NewDocument();
		BuildUi();
		recorder.RecordingStopped += delegate
		{
			BeginInvoke(FinishRecording);
		};
		player.PlaybackStopped += delegate(string message)
		{
			BeginInvoke(delegate
			{
				FinishPlayback(message);
			});
		};
		UpdateUi("ready. f8 records, f9 plays, f10 stops.");
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		ResetOptionsScroll();
	}

	private void ResetOptionsScroll()
	{
		if (optionsScroll != null)
		{
			optionsScroll.AutoScrollPosition = new Point(0, 0);
			optionsScroll.Invalidate();
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.RegisterHotKey(base.Handle, 1, 0u, 119);
		NativeMethods.RegisterHotKey(base.Handle, 2, 0u, 120);
		NativeMethods.RegisterHotKey(base.Handle, 3, 0u, 121);
		NativeMethods.RegisterRawMouseInput(base.Handle);
	}

	protected override void OnHandleDestroyed(EventArgs e)
	{
		NativeMethods.UnregisterHotKey(base.Handle, 1);
		NativeMethods.UnregisterHotKey(base.Handle, 2);
		NativeMethods.UnregisterHotKey(base.Handle, 3);
		base.OnHandleDestroyed(e);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 786)
		{
			int num = ((IntPtr)m.WParam).ToInt32();
			if (num == 1)
			{
				ToggleRecording();
			}
			if (num == 2)
			{
				StartPlayback();
			}
			if (num == 3)
			{
				StopAll();
			}
		}
		else
		{
			if (m.Msg == 255 && recorder.IsRecording && NativeMethods.TryGetRawMouseDelta(m.LParam, out var dx, out var dy))
			{
				recorder.RecordRawMouse(dx, dy);
			}
			base.WndProc(ref m);
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		recorder.Dispose();
		player.Stop();
		base.OnFormClosing(e);
	}

	private void BuildUi()
	{
		Panel panel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 96,
			BackColor = Color.FromArgb(20, 25, 34),
			Padding = new Padding(20, 12, 20, 10)
		};
		Label value = new Label
		{
			Text = "relay",
			Font = new Font("Segoe UI Semibold", 22f),
			Dock = DockStyle.Top,
			Height = 44,
			ForeColor = Color.White
		};
		Label value2 = new Label
		{
			Text = "input automation utility.",
			Dock = DockStyle.Top,
			Height = 20,
			ForeColor = Color.FromArgb(160, 170, 185)
		};
		panel.Controls.Add(value2);
		panel.Controls.Add(value);
		statusLabel.Dock = DockStyle.Bottom;
		statusLabel.Height = 34;
		statusLabel.Padding = new Padding(24, 0, 24, 0);
		statusLabel.TextAlign = ContentAlignment.MiddleLeft;
		statusLabel.BackColor = Color.FromArgb(20, 25, 34);
		statusLabel.ForeColor = Color.FromArgb(185, 196, 212);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(16),
			ColumnCount = 2,
			RowCount = 1
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 318f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		base.Controls.Add(tableLayoutPanel);
		base.Controls.Add(panel);
		base.Controls.Add(statusLabel);
		StyledScrollPanel styledScrollPanel = new StyledScrollPanel();
		styledScrollPanel.Dock = DockStyle.Fill;
		styledScrollPanel.Margin = Padding.Empty;
		optionsScroll = styledScrollPanel;
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoSize = true;
		flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		flowLayoutPanel.Dock = DockStyle.Top;
		flowLayoutPanel.Margin = Padding.Empty;
		flowLayoutPanel.Padding = new Padding(0, 4, 0, 12);
		optionsPanel = flowLayoutPanel;
		optionsScroll.Controls.Add(optionsPanel);
		optionsScroll.Resize += delegate
		{
			LayoutSidebarControls();
		};
		tableLayoutPanel.Controls.Add(optionsScroll, 0, 0);
		recordButton.Text = "record / stop  (f8)";
		playButton.Text = "play  (f9)";
		stopButton.Text = "stop  (f10)";
		saveButton.Text = "save readable macro";
		loadButton.Text = "load macro";
		StyleSidebarButton(recordButton, Color.FromArgb(221, 72, 83));
		StyleSidebarButton(playButton, Color.FromArgb(66, 179, 118));
		StyleSidebarButton(stopButton, Color.FromArgb(89, 101, 122));
		StyleSidebarButton(saveButton, Color.FromArgb(74, 123, 232));
		StyleSidebarButton(loadButton, Color.FromArgb(74, 123, 232));
		recordButton.Click += delegate
		{
			ToggleRecording();
		};
		playButton.Click += delegate
		{
			StartPlayback();
		};
		stopButton.Click += delegate
		{
			StopAll();
		};
		saveButton.Click += delegate
		{
			SaveMacro();
		};
		loadButton.Click += delegate
		{
			LoadMacro();
		};
		optionsPanel.Controls.Add(recordButton);
		optionsPanel.Controls.Add(playButton);
		optionsPanel.Controls.Add(stopButton);
		optionsPanel.Controls.Add(Spacer(10));
		relativeMouseBox.Text = "game-style relative mouse moves";
		relativeMouseBox.Checked = true;
		relativeMouseBox.Height = 28;
		relativeMouseBox.Margin = new Padding(0, 0, 0, 6);
		relativeMouseBox.ForeColor = Color.FromArgb(230, 234, 242);
		optionsPanel.Controls.Add(relativeMouseBox);
		restoreCursorBox.Text = "restore start cursor before playback";
		restoreCursorBox.Checked = true;
		restoreCursorBox.Height = 28;
		restoreCursorBox.Margin = new Padding(0, 0, 0, 6);
		restoreCursorBox.ForeColor = Color.FromArgb(230, 234, 242);
		optionsPanel.Controls.Add(restoreCursorBox);
		continuousPlaybackBox.Text = "continuous playback";
		continuousPlaybackBox.Checked = false;
		continuousPlaybackBox.Height = 28;
		continuousPlaybackBox.Margin = new Padding(0, 0, 0, 6);
		continuousPlaybackBox.ForeColor = Color.FromArgb(230, 234, 242);
		optionsPanel.Controls.Add(continuousPlaybackBox);
		speedLabel.Text = "speed multiplier";
		speedLabel.Height = 24;
		speedLabel.Margin = new Padding(0, 2, 0, 4);
		speedLabel.ForeColor = Color.FromArgb(190, 200, 214);
		optionsPanel.Controls.Add(speedLabel);
		speedBox.Text = "1.00";
		StyleNumberBox(speedBox);
		optionsPanel.Controls.Add(speedBox);
		mouseScaleLabel.Text = "mouse distance multiplier";
		mouseScaleLabel.Height = 24;
		mouseScaleLabel.Margin = new Padding(0, 2, 0, 4);
		mouseScaleLabel.ForeColor = Color.FromArgb(190, 200, 214);
		optionsPanel.Controls.Add(mouseScaleLabel);
		mouseScaleBox.Text = "1.00";
		StyleNumberBox(mouseScaleBox);
		optionsPanel.Controls.Add(mouseScaleBox);
		optionsPanel.Controls.Add(Spacer(10));
		optionsPanel.Controls.Add(saveButton);
		optionsPanel.Controls.Add(loadButton);
		optionsPanel.Controls.Add(Spacer(10));
		Label value3 = new Label
		{
			Text = "current file",
			Height = 22,
			Margin = new Padding(0, 0, 0, 4),
			ForeColor = Color.FromArgb(160, 170, 185)
		};
		optionsPanel.Controls.Add(value3);
		fileBox.ReadOnly = true;
		fileBox.Height = 28;
		fileBox.Margin = new Padding(0, 0, 0, 10);
		fileBox.BackColor = Color.FromArgb(25, 31, 42);
		fileBox.ForeColor = Color.FromArgb(230, 234, 242);
		fileBox.BorderStyle = BorderStyle.FixedSingle;
		optionsPanel.Controls.Add(fileBox);
		statsLabel.Height = 84;
		statsLabel.Margin = Padding.Empty;
		statsLabel.ForeColor = Color.FromArgb(190, 200, 214);
		optionsPanel.Controls.Add(statsLabel);
		LayoutSidebarControls();
		ResetOptionsScroll();
		Panel panel2 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(19, 23, 31),
			Padding = new Padding(14)
		};
		tableLayoutPanel.Controls.Add(panel2, 1, 0);
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 38,
			Padding = new Padding(0, 0, 0, 4)
		};
		Label value4 = new Label
		{
			Text = "recorded events",
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Font = new Font("Segoe UI Semibold", 12f),
			ForeColor = Color.White
		};
		editEventsButton.Text = "edit events";
		editEventsButton.Dock = DockStyle.Right;
		editEventsButton.Width = 108;
		editEventsButton.Height = 30;
		editEventsButton.FlatStyle = FlatStyle.Flat;
		editEventsButton.FlatAppearance.BorderSize = 0;
		editEventsButton.BackColor = Color.FromArgb(74, 123, 232);
		editEventsButton.ForeColor = Color.White;
		editEventsButton.Font = new Font("Segoe UI Semibold", 9f);
		editEventsButton.Cursor = Cursors.Hand;
		editEventsButton.Click += delegate
		{
			ToggleEventsEditMode();
		};
		panel3.Controls.Add(editEventsButton);
		panel3.Controls.Add(value4);
		eventGridHost = new StyledEventGridHost();
		eventGrid = eventGridHost.Grid;
		SetupEventGrid();
		eventGridHost.Dock = DockStyle.Fill;
		panel2.Controls.Add(eventGridHost);
		panel2.Controls.Add(panel3);
	}

	private void SetupEventGrid()
	{
		eventGrid.AllowUserToAddRows = false;
		eventGrid.AllowUserToDeleteRows = false;
		eventGrid.AllowUserToResizeRows = false;
		eventGrid.RowHeadersVisible = false;
		eventGrid.ReadOnly = true;
		eventGrid.MultiSelect = false;
		eventGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		eventGrid.BorderStyle = BorderStyle.None;
		eventGrid.BackgroundColor = Color.FromArgb(13, 16, 22);
		eventGrid.GridColor = Color.FromArgb(35, 42, 56);
		eventGrid.EnableHeadersVisualStyles = false;
		eventGrid.ScrollBars = ScrollBars.None;
		eventGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
		eventGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		eventGrid.ColumnHeadersHeight = 28;
		eventGrid.RowTemplate.Height = 24;
		eventGrid.Font = new Font("Consolas", 9.5f);
		DataGridViewCellStyle columnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(25, 31, 42),
			ForeColor = Color.FromArgb(190, 200, 214),
			SelectionBackColor = Color.FromArgb(25, 31, 42),
			SelectionForeColor = Color.FromArgb(190, 200, 214),
			Alignment = DataGridViewContentAlignment.MiddleLeft,
			Font = new Font("Segoe UI Semibold", 9f)
		};
		eventGrid.ColumnHeadersDefaultCellStyle = columnHeadersDefaultCellStyle;
		DataGridViewCellStyle defaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(13, 16, 22),
			ForeColor = Color.FromArgb(220, 226, 236),
			SelectionBackColor = Color.FromArgb(50, 62, 84),
			SelectionForeColor = Color.White
		};
		eventGrid.DefaultCellStyle = defaultCellStyle;
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colT",
			HeaderText = "ms",
			ValueType = typeof(long)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colType",
			HeaderText = "type",
			ValueType = typeof(string)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colX",
			HeaderText = "x",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colY",
			HeaderText = "y",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colDx",
			HeaderText = "dx",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colDy",
			HeaderText = "dy",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colButton",
			HeaderText = "btn",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colWheel",
			HeaderText = "wheel",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colVk",
			HeaderText = "vk",
			ValueType = typeof(int)
		});
		eventGrid.Columns.Add(new DataGridViewTextBoxColumn
		{
			Name = "colKey",
			HeaderText = "key",
			ValueType = typeof(string)
		});
		eventGrid.CellEndEdit += EventGrid_CellEndEdit;
	}

	private void ToggleEventsEditMode()
	{
		SetEventsEditMode(!eventsEditMode);
		UpdateUi(eventsEditMode ? "editing events. click done editing when finished." : "event editing locked.");
	}

	private void SetEventsEditMode(bool enabled)
	{
		if (enabled && (recorder.IsRecording || player.IsPlaying || document.events == null || document.events.Count == 0))
		{
			enabled = false;
		}
		if (!enabled && eventsEditMode)
		{
			eventGrid.EndEdit();
		}
		eventsEditMode = enabled;
		eventGrid.ReadOnly = !enabled;
		editEventsButton.Text = (enabled ? "done editing" : "edit events");
		editEventsButton.BackColor = (enabled ? Color.FromArgb(100, 110, 130) : Color.FromArgb(74, 123, 232));
	}

	private void EventGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		if (eventsEditMode && e.RowIndex >= 0)
		{
			CommitEventRow(e.RowIndex);
		}
	}

	private void CommitEventRow(int rowIndex)
	{
		if (document.events == null || rowIndex < 0 || rowIndex >= document.events.Count)
		{
			return;
		}
		MacroEvent macroEvent = document.events[rowIndex];
		DataGridViewRow dataGridViewRow = eventGrid.Rows[rowIndex];
		try
		{
			macroEvent.t = Math.Max(0L, ParseLongCell(dataGridViewRow.Cells["colT"], macroEvent.t));
			string text = Convert.ToString(dataGridViewRow.Cells["colType"].Value, CultureInfo.InvariantCulture);
			if (!string.IsNullOrWhiteSpace(text))
			{
				macroEvent.type = text.Trim();
			}
			macroEvent.x = ParseIntCell(dataGridViewRow.Cells["colX"], macroEvent.x);
			macroEvent.y = ParseIntCell(dataGridViewRow.Cells["colY"], macroEvent.y);
			macroEvent.dx = ParseIntCell(dataGridViewRow.Cells["colDx"], macroEvent.dx);
			macroEvent.dy = ParseIntCell(dataGridViewRow.Cells["colDy"], macroEvent.dy);
			macroEvent.button = ParseIntCell(dataGridViewRow.Cells["colButton"], macroEvent.button);
			macroEvent.wheel = ParseIntCell(dataGridViewRow.Cells["colWheel"], macroEvent.wheel);
			macroEvent.vk = ParseIntCell(dataGridViewRow.Cells["colVk"], macroEvent.vk);
			string text2 = Convert.ToString(dataGridViewRow.Cells["colKey"].Value, CultureInfo.InvariantCulture);
			macroEvent.key = (string.IsNullOrWhiteSpace(text2) ? null : text2.Trim());
			document.events.Sort((MacroEvent a, MacroEvent b) => a.t.CompareTo(b.t));
			RefreshEventList();
			UpdateUi("event updated.");
		}
		catch (FormatException)
		{
			RefreshEventList();
			UpdateUi("invalid value. changes were not saved.");
		}
	}

	private static long ParseLongCell(DataGridViewCell cell, long fallback)
	{
		if (cell.Value == null || cell.Value == DBNull.Value)
		{
			return fallback;
		}
		if (long.TryParse(Convert.ToString(cell.Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		throw new FormatException();
	}

	private static int ParseIntCell(DataGridViewCell cell, int fallback)
	{
		if (cell.Value == null || cell.Value == DBNull.Value)
		{
			return fallback;
		}
		if (int.TryParse(Convert.ToString(cell.Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		throw new FormatException();
	}

	private void LayoutSidebarControls()
	{
		if (optionsPanel == null)
		{
			return;
		}
		int sidebarContentWidth = SidebarContentWidth;
		optionsPanel.Width = sidebarContentWidth;
		recordButton.Width = sidebarContentWidth;
		playButton.Width = sidebarContentWidth;
		stopButton.Width = sidebarContentWidth;
		relativeMouseBox.Width = sidebarContentWidth;
		restoreCursorBox.Width = sidebarContentWidth;
		continuousPlaybackBox.Width = sidebarContentWidth;
		speedLabel.Width = sidebarContentWidth;
		speedBox.Width = sidebarContentWidth;
		mouseScaleLabel.Width = sidebarContentWidth;
		mouseScaleBox.Width = sidebarContentWidth;
		saveButton.Width = sidebarContentWidth;
		loadButton.Width = sidebarContentWidth;
		fileBox.Width = sidebarContentWidth;
		statsLabel.Width = sidebarContentWidth;
		foreach (Control control in optionsPanel.Controls)
		{
			Label label = (Label)((control is Label) ? control : null);
			if (label != null && label != statsLabel)
			{
				label.Width = sidebarContentWidth;
			}
		}
	}

	private static Control Spacer(int height)
	{
		return new Label
		{
			Text = "",
			Height = height,
			Margin = Padding.Empty
		};
	}

	private void StyleSidebarButton(Button button, Color color)
	{
		button.Height = 44;
		button.Width = SidebarContentWidth;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.UseVisualStyleBackColor = false;
		button.BackColor = color;
		button.ForeColor = Color.White;
		button.Font = new Font("Segoe UI Semibold", 10f);
		button.Margin = new Padding(0, 0, 0, 8);
		button.Cursor = Cursors.Hand;
	}

	private void StyleNumberBox(TextBox textBox)
	{
		textBox.Width = SidebarContentWidth;
		textBox.Height = 28;
		textBox.BackColor = Color.FromArgb(25, 31, 42);
		textBox.ForeColor = Color.FromArgb(230, 234, 242);
		textBox.BorderStyle = BorderStyle.FixedSingle;
		textBox.Font = new Font("Consolas", 11f);
		textBox.Margin = new Padding(0, 0, 0, 8);
	}

	private void ToggleRecording()
	{
		if (!player.IsPlaying)
		{
			if (recorder.IsRecording)
			{
				recorder.Stop();
				return;
			}
			currentFile = null;
			fileBox.Text = "";
			SetEventsEditMode(enabled: false);
			document = NewDocument();
			recorder.Start(document);
			UpdateUi("recording. press f8 again to stop.");
		}
	}

	private void FinishRecording()
	{
		document.events = recorder.Events;
		UpdateUi("recording stopped. save it as .relay.json when ready.");
		RefreshEventList();
	}

	private void StartPlayback()
	{
		if (!recorder.IsRecording && !player.IsPlaying && document != null && document.events != null && document.events.Count != 0)
		{
			double speed = PlaybackSpeed();
			double mouseScale = MouseScale();
			UpdateUi(continuousPlaybackBox.Checked ? "looping macro. press f10 to stop." : "playing macro. press f10 to stop.");
			player.Play(document, relativeMouseBox.Checked, restoreCursorBox.Checked, speed, mouseScale, continuousPlaybackBox.Checked);
		}
	}

	private void FinishPlayback(string message)
	{
		UpdateUi(message);
	}

	private void StopAll()
	{
		if (recorder.IsRecording)
		{
			recorder.Stop();
		}
		if (player.IsPlaying)
		{
			player.Stop();
		}
		UpdateUi("stopped.");
	}

	private double PlaybackSpeed()
	{
		double result = ParseMultiplier(speedBox.Text, 1.0, 0.05, 10.0);
		speedBox.Text = result.ToString("0.##", CultureInfo.InvariantCulture);
		return result;
	}

	private double MouseScale()
	{
		double result = ParseMultiplier(mouseScaleBox.Text, 1.0, 0.01, 20.0);
		mouseScaleBox.Text = result.ToString("0.##", CultureInfo.InvariantCulture);
		return result;
	}

	private static double ParseMultiplier(string text, double fallback, double min, double max)
	{
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
		{
			result = fallback;
		}
		if (double.IsNaN(result) || double.IsInfinity(result))
		{
			result = fallback;
		}
		if (result < min)
		{
			result = min;
		}
		if (result > max)
		{
			result = max;
		}
		return result;
	}

	private static MacroDocument NewDocument()
	{
		Rectangle virtualScreen = SystemInformation.VirtualScreen;
		return new MacroDocument
		{
			format = "clear-macro v1",
			app = "relay",
			createdUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
			recordingScreen = new ScreenInfo
			{
				width = virtualScreen.Width,
				height = virtualScreen.Height,
				left = virtualScreen.Left,
				top = virtualScreen.Top
			},
			startMouse = null,
			events = new List<MacroEvent>()
		};
	}

	private void SaveMacro()
	{
		if (document.events.Count == 0)
		{
			UpdateUi("nothing recorded yet.");
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		try
		{
			saveFileDialog.Title = "Save relay macro file";
			saveFileDialog.Filter = "relay macro (*.relay.json)|*.relay.json|json (*.json)|*.json";
			saveFileDialog.InitialDirectory = MacrosDirectory;
			saveFileDialog.FileName = "macro.relay.json";
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				File.WriteAllText(saveFileDialog.FileName, MacroJson.Serialize(document), Encoding.UTF8);
				currentFile = saveFileDialog.FileName;
				fileBox.Text = currentFile;
				UpdateUi("saved readable macro.");
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void LoadMacro()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		try
		{
			openFileDialog.Title = "Load relay macro file";
			openFileDialog.Filter = "relay macro (*.relay.json;*.json)|*.relay.json;*.json|all files (*.*)|*.*";
			openFileDialog.InitialDirectory = MacrosDirectory;
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				string json = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
				document = MacroJson.Deserialize(json);
				if (document.events == null)
				{
					document.events = new List<MacroEvent>();
				}
				SetEventsEditMode(enabled: false);
				currentFile = openFileDialog.FileName;
				fileBox.Text = currentFile;
				RefreshEventList();
				UpdateUi("loaded macro.");
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private void RefreshEventList()
	{
		MacroEvent macroEvent = null;
		if (eventGrid.CurrentCell != null)
		{
			int rowIndex = eventGrid.CurrentCell.RowIndex;
			if (rowIndex >= 0 && document.events != null && rowIndex < document.events.Count)
			{
				macroEvent = document.events[rowIndex];
			}
		}
		eventGrid.Rows.Clear();
		if (document.events == null)
		{
			return;
		}
		foreach (MacroEvent @event in document.events)
		{
			eventGrid.Rows.Add(@event.t, @event.type, @event.x, @event.y, @event.dx, @event.dy, @event.button, @event.wheel, @event.vk, @event.key ?? "");
		}
		if (macroEvent != null)
		{
			int num = document.events.IndexOf(macroEvent);
			if (num >= 0 && num < eventGrid.Rows.Count)
			{
				eventGrid.CurrentCell = eventGrid.Rows[num].Cells[0];
			}
		}
	}

	private void UpdateUi(string status)
	{
		statusLabel.Text = status;
		int num = ((document.events != null) ? document.events.Count : 0);
		long durationMs = document.GetDurationMs();
		statsLabel.Text = string.Format(CultureInfo.InvariantCulture, "events: {0}\r\nduration: {1:0.00}s\r\nformat: readable json\r\nmouse: {2}", num, (double)durationMs / 1000.0, relativeMouseBox.Checked ? "relative/game" : "absolute/desktop");
		bool flag = recorder.IsRecording || player.IsPlaying;
		playButton.Enabled = !flag && num > 0;
		saveButton.Enabled = !flag && num > 0;
		loadButton.Enabled = !flag;
		recordButton.Enabled = !player.IsPlaying;
		editEventsButton.Enabled = !flag && num > 0;
		if (flag)
		{
			SetEventsEditMode(enabled: false);
		}
	}
}

// ==========================================
// File: Program.cs
// ==========================================
internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new MainForm());
	}
}

