namespace relay;

internal static class MacroJson
{
	private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
	};

	public static string Serialize(MacroDocument document)
	{
		using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(document, jsonOptions));
		StringBuilder result = new StringBuilder();
		result.Append('{').AppendLine();
		int propertyIndex = 0;
		int propertyCount = json.RootElement.EnumerateObject().Count();
		foreach (JsonProperty property in json.RootElement.EnumerateObject())
		{
			result.Append("  ").Append(JsonSerializer.Serialize(property.Name)).Append(": ");
			if (property.Name == "events" && property.Value.ValueKind == JsonValueKind.Array)
			{
				result.Append('[').AppendLine();
				int eventIndex = 0;
				int eventCount = property.Value.GetArrayLength();
				foreach (JsonElement item in property.Value.EnumerateArray())
				{
					result.Append("    ").Append(item.GetRawText());
					if (++eventIndex < eventCount) result.Append(',');
					result.AppendLine();
				}
				result.Append("  ]");
			}
			else
			{
				string value = JsonSerializer.Serialize(property.Value, new JsonSerializerOptions { WriteIndented = true });
				result.Append(value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine + "  "));
			}
			if (++propertyIndex < propertyCount) result.Append(',');
			result.AppendLine();
		}
		return result.Append('}').ToString();
	}

	public static MacroDocument Deserialize(string json)
	{
		return JsonSerializer.Deserialize<MacroDocument>(json, jsonOptions);
	}
}
