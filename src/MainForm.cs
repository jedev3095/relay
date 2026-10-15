namespace relay;

internal sealed class MainForm : Form
{
	private const int HotkeyRecord = 1;

	private const int HotkeyPlay = 2;

	private const int HotkeyStop = 3;

	private const int HotkeyClick = 4;

	private const int HotkeySave = 5;

	private const int HotkeyLoad = 6;

	private const int WM_HOTKEY = 786;

	private const int ActionButtonHeight = 44;

	private readonly MacroRecorder recorder = new MacroRecorder();

	private readonly MacroPlayer player = new MacroPlayer();

	private readonly AutoClicker clicker = new AutoClicker();

	private readonly RelaySettings settings = RelaySettings.Load();

	private readonly Dictionary<string, Button> hotkeyButtons = new Dictionary<string, Button>();

	private readonly NumericUpDown clickIntervalBox = new NumericUpDown();

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

	private readonly Button clickButton = new Button();

	private readonly Button saveButton = new Button();

	private readonly Button loadButton = new Button();

	private MacroDocument document;

	private string currentFile;

	private StyledScrollPanel optionsScroll;

	private FlowLayoutPanel optionsPanel;

	private bool eventsEditMode;

	private bool capturingHotkey;

	private WebView2 webView;

	private Control[] legacyControls;

	private bool webReady;

	private Size normalSize = new Size(970, 580);

	private static string MacrosDirectory
	{
		get
		{
			string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Relay", "macros");
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
		base.Width = settings.CompactMode ? 440 : 970;
		base.Height = 580;
		MinimumSize = settings.CompactMode ? new Size(400, 500) : new Size(760, 480);
		BackColor = Color.FromArgb(15, 18, 24);
		ForeColor = Color.FromArgb(230, 234, 242);
		Font = new Font("Segoe UI", 10f);
		base.StartPosition = FormStartPosition.CenterScreen;
		try
		{
			base.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
		document = NewDocument();
		recorder.IsHotkeyKey = IsHotkeyKey;
		BuildUi();
		BuildWebUi();
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
		UpdateUi("ready.");
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		ResetOptionsScroll();
		InitializeWebUi();
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
		RegisterHotkeys();
		NativeMethods.RegisterRawMouseInput(base.Handle);
	}

	protected override void OnHandleDestroyed(EventArgs e)
	{
		UnregisterHotkeys();
		base.OnHandleDestroyed(e);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == WM_HOTKEY)
		{
			int num = ((IntPtr)m.WParam).ToInt32();
			if (capturingHotkey) return;
			if (num == HotkeyRecord)
			{
				ToggleRecording();
			}
			if (num == HotkeyPlay)
			{
				StartPlayback();
			}
			if (num == HotkeyStop)
			{
				StopAll();
			}
			if (num == HotkeyClick) ToggleClicker();
			if (num == HotkeySave) SaveMacro();
			if (num == HotkeyLoad) LoadMacro();
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
		clicker.Dispose();
		base.OnFormClosing(e);
	}

	private void BuildWebUi()
	{
		legacyControls = new Control[Controls.Count];
		Controls.CopyTo(legacyControls, 0);
		foreach (Control control in legacyControls) control.Visible = false;
		webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(24, 26, 29) };
		Controls.Add(webView);
		webView.BringToFront();
	}

	private async void InitializeWebUi()
	{
		try
		{
			string webData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Relay", "WebView2");
			CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(userDataFolder: webData);
			await webView.EnsureCoreWebView2Async(environment);
			webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
			webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
			webReady = true;
			using Stream ui = typeof(MainForm).Assembly.GetManifestResourceStream("relay.ui.index.html")
				?? throw new FileNotFoundException("embedded interface missing");
			using StreamReader reader = new StreamReader(ui, Encoding.UTF8);
			webView.NavigateToString(reader.ReadToEnd());
		}
		catch (Exception ex)
		{
			webReady = false;
			webView.Visible = false;
			foreach (Control control in legacyControls) control.Visible = true;
			UpdateUi("web interface unavailable: " + ex.Message);
		}
	}

	private void SendWebState()
	{
		if (!webReady || webView?.CoreWebView2 == null) return;
		Dictionary<string, string> hotkeys = new Dictionary<string, string>();
		foreach (var pair in settings.Hotkeys) hotkeys[pair.Key] = pair.Value.ToString();
		var state = new
		{
			status = statusLabel.Text,
			eventCount = document.events?.Count ?? 0,
			durationMs = document.GetDurationMs(),
			file = currentFile ?? "",
			recording = recorder.IsRecording,
			playing = player.IsPlaying,
			clicking = clicker.IsRunning,
			busy = recorder.IsRecording || player.IsPlaying,
			editing = eventsEditMode,
			events = document.events?.Select(item => new
			{
				item.t, item.type, item.x, item.y, item.dx, item.dy, item.button, item.wheel,
				key = (item.type == "key_down" || item.type == "key_up")
					? ((Keys)item.vk).ToString() : item.key
			}),
			hotkeys,
			options = new
			{
				relativeMouse = relativeMouseBox.Checked,
				restoreCursor = restoreCursorBox.Checked,
				continuousPlayback = continuousPlaybackBox.Checked,
				speed = speedBox.Text,
				mouseScale = mouseScaleBox.Text,
				clickInterval = settings.ClickIntervalMs,
				clickButton = settings.ClickButton,
				compactMode = settings.CompactMode,
				showEvents = settings.ShowEventsTable
			}
		};
		webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(state));
	}

