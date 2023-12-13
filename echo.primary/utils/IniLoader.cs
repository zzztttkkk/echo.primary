using System.Reflection;
using System.Text.RegularExpressions;

namespace echo.primary.utils;

public delegate object IniCustomParse(object src);

internal delegate string OnProcessEnvMissing(string envkey);

[AttributeUsage(AttributeTargets.Property)]
public class IniPropAttr : Attribute {
	public string Name = "";
	public List<string>? Alias = null;
	public bool Ingore = false;
	public IniCustomParse? Parse = null;
}

public class IniGroup {
	public Dictionary<string, string> Values = new();
	public Dictionary<string, IniGroup> Subs = new();

	internal IniGroup EnsureSubGroup(IEnumerable<string> path) {
		var current = this;
		foreach (var name in path) {
			current.Subs.TryGetValue(name, out var tmp);
			if (tmp == null) {
				tmp = new IniGroup();
				current.Subs[name] = tmp;
			}

			current = tmp;
		}

		return current;
	}

	internal IniGroup? GetGroup(List<string> path) {
		var current = this;
		foreach (var gname in path) {
			current.Subs.TryGetValue(gname.Trim().ToUpper(), out var tmp);
			if (tmp == null) return null;
			current = tmp;
		}

		return current;
	}

	internal string? GetValue(string key) {
		Values.TryGetValue(key, out var result);
		return result;
	}

	internal string? GetValue(List<string> path) {
		if (path.Count < 1) return null;
		var name = path.Last().Trim().ToUpper();
		var group = GetGroup(path[..^1]);
		if (group == null) return null;
		group.Values.TryGetValue(name, out var result);
		return result;
	}

	private static readonly Regex ProcessRefRegexp = new(@"\$ENV\{.+?\}");
	private static readonly Regex SelfRefRegexp = new(@"\$\{.+?\}");

	private void FillProcessEnvs(OnProcessEnvMissing? opem = null) {
		foreach (var pair in Values) {
			ProcessRefRegexp.Replace(pair.Value, match => {
				var envname = match.Value[5..^1].Trim();
				var envval = Environment.GetEnvironmentVariable(envname);
				return envval ?? (opem == null ? match.Value : opem(envname));
			});
		}

		foreach (var sg in Subs.Values) {
			sg.FillProcessEnvs(opem);
		}
	}

	internal void Fill(string path, Dictionary<string, string> deps, OnProcessEnvMissing? opem = null) {
		FillProcessEnvs(opem);
	}
}

public static class IniLoader {
	private static readonly Regex NameRegexp = new("^[A-Z_][A-Z0-9_]*$");

	private static string[] MustGroupPath(string[] path, string fp, int lidx) {
		var names = new string[path.Length];

		var idx = 0;
		foreach (var tname in path) {
			var name = tname.ToUpper();
			if (!NameRegexp.IsMatch(name)) {
				throw new Exception($"bad group name line, ${fp}:${lidx}");
			}

			names[idx] = name;
			idx++;
		}

		return names;
	}

	private static string MustKey(string v, string fp, int lidx) {
		v = v.ToUpper();
		if (!NameRegexp.IsMatch(v)) {
			throw new Exception($"bad key name line, ${fp}:${lidx}");
		}

		return v;
	}

	public static IniGroup Parse(string fp) {
		var root = new IniGroup();

		var current = root;
		var lineIdx = 0;
		foreach (var linews in File.ReadLines(fp)) {
			lineIdx++;
			var line = linews.Trim();
			if (line.StartsWith('#') || line.Length < 1) {
				continue;
			}

			if (line.StartsWith('[')) {
				if (!line.EndsWith(']')) {
					throw new Exception($"bad group name line, ${fp}:${lineIdx}");
				}

				current = root.EnsureSubGroup(MustGroupPath(line[1..^1].Split('.'), fp, lineIdx));
				continue;
			}

			var idx = line.IndexOf('=');
			if (idx < 0) {
				current.Values[MustKey(line, fp, lineIdx)] = "";
			}
			else {
				current.Values[MustKey(line[..idx].Trim(), fp, lineIdx)] = line[(idx + 1)..].Trim();
			}
		}

		root.Fill("", new());
		return root;
	}

	private static bool IsSimpleType(Type t) {
		return t.IsPrimitive || t == typeof(string);
	}

	private static void OnSimpleType(IniGroup src, PropertyInfo prop, object dst) {
		var attr = prop.GetCustomAttributes<IniPropAttr>().FirstOrDefault(new IniPropAttr());
		string? val = null;
		foreach (
			var key in new[] { prop.Name, attr.Name }
				.Select(tmp => tmp.Trim().ToUpper())
				.Where(key => key.Length >= 1)
		) {
			val = src.GetValue(key);
			if (val != null) break;
		}

		if (val == null && attr.Alias is { Count: > 0 }) {
			foreach (
				var key in attr.Alias
					.Select(tmp => tmp.Trim().ToUpper())
					.Where(v => v.Length >= 1)
			) {
				val = src.GetValue(key);
				if (val != null) break;
			}
		}

		if (prop.PropertyType == typeof(string)) {
		}
	}

	private static void Bind(IniGroup src, object dst) {
		foreach (var property in dst.GetType().GetProperties()) {
			if (!property.CanWrite) continue;

			if (IsSimpleType(property.PropertyType)) {
				OnSimpleType(src, property, dst);
				continue;
			}

			Console.WriteLine(property.PropertyType);
		}
	}

	public static void Parse(string fp, object ins) {
		var root = Parse(fp);
		Bind(root, ins);
	}
}