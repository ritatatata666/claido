namespace Claido.Services
{
    public static class BuilderExtensions
    {
        public static void LoadApiKey(this WebApplicationBuilder builder)
        {
			if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("API_KEY")))
			{
				return;
			}

			var contentRootPath = builder.Environment.ContentRootPath;

			var envFiles = new[]
			{
				Path.GetFullPath(Path.Combine(contentRootPath, "..", ".env")),
				Path.Combine(contentRootPath, ".env")
			};

			foreach (var filePath in envFiles)
			{
				if (!File.Exists(filePath))
					continue;

				foreach (var rawLine in File.ReadLines(filePath))
				{
					var line = rawLine.Trim();
					if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
						continue;

					if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
						line = line["export ".Length..].Trim();

					var separatorIndex = line.IndexOf('=');
					if (separatorIndex <= 0)
						continue;

					var key = line[..separatorIndex].Trim();
					if (!string.Equals(key, "API_KEY", StringComparison.Ordinal))
						continue;

					var value = line[(separatorIndex + 1)..].Trim();
					if (value.Length >= 2 &&
						((value.StartsWith('"') && value.EndsWith('"')) ||
						 (value.StartsWith('\'') && value.EndsWith('\''))))
					{
						value = value[1..^1];
					}

					if (!string.IsNullOrWhiteSpace(value))
					{
						Environment.SetEnvironmentVariable("API_KEY", value);
						return;
					}
				}
			}
		}
	}
}