	private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
	{
		try
		{
			using JsonDocument message = JsonDocument.Parse(e.WebMessageAsJson);
			JsonElement root = message.RootElement;
			string type = root.GetProperty("type").GetString();
			switch (type)
			{
			case "ready": SendWebState(); break;
			case "record": ToggleRecording(); break;
			case "play": StartPlayback(); break;
			case "stop": StopAll(); break;
			case "click": ToggleClicker(); break;
			case "save": SaveMacro(); break;
			case "load": LoadMacro(); break;
			case "edit": ToggleEventsEditMode(); break;
			case "option": SetWebOption(root.GetProperty("name").GetString(), root.GetProperty("value")); break;
			case "editEvent": SetWebEvent(root); break;
			case "beginHotkey": capturingHotkey = true; UnregisterHotkeys(); break;
			case "cancelHotkey": capturingHotkey = false; RegisterHotkeys(); SendWebState(); break;
			case "hotkey": SetWebHotkey(root); break;
			}
		}
		catch (Exception ex)
		{
			UpdateUi("interface error: " + ex.Message);
		}
	}

	private void SetWebOption(string name, JsonElement value)
	{
		bool saved = true;
		switch (name)
		{
		case "relativeMouse": relativeMouseBox.Checked = value.GetBoolean(); break;
		case "restoreCursor": restoreCursorBox.Checked = value.GetBoolean(); break;
		case "continuousPlayback": continuousPlaybackBox.Checked = value.GetBoolean(); break;
		case "speed": speedBox.Text = value.GetString(); PlaybackSpeed(); break;
		case "mouseScale": mouseScaleBox.Text = value.GetString(); MouseScale(); break;
		case "clickInterval":
			if (clicker.IsRunning) clicker.Stop();
			settings.ClickIntervalMs = Math.Clamp(int.Parse(value.GetString(), CultureInfo.InvariantCulture), 10, 60000);
			saved = SaveSettings();
			break;
		case "clickButton":
			if (clicker.IsRunning) clicker.Stop();
			settings.ClickButton = Math.Clamp(int.Parse(value.GetString(), CultureInfo.InvariantCulture), 1, 3);
			saved = SaveSettings();
			break;
		case "compactMode":
			ApplyCompactMode(value.GetBoolean());
			saved = SaveSettings();
			break;
		case "showEvents":
			settings.ShowEventsTable = value.GetBoolean();
			saved = SaveSettings();
			break;
		default: return;
		}
		if (saved) UpdateUi("settings updated.");
	}

