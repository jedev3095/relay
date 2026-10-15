namespace relay;

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
