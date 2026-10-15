namespace relay;

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
