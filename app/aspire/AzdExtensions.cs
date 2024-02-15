using System.Text.Json;
using System.Text.Json.Serialization;

internal static class AzdExtensions
{
    public static IResourceBuilder<T> WithAzdEnvironment<T>(this IResourceBuilder<T> builder, string name) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(
            context =>
            {
                string hostDirectory = builder.ApplicationBuilder.AppHostDirectory;
                string azdDirectory = Path.Combine(hostDirectory, "..", "..", ".azure");

                string azdConfigPath = Path.Combine(azdDirectory, "config.json");

                if (!File.Exists(azdConfigPath))
                {
                    throw new InvalidOperationException(".azure/config.json not found.");
                }

                var azdConfig = JsonSerializer.Deserialize<AzdConfiguration>(File.ReadAllText(azdConfigPath));

                if (azdConfig?.DefaultEnvironment is null)
                {
                    throw new InvalidOperationException("No default environment not found in .azure/config.json.");
                }

                string environmentPath = Path.Combine(azdDirectory, azdConfig.DefaultEnvironment, $".env");

                if (!File.Exists(environmentPath))
                {
                    throw new InvalidOperationException($"Environment file not found: {environmentPath}");
                }

                var env = File.ReadAllText(environmentPath);

                var dictionary =
                    env
                        .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Split("=", 2))
                        .ToDictionary(
                            parts => parts[0],
                            parts => parts[1].Trim('"'),
                            StringComparer.OrdinalIgnoreCase);

                if (!dictionary.TryGetValue(name, out var value))
                {
                    throw new InvalidOperationException($"Environment variable not found: {name}");
                }

                context.EnvironmentVariables[name] = value;
            });
    }

    private sealed record AzdConfiguration
    {
        [JsonPropertyName("defaultEnvironment")]
        public string? DefaultEnvironment { get; init; }

        [JsonPropertyName("version")]
        public int? Version { get; init; }
    }
}