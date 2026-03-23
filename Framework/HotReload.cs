using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using System.Diagnostics;
using System.Reflection;

namespace BuildingsExpanded.Framework;

internal static class HotReload
{
	public static event Action<string>? SourceFileChanged;
	public static event Action<string>? FileUpdated;

	public static string? SourceFolder { get; private set; }
	public static string FolderPath { get; private set; } = "";

	[Conditional("DEBUG")]
	public static void Init(IModHelper help, Mod mod)
	{
		FolderPath = help.DirectoryPath;

		#if DEBUG
		SetupHotReload(help, mod);
		#endif
	}

#if DEBUG

	private static string sourcePath = "";
	private static string destPath = "";
	private static FileSystemWatcher watcher = null!;
	private static readonly string[] allowedTypes = [".json", ".png", ".tmx", ".tbin", ".mgfx", ".wav", ".ogg"];

	private static void SetupHotReload(IModHelper help, Mod mod)
	{
		var path = mod.GetType().Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(a => a.Key == "ProjectPath")?.Value;

		if (path is null)
			return;

		sourcePath = path;
		destPath = help.DirectoryPath;

		if (sourcePath[^1] == Path.DirectorySeparatorChar)
			sourcePath = sourcePath[..^1];

		SourceFolder = sourcePath;

		Print.Log($"Debug mode: mirroring files from {sourcePath} to {destPath} ...");

		watcher = new(Path.Join(sourcePath));
		watcher.Changed += (s, e) => SourceChanged(e.FullPath);
		watcher.Renamed += (s, e) => SourceChanged(e.FullPath);
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
	}

	private static bool FileIsWhitelisted(string extension)
	{
		for (int i = 0; i < allowedTypes.Length; i++)
			if (allowedTypes[i].EqualsIgnoreCase(extension))
				return true;
		return false;
	}

	private static void SourceChanged(string file)
	{
		if (file.EndsWithIgnoreCase("manifest.json"))
			return;

		if (file.ContainsIgnoreCase(".vs"))
			return;

		var ext = Path.GetExtension(file);
		if (ext is null || ext.Length is 0 || ext.EqualsIgnoreCase(".cs") || ext.EqualsIgnoreCase(".csproj") || ext.Contains('~'))
			return;

		var local = file.Replace(sourcePath, "");
		if (local.StartsWith(Path.DirectorySeparatorChar))
			local = local[1..];

		SourceFileChanged?.Invoke(local);

		if (!FileIsWhitelisted(ext))
			return;

		var target = file.Replace(sourcePath, destPath);
		Directory.CreateDirectory(Path.GetDirectoryName(target) ?? "");
		DelayedAction.functionAfterDelay(() =>
		{
			File.Copy(file, target, true);
			FileUpdated?.Invoke(local);
		}, 1);
	}

#endif
}
