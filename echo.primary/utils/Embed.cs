using System.Resources;
using Microsoft.Extensions.FileSystemGlobbing;

namespace echo.primary.utils;

public class Embed(string name, IEnumerable<string> patterns, IReadOnlyCollection<string>? exclusions = null) {
	public void Make() {
		var matcher = new Matcher();
		matcher.AddExclude("**/*.resource");
		matcher.AddIncludePatterns(patterns);
		if (exclusions != null) matcher.AddExcludePatterns(exclusions);

		using var rw = new ResourceWriter(name.EndsWith(".resource") ? name : $"{name}.resource");
		foreach (var fp in matcher.GetResultsInFullPath("./")) {
			rw.AddResource(fp, File.ReadAllBytes(fp));
		}
	}
}