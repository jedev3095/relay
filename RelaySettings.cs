using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace relay;

internal sealed class HotkeyBinding
{
	public Keys Key { get; set; }
	public Keys Modifiers { get; set; }

	public override string ToString()
	{
		return (Modifiers == Keys.None ? "" : Modifiers.ToString().Replace(", ", " + ") + " + ") + Key;
	}

	public uint NativeModifiers =>
		((Modifiers & Keys.Alt) != 0 ? 1u : 0u) |
		((Modifiers & Keys.Control) != 0 ? 2u : 0u) |
		((Modifiers & Keys.Shift) != 0 ? 4u : 0u);

	public bool Matches(int vk) => (int)Key == vk && (Control.ModifierKeys & (Keys.Alt | Keys.Control | Keys.Shift)) == Modifiers;
}

internal sealed class RelaySettings
{
	public Dictionary<string, HotkeyBinding> Hotkeys { get; set; } = Defaults();
	public int ClickIntervalMs { get; set; } = 100;
	public int ClickButton { get; set; } = 1;
	public bool CompactMode { get; set; }
	public bool ShowEventsTable { get; set; } = true;

	public static readonly string[] Actions = { "record", "play", "stop", "click", "save", "load" };

	private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Relay", "settings.json");

	private static Dictionary<string, HotkeyBinding> Defaults() => new()
	{
		["record"] = new() { Key = Keys.F8 },
		["play"] = new() { Key = Keys.F9 },
		["stop"] = new() { Key = Keys.F10 },
		["click"] = new() { Key = Keys.F7 },
		["save"] = new() { Key = Keys.S, Modifiers = Keys.Control | Keys.Shift },
		["load"] = new() { Key = Keys.O, Modifiers = Keys.Control | Keys.Shift }
	};

	public static RelaySettings Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				RelaySettings settings = JsonSerializer.Deserialize<RelaySettings>(File.ReadAllText(FilePath));
				if (settings != null)
				{
					settings.Hotkeys ??= new();
					foreach (var item in Defaults())
					{
						settings.Hotkeys.TryAdd(item.Key, item.Value);
					}
					settings.ClickIntervalMs = Math.Clamp(settings.ClickIntervalMs, 10, 60000);
					settings.ClickButton = Math.Clamp(settings.ClickButton, 1, 3);
					return settings;
				}
			}
		}
		catch (Exception)
		{
		}
		return new RelaySettings();
	}

	public void Save()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
		File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
	}
}
