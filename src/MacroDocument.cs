namespace relay;

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
