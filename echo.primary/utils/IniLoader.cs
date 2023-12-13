using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace echo.primary.utils;

public delegate object IniCustomParse(object src);

internal delegate string OnProcessEnvMissing(string envkey);

[AttributeUsage(AttributeTargets.Property)]
public class IniPropAttr : Attribute {
	public string Name = "";
	public List<string>? Aliases = null;
	public bool Ingored = false;
	public bool Required = false;
	public string Description = "";
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

	internal IniGroup? GetGroup(string key) {
		Subs.TryGetValue(key, out var result);
		return result;
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
			var tmp = ProcessRefRegexp.Replace(pair.Value, match => {
				var envname = match.Value[5..^1].Trim();
				var envval = Environment.GetEnvironmentVariable(envname);
				return envval ??
				       (opem == null
					       ? throw new Exception(
						       $"{typeof(IniLoader).Namespace}.{nameof(IniLoader)}: missing process env, {envname}"
					       )
					       : opem(envname)
				       );
			});
			Values[pair.Key] = tmp;
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

	// keep space
	private static string Unquote(string v, string fp, int lidx) {
		if (v.StartsWith('"') && v.EndsWith('"')) {
			return v[1..^1];
		}

		if (v.StartsWith('\'') && v.EndsWith('\'')) {
			return v[1..^1];
		}

		if (v.StartsWith('"') || v.StartsWith('\'')) {
			throw new Exception($"bad string line, ${fp}:${lidx}");
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
				current.Values[
					MustKey(line[..idx].Trim(), fp, lineIdx)
				] = Unquote(line[(idx + 1)..].Trim(), fp, lineIdx);
			}
		}

		root.Fill("", new());
		return root;
	}

	private static bool IsSimpleType(Type t) {
		return t.IsPrimitive || t == typeof(string);
	}


	private static void OnSimpleType(IniGroup src, PropertyInfo prop, object dst, IniPropAttr attr) {
		string? val = null;
		foreach (
			var key in new[] { prop.Name, attr.Name }
				.Select(tmp => tmp.Trim().ToUpper())
				.Where(key => key.Length >= 1)
		) {
			val = src.GetValue(key);
			if (val != null) break;
		}

		if (val == null && attr.Aliases is { Count: > 0 }) {
			foreach (
				var key in attr.Aliases
					.Select(tmp => tmp.Trim().ToUpper())
					.Where(v => v.Length >= 1)
			) {
				val = src.GetValue(key);
				if (val != null) break;
			}
		}

		if (val == null) {
			if (attr.Required) throw new Exception($"missing required filed, {prop.Name}");
			return;
		}

		if (prop.PropertyType == typeof(string)) {
			prop.SetValue(dst, val);
			return;
		}

		if (Reflection.IsIntType(prop.PropertyType)) {
			try {
				var frombase = 10;

				if (val.StartsWith("0x") || val.StartsWith("0X")) {
					frombase = 16;
					val = val[2..];
				}
				else if (val.StartsWith("0o") || val.StartsWith("0O")) {
					frombase = 8;
					val = val[2..];
				}
				else if (val.StartsWith("0b") || val.StartsWith("0B")) {
					frombase = 2;
					val = val[2..];
				}

				prop.SetValue(dst, Reflection.StringToInt(val, prop.PropertyType, frombase));
				return;
			}
			catch {
				throw new Exception($"can not convert to a int, {val}");
			}
		}

		if (Reflection.IsFloatType(prop.PropertyType)) {
			try {
				prop.SetValue(dst, Reflection.StringToFloat(val, prop.PropertyType));
				return;
			}
			catch {
				throw new Exception($"can not convert to a int, {val}");
			}
		}
	}

	private static void OnNonSimpleType(IniGroup src, PropertyInfo prop, object dst, IniPropAttr attr) {
		IniGroup? group = null;
		foreach (
			var key in new[] { prop.Name, attr.Name }
				.Select(tmp => tmp.Trim().ToUpper())
				.Where(key => key.Length >= 1)
		) {
			group = src.GetGroup(key);
			if (group != null) break;
		}

		if (group == null && attr.Aliases is { Count: > 0 }) {
			foreach (
				var key in attr.Aliases
					.Select(tmp => tmp.Trim().ToUpper())
					.Where(v => v.Length >= 1)
			) {
				group = src.GetGroup(key);
				if (group != null) break;
			}
		}

		if (group == null) {
			if (attr.Required) throw new Exception($"missing required filed, {prop.Name}");
			return;
		}

		var ins = prop.GetValue(dst);
		if (ins == null) {
			var constructor = prop.PropertyType.GetConstructor([]);
			if (constructor == null) {
				throw new Exception($"type {prop.PropertyType} must has a public default constructor");
			}

			ins = constructor.Invoke(null);
		}

		Bind(group, ins);
		prop.SetValue(dst, ins);
	}

	private static void Bind(IniGroup src, object dst) {
		foreach (var property in dst.GetType().GetProperties()) {
			if (!property.CanWrite) continue;

			var attr = property.GetCustomAttributes<IniPropAttr>().FirstOrDefault(new IniPropAttr());
			if (attr.Ingored) return;

			if (IsSimpleType(property.PropertyType)) {
				OnSimpleType(src, property, dst, attr);
				continue;
			}

			OnNonSimpleType(src, property, dst, attr);
		}
	}

	public static T Parse<T>(string fp) where T : class, new() {
		var ins = new T();
		var root = Parse(fp);
		Bind(root, ins);
		return ins;
	}
}