
namespace Tabliq.Tests.RemoteExecuter;

public static class Resources
{
    public static string GetFile(string path)
    {
        path = path.Replace("\\", "/").Replace("/", ".");

        using var stream = typeof(Resources).Assembly.GetManifestResourceStream($"Tabliq.Tests.RemoteExecuter.{path}");
        if (stream == null)
        {
            throw new FileNotFoundException($"Resource '{path}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static class Schemas
    {
        public static string AnonSchema => GetFile("Schemas.AnonSchema.json");
        public static string Default => GetFile("Schemas.Default.json");
        public static string FriendlyNames => GetFile("Schemas.FriendlyNames.json");
    }
}