	private void ApplyCompactMode(bool enabled)
	{
		if (settings.CompactMode == enabled) return;
		if (enabled) normalSize = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
		Rectangle area = Screen.FromControl(this).WorkingArea;
		Point center = new Point(Left + Width / 2, Top + Height / 2);
		WindowState = FormWindowState.Normal;
		MinimumSize = enabled ? new Size(400, 500) : new Size(760, 480);
		Size target = enabled ? new Size(440, 580) : normalSize;
		Size = new Size(Math.Min(target.Width, area.Width), Math.Min(target.Height, area.Height));
		Location = new Point(
			Math.Clamp(center.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)),
			Math.Clamp(center.Y - Height / 2, area.Top, Math.Max(area.Top, area.Bottom - Height)));
		settings.CompactMode = enabled;
	}

	private void SetWebEvent(JsonElement root)
	{
		if (!eventsEditMode || recorder.IsRecording || player.IsPlaying) return;
		int index = root.GetProperty("index").GetInt32();
		if (index < 0 || index >= document.events.Count) return;
		string field = root.GetProperty("field").GetString();
		string value = root.GetProperty("value").GetString() ?? "";
		MacroEvent item = document.events[index];
		bool valid = true;
		if (field == "type") item.type = value.Trim();
		else if (field == "key")
		{
			if ((item.type == "key_down" || item.type == "key_up") &&
				Enum.TryParse(value.Trim(), true, out Keys key) && key != Keys.None &&
				((int)key & ~255) == 0 && Enum.IsDefined(typeof(Keys), key))
			{
				int oldVk = item.vk;
				int step = item.type == "key_down" ? 1 : -1;
				string pairedType = item.type == "key_down" ? "key_up" : "key_down";
				int pairIndex = -1;
				for (int i = index + step; i >= 0 && i < document.events.Count; i += step)
				{
					if (document.events[i].vk == oldVk && document.events[i].type == pairedType)
					{
						pairIndex = i;
						break;
					}
				}
				int start = Math.Min(index, pairIndex < 0 ? index : pairIndex);
				int end = Math.Max(index, pairIndex < 0 ? index : pairIndex);
				for (int i = start; i <= end; i++)
				{
					MacroEvent keyEvent = document.events[i];
					if (keyEvent.vk != oldVk || (keyEvent.type != "key_down" && keyEvent.type != "key_up")) continue;
					keyEvent.key = key.ToString();
					keyEvent.vk = (int)key;
					keyEvent.scanCode = 0;
				}
			}
			else valid = false;
		}
		else if (field == "t")
		{
			if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number)) item.t = Math.Max(0, number);
			else valid = false;
		}
		else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
		{
			switch (field)
			{
			case "x": item.x = number; break;
			case "y": item.y = number; break;
			case "dx": item.dx = number; break;
			case "dy": item.dy = number; break;
			case "button": item.button = number; break;
			case "wheel": item.wheel = number; break;
			case "vk": item.vk = number; break;
			default: valid = false; break;
			}
		}
		else valid = false;
		if (valid)
		{
			document.events.Sort((MacroEvent a, MacroEvent b) => a.t.CompareTo(b.t));
			RefreshEventList();
			UpdateUi("event updated.");
		}
		else UpdateUi("invalid event value.");
	}

	private void SetWebHotkey(JsonElement root)
	{
		string action = root.GetProperty("action").GetString();
		string code = root.GetProperty("code").GetString();
		Keys key = KeyFromWebCode(code);
		Keys modifiers = (root.GetProperty("ctrl").GetBoolean() ? Keys.Control : Keys.None) |
			(root.GetProperty("shift").GetBoolean() ? Keys.Shift : Keys.None) |
			(root.GetProperty("alt").GetBoolean() ? Keys.Alt : Keys.None);
		capturingHotkey = false;
		if (Array.IndexOf(RelaySettings.Actions, action) < 0 || key == Keys.None)
		{
			RegisterHotkeys();
			UpdateUi("unsupported shortcut. choose another key.");
			return;
		}
		foreach (var pair in settings.Hotkeys)
		{
			if (pair.Key != action && pair.Value.Key == key && pair.Value.Modifiers == modifiers)
			{
				RegisterHotkeys();
				UpdateUi("that shortcut is already assigned.");
				return;
			}
		}
		int id = Array.IndexOf(RelaySettings.Actions, action) + 1;
		HotkeyBinding next = new HotkeyBinding { Key = key, Modifiers = modifiers };
		if (!NativeMethods.RegisterHotKey(Handle, id, next.NativeModifiers | 0x4000u, (int)key))
		{
			RegisterHotkeys();
			UpdateUi("shortcut unavailable. choose another.");
			return;
		}
		NativeMethods.UnregisterHotKey(Handle, id);
		settings.Hotkeys[action] = next;
		RegisterHotkeys();
		if (SaveSettings()) UpdateUi(action + " shortcut set to " + next + ".");
	}

	private static Keys KeyFromWebCode(string code)
	{
		if (string.IsNullOrWhiteSpace(code)) return Keys.None;
		if (code.StartsWith("Key") && code.Length == 4 && Enum.TryParse(code.Substring(3), out Keys letter)) return letter;
		if (code.StartsWith("Digit") && code.Length == 6 && char.IsDigit(code[5])) return (Keys)((int)Keys.D0 + code[5] - '0');
		if (code.StartsWith("Numpad") && code.Length == 7 && char.IsDigit(code[6])) return (Keys)((int)Keys.NumPad0 + code[6] - '0');
		if (Enum.TryParse(code, out Keys key) && (int)key > 0 && (int)key < 256) return key;
		return code switch
		{
			"Space" => Keys.Space,
			"ArrowUp" => Keys.Up,
			"ArrowDown" => Keys.Down,
			"ArrowLeft" => Keys.Left,
			"ArrowRight" => Keys.Right,
			"Backquote" => Keys.Oemtilde,
			"Minus" => Keys.OemMinus,
			"Equal" => Keys.Oemplus,
			"BracketLeft" => Keys.OemOpenBrackets,
			"BracketRight" => Keys.OemCloseBrackets,
			"Backslash" => Keys.OemPipe,
			"Semicolon" => Keys.OemSemicolon,
			"Quote" => Keys.OemQuotes,
			"Comma" => Keys.Oemcomma,
			"Period" => Keys.OemPeriod,
			"Slash" => Keys.OemQuestion,
			_ => Keys.None
		};
	}

	private void BuildUi()
	{
		TableLayoutPanel tabs = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(15, 18, 24),
			ColumnCount = 1,
			RowCount = 2,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		tabs.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		tabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		Panel tabStrip = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 25, 34) };
		Panel tabContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 18, 24) };
		Panel mainTab = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 18, 24) };
		Panel settingsTab = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 18, 24), Visible = false };
		Button macroTabButton = new Button { Text = "macro", Location = new Point(16, 5), Size = new Size(110, 32) };
		Button settingsTabButton = new Button { Text = "settings", Location = new Point(132, 5), Size = new Size(110, 32) };
		foreach (Button button in new[] { macroTabButton, settingsTabButton })
		{
			button.FlatStyle = FlatStyle.Flat;
			button.FlatAppearance.BorderSize = 0;
			button.ForeColor = Color.White;
			button.Font = new Font("Segoe UI Semibold", 10f);
			button.Cursor = Cursors.Hand;
		}
		void SelectTab(bool showSettings)
		{
			mainTab.Visible = !showSettings;
			settingsTab.Visible = showSettings;
			macroTabButton.BackColor = showSettings ? Color.FromArgb(34, 42, 56) : Color.FromArgb(74, 123, 232);
			settingsTabButton.BackColor = showSettings ? Color.FromArgb(74, 123, 232) : Color.FromArgb(34, 42, 56);
		}
		macroTabButton.Click += delegate { SelectTab(false); };
		settingsTabButton.Click += delegate { SelectTab(true); };
		tabStrip.Controls.Add(macroTabButton);
		tabStrip.Controls.Add(settingsTabButton);
		tabContent.Controls.Add(mainTab);
		tabContent.Controls.Add(settingsTab);
		tabs.Controls.Add(tabStrip, 0, 0);
		tabs.Controls.Add(tabContent, 0, 1);
		BuildSettingsTab(settingsTab);
		SelectTab(false);
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
		mainTab.Controls.Add(tableLayoutPanel);
		base.Controls.Add(tabs);
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
		flowLayoutPanel.Margin = Padding.Empty;
		flowLayoutPanel.Padding = new Padding(0, 4, 0, 12);
		optionsPanel = flowLayoutPanel;
		optionsPanel.Layout += delegate { optionsScroll.RefreshContentHeight(); };
		optionsScroll.Controls.Add(optionsPanel);
		optionsScroll.Resize += delegate
		{
			LayoutSidebarControls();
		};
		tableLayoutPanel.Controls.Add(optionsScroll, 0, 0);
		UpdateActionLabels();
		StyleSidebarButton(recordButton, Color.FromArgb(221, 72, 83));
		StyleSidebarButton(playButton, Color.FromArgb(66, 179, 118));
		StyleSidebarButton(stopButton, Color.FromArgb(89, 101, 122));
		StyleSidebarButton(clickButton, Color.FromArgb(177, 119, 54));
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
		clickButton.Click += delegate { ToggleClicker(); };
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
		optionsPanel.Controls.Add(clickButton);
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

	private void BuildSettingsTab(Panel tab)
	{
		StyledScrollPanel scroll = new StyledScrollPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
		FlowLayoutPanel panel = new FlowLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(24, 18, 24, 18)
		};
		panel.Width = 550;
		panel.Layout += delegate { scroll.RefreshContentHeight(); };
		scroll.Controls.Add(panel);
		tab.Controls.Add(scroll);
		panel.Controls.Add(new Label { Text = "global hotkeys", Font = new Font("Segoe UI Semibold", 15f), ForeColor = Color.White, Width = 460, Height = 38 });
		panel.Controls.Add(new Label { Text = "Click a shortcut, then press a key combination. Esc cancels.", ForeColor = Color.FromArgb(170, 182, 199), Width = 520, Height = 30 });
		string[] labels = { "record / stop", "play", "stop all", "autoclicker on / off", "save macro", "load macro" };
		for (int i = 0; i < RelaySettings.Actions.Length; i++)
		{
			string action = RelaySettings.Actions[i];
			Panel row = new Panel { Width = 500, Height = 48, Margin = new Padding(0, 0, 0, 3) };
			row.Controls.Add(new Label { Text = labels[i], Location = new Point(0, 10), Width = 240, Height = 28, ForeColor = Color.FromArgb(230, 234, 242) });
			Button keyButton = new Button { Text = settings.Hotkeys[action].ToString(), Location = new Point(255, 3), Width = 225, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 53, 72), ForeColor = Color.White, Cursor = Cursors.Hand };
			keyButton.FlatAppearance.BorderSize = 0;
			keyButton.Click += delegate { if (!capturingHotkey) UnregisterHotkeys(); capturingHotkey = true; keyButton.Text = "press shortcut..."; keyButton.Focus(); };
			keyButton.KeyDown += delegate(object sender, KeyEventArgs e) { CaptureHotkey(action, keyButton, e); };
			keyButton.Leave += delegate { if (capturingHotkey) { capturingHotkey = false; keyButton.Text = settings.Hotkeys[action].ToString(); RegisterHotkeys(); } };
			hotkeyButtons[action] = keyButton;
			row.Controls.Add(keyButton);
			panel.Controls.Add(row);
		}
		panel.Controls.Add(new Label { Text = "autoclicker", Font = new Font("Segoe UI Semibold", 15f), ForeColor = Color.White, Width = 460, Height = 40, Margin = new Padding(0, 20, 0, 0) });
		panel.Controls.Add(new Label { Text = "interval between clicks (milliseconds)", ForeColor = Color.FromArgb(230, 234, 242), Width = 460, Height = 25 });
		clickIntervalBox.Minimum = 10;
		clickIntervalBox.Maximum = 60000;
		clickIntervalBox.Value = settings.ClickIntervalMs;
		clickIntervalBox.Increment = 10;
		clickIntervalBox.Width = 180;
		clickIntervalBox.BackColor = Color.FromArgb(25, 31, 42);
		clickIntervalBox.ForeColor = Color.White;
		clickIntervalBox.ValueChanged += delegate { if (clicker.IsRunning) clicker.Stop(); settings.ClickIntervalMs = (int)clickIntervalBox.Value; if (SaveSettings()) UpdateUi("autoclicker interval set."); };
		panel.Controls.Add(clickIntervalBox);
		panel.Controls.Add(new Label { Text = "mouse button", ForeColor = Color.FromArgb(230, 234, 242), Width = 460, Height = 25, Margin = new Padding(0, 12, 0, 0) });
		FlowLayoutPanel buttons = new FlowLayoutPanel { Width = 340, Height = 36, BackColor = Color.FromArgb(15, 18, 24), Margin = Padding.Empty };
		string[] buttonNames = { "left", "right", "middle" };
		for (int i = 0; i < buttonNames.Length; i++)
		{
			int choice = i + 1;
			RadioButton radio = new RadioButton { Text = buttonNames[i], Width = 105, Height = 30, Checked = settings.ClickButton == choice, ForeColor = Color.White, BackColor = Color.FromArgb(15, 18, 24), Margin = Padding.Empty };
			radio.CheckedChanged += delegate { if (radio.Checked) { if (clicker.IsRunning) clicker.Stop(); settings.ClickButton = choice; if (SaveSettings()) UpdateUi("autoclicker button set."); } };
			buttons.Controls.Add(radio);
		}
		panel.Controls.Add(buttons);
	}

	private void CaptureHotkey(string action, Button button, KeyEventArgs e)
	{
		if (!capturingHotkey) return;
		e.SuppressKeyPress = true;
		if (e.KeyCode == Keys.Escape)
		{
			capturingHotkey = false;
			button.Text = settings.Hotkeys[action].ToString();
			RegisterHotkeys();
			return;
		}
		if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) return;
		Keys modifiers = e.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt);
		HotkeyBinding next = new HotkeyBinding { Key = e.KeyCode, Modifiers = modifiers };
		foreach (var pair in settings.Hotkeys)
		{
			if (pair.Key != action && pair.Value.Key == next.Key && pair.Value.Modifiers == next.Modifiers)
			{
				UpdateUi("that shortcut is already assigned.");
				return;
			}
		}
		int id = Array.IndexOf(RelaySettings.Actions, action) + 1;
		if (!NativeMethods.RegisterHotKey(Handle, id, next.NativeModifiers | 0x4000u, (int)next.Key))
		{
			UpdateUi("shortcut unavailable. choose another.");
			return;
		}
		NativeMethods.UnregisterHotKey(Handle, id);
		settings.Hotkeys[action] = next;
		capturingHotkey = false;
		button.Text = next.ToString();
		RegisterHotkeys();
		UpdateActionLabels();
		if (SaveSettings()) UpdateUi(action + " shortcut set to " + next + ".");
	}

	private void RegisterHotkeys()
	{
		for (int i = 0; i < RelaySettings.Actions.Length; i++)
		{
			HotkeyBinding key = settings.Hotkeys[RelaySettings.Actions[i]];
			NativeMethods.RegisterHotKey(Handle, i + 1, key.NativeModifiers | 0x4000u, (int)key.Key);
		}
	}

	private void UnregisterHotkeys()
	{
		for (int i = 0; i < RelaySettings.Actions.Length; i++) NativeMethods.UnregisterHotKey(Handle, i + 1);
	}

	private bool IsHotkeyKey(int vk)
	{
		foreach (HotkeyBinding key in settings.Hotkeys.Values) if (key.Matches(vk)) return true;
		return false;
	}

	private void UpdateActionLabels()
	{
		recordButton.Text = "record / stop";
		playButton.Text = "play";
		stopButton.Text = "stop";
		clickButton.Text = clicker.IsRunning ? "stop autoclicker" : "start autoclicker";
		saveButton.Text = "save macro";
		loadButton.Text = "load macro";
	}

	private bool SaveSettings()
	{
		try { settings.Save(); return true; }
		catch (Exception ex) { UpdateUi("settings could not be saved: " + ex.Message); return false; }
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
		SendWebState();
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
		clickButton.Width = sidebarContentWidth;
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
		optionsScroll.RefreshContentHeight();
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
			if (clicker.IsRunning) clicker.Stop();
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
			UpdateUi("recording. press " + settings.Hotkeys["record"] + " again to stop.");
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
			if (clicker.IsRunning) clicker.Stop();
			double speed = PlaybackSpeed();
			double mouseScale = MouseScale();
			UpdateUi((continuousPlaybackBox.Checked ? "looping macro. press " : "playing macro. press ") + settings.Hotkeys["stop"] + " to stop.");
			player.Play(document, relativeMouseBox.Checked, restoreCursorBox.Checked, speed, mouseScale, continuousPlaybackBox.Checked);
			SendWebState();
		}
	}

	private void ToggleClicker()
	{
		if (clicker.IsRunning)
		{
			clicker.Stop();
			UpdateUi("autoclicker stopped.");
		}
		else if (!recorder.IsRecording && !player.IsPlaying)
		{
			clicker.Start(settings.ClickIntervalMs, settings.ClickButton);
			UpdateUi("autoclicker running. press " + settings.Hotkeys["click"] + " to stop.");
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
		if (clicker.IsRunning) clicker.Stop();
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
		if (recorder.IsRecording || player.IsPlaying) return;
		if (clicker.IsRunning) clicker.Stop();
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
		if (recorder.IsRecording || player.IsPlaying) return;
		if (clicker.IsRunning) clicker.Stop();
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
		SendWebState();
	}

	private void UpdateUi(string status)
	{
		statusLabel.Text = status;
		int num = ((document.events != null) ? document.events.Count : 0);
		long durationMs = document.GetDurationMs();
		statsLabel.Text = string.Format(CultureInfo.InvariantCulture, "events: {0}\r\nduration: {1:0.00}s\r\nformat: readable json\r\nmouse: {2}", num, (double)durationMs / 1000.0, relativeMouseBox.Checked ? "relative/game" : "absolute/desktop");
		bool flag = recorder.IsRecording || player.IsPlaying;
		UpdateActionLabels();
		playButton.Enabled = !flag && num > 0;
		saveButton.Enabled = !flag && num > 0;
		loadButton.Enabled = !flag;
		recordButton.Enabled = !player.IsPlaying;
		clickButton.Enabled = !flag || clicker.IsRunning;
		editEventsButton.Enabled = !flag && num > 0;
		if (flag)
		{
			SetEventsEditMode(enabled: false);
		}
		SendWebState();
	}
}
