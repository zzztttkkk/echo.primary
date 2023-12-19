using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace echo.primary.utils;

public interface IIniParser {
	object Parse(Type targetType, object src);
};

internal delegate string OnProcessEnvMissing(string envkey);

[AttributeUsage(AttributeTargets.Property)]
public class Ini : Attribute {
	public string Name = "";
	public List<string>? Aliases = null;
	public bool Ingored = false;
	public bool Optional = false;
	public string Description = "";
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] public Type? ParserType = null;

	internal IIniParser Parser {
		get {
			if (!typeof(IIniParser).IsAssignableFrom(ParserType!)) {
				throw new Exception($"{ParserType!.FullName} is not a {nameof(IIniParser)}");
			}

			var constructor = ParserType.GetConstructor([]);
			if (constructor == null) {
				throw new Exception($"{ParserType!.FullName} ha s no public default constructor");
			}

			return (IIniParser)constructor.Invoke(null);
		}
	}
}

public class IniGroup {
	public Dictionary<string, string> Values = new();
	public Dictionary<string, IniGroup> Subs = new();

	internal IniGroup EnsureSubGroup(IEnumerable<string> path) {
		var current = this;
		foreach (var name in path) {
			current.Values.TryGetValue(name, out var vtmp);
			if (vtmp != null) {
				throw new Exception($"`{name}` in values, can not in subs");
			}

			current.Subs.TryGetValue(name, out var tmp);
			if (tmp == null) {
				tmp = new IniGroup();
				current.Subs[name] = tmp;
			}

			current = tmp;
		}

		return current;
	}

	internal void SetValue(string k, string v) {
		Subs.TryGetValue(k, out var tmp);
		if (tmp != null) {
			throw new Exception($"`{k}` in subs, can not in values");
		}

		Values[k] = v;
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

internal class ColorParser : IIniParser {
	private static int? s2i(string? v) {
		if (string.IsNullOrEmpty(v)) return null;

		try {
			return Convert.ToInt32(v);
		}
		catch {
			try {
				return Convert.ToInt32(v, 16);
			}
			catch {
				return null;
			}
		}
	}

	public object Parse(Type targetType, object src) {
		switch (src) {
			case string txt: {
				break;
			}
			case IniGroup group: {
				var name = group.GetValue("NAME");
				if (name != null) {
					return Color.FromName(name);
				}

				var r = s2i(group.GetValue("R"));
				var g = s2i(group.GetValue("G"));
				var b = s2i(group.GetValue("B"));
				var a = s2i(group.GetValue("A"));
				return Color.FromArgb(a ?? 255, r ?? 0, g ?? 0, b ?? 0);
			}
		}

		return Color.FromArgb(255, 0, 0, 0);
	}
}

public static class IniLoader {
	private static readonly Regex NameRegexp = new("^[A-Z_][A-Z0-9_]*$");

	private static Dictionary<Type, IIniParser> InternalParsers = new() {
		{ typeof(Color), new ColorParser() }
	};

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

				var name = line[1..^1].Trim();
				if (name == "" || name.Equals("$root", StringComparison.CurrentCultureIgnoreCase)) {
					current = root;
				}
				else {
					current = root.EnsureSubGroup(MustGroupPath(name.Split('.'), fp, lineIdx));
				}

				continue;
			}

			var idx = line.IndexOf('=');
			if (idx < 0) {
				current.SetValue(MustKey(line, fp, lineIdx), "");
			}
			else {
				current.SetValue(
					MustKey(line[..idx].Trim(), fp, lineIdx),
					Unquote(line[(idx + 1)..].Trim(), fp, lineIdx)
				);
			}
		}

		root.Fill("", new());
		return root;
	}

	private static bool IsSimpleType(Type t) {
		return t.IsPrimitive || t == typeof(string) || t.IsEnum;
	}

	private static void OnSimpleType(IniGroup src, PropertyInfo prop, object dst, Ini attr) {
		var val = GetString(src, prop, attr);
		if (val == null) {
			if (!attr.Optional) throw new Exception($"missing required filed, {prop.Name}");
			return;
		}

		if (prop.PropertyType.IsEnum) {
			try {
				prop.SetValue(dst, Enum.Parse(prop.PropertyType, val, true));
				return;
			}
			catch {
				throw new Exception($"can not convert to enum `{prop.PropertyType.Name}`, {val}");
			}
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

		if (prop.PropertyType == typeof(bool)) {
			prop.SetValue(dst, bool.Parse(val));
			return;
		}

		throw new Exception();
	}

	private static void OnNonSimpleType(IniGroup src, PropertyInfo prop, object dst, Ini attr) {
		var group = GetGroup(src, prop, attr);
		if (group == null) {
			if (!attr.Optional) throw new Exception($"missing required filed, {prop.Name}");
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

	private static IniGroup? GetGroup(IniGroup src, PropertyInfo prop, Ini attr) {
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

		return group;
	}

	private static string? GetString(IniGroup src, PropertyInfo prop, Ini attr) {
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

		return val;
	}

	private static readonly JsonNamingPolicy[] AllNameingPolicy = {
		JsonNamingPolicy.CamelCase,
		JsonNamingPolicy.KebabCaseLower,
		JsonNamingPolicy.KebabCaseUpper,
		JsonNamingPolicy.SnakeCaseLower,
		JsonNamingPolicy.SnakeCaseUpper
	};

	public static void Bind(IniGroup src, object dst) {
		foreach (var property in dst.GetType().GetProperties()) {
			if (!property.CanWrite) continue;

			var attr = property.GetCustomAttributes<Ini>().FirstOrDefault(new Ini());
			if (attr.Ingored) return;
			attr.Aliases ??= [];

			foreach (var policy in AllNameingPolicy) {
				attr.Aliases.Add(policy.ConvertName(property.Name));
			}

			if (attr.ParserType != null) {
				var val = GetGroup(src, property, attr) ?? (object?)GetString(src, property, attr);
				if (val == null) {
					if (!attr.Optional) throw new Exception($"missing required filed, {property.Name}");
					continue;
				}

				property.SetValue(dst, attr.Parser.Parse(property.PropertyType, val));
				continue;
			}

			InternalParsers.TryGetValue(property.PropertyType, out var parser);
			if (parser != null) {
				var val = GetGroup(src, property, attr) ?? (object?)GetString(src, property, attr);
				if (val == null) {
					if (!attr.Optional) throw new Exception($"missing required filed, {property.Name}");
					continue;
				}

				property.SetValue(dst, parser.Parse(property.PropertyType, val));
				continue;
			}

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

public static class Parsers {
	private static readonly Regex UnitGroupRegexp = new(@"(?<nums>\d+)\s*(?<unit>[a-zA-Z]*)");

	private record UnitItem(string Nums, string Unit) {
		public string Nums { get; set; } = Nums;
		public string Unit { get; set; } = Unit;
	};

	private static List<UnitItem> Items(string txt) {
		var lst = new List<UnitItem>();
		var matchs = UnitGroupRegexp.Matches(txt);
		for (var i = 0; i < matchs.Count; i++) {
			var match = matchs[i];
			lst.Add(new UnitItem(match.Groups["nums"].Value, match.Groups["unit"].Value));
		}

		return lst;
	}

	public class ByteSizeParser : IIniParser {
		public object Parse(Type targetType, object src) {
			if (!Reflection.IsIntType(targetType)) {
				throw new Exception("the prop is not an int");
			}

			var items = new List<UnitItem>();

			switch (src) {
				case IniGroup group: {
					var tu = group.GetValue("UNIT");
					var tv = group.GetValue("VALUE");
					var item = new UnitItem("", "");
					if (!string.IsNullOrEmpty(tu)) {
						item.Unit = tu;
					}

					if (!string.IsNullOrEmpty(tv)) {
						item.Nums = tv;
					}

					items.Add(item);
					break;
				}
				case string txt: {
					items = Items(txt);
					break;
				}
			}


			ulong bv = 0;
			foreach (var item in items) {
				switch (item.Unit.Trim().ToUpper()) {
					case "K":
					case "KB": {
						bv += Convert.ToUInt64(item.Nums.Trim()) * 1024;
						break;
					}
					case "M":
					case "MB": {
						bv += Convert.ToUInt64(item.Nums.Trim()) * 1024 * 1024;
						break;
					}
					case "G":
					case "GB": {
						bv += Convert.ToUInt64(item.Nums.Trim()) * 1024 * 1024 * 1024;
						break;
					}
					case "BYTE":
					case "B":
					case "": {
						bv += Convert.ToUInt64(item.Nums.Trim());
						break;
					}
					default: {
						throw new Exception("bad value, can not cast to byte size");
					}
				}
			}

			return Reflection.ObjectToInt(bv, targetType);
		}
	}

	public class TimeDurationParser : IIniParser {
		public object Parse(Type targetType, object src) {
			throw new NotImplementedException();
		}
	}

	public class BinaryFileParser : IIniParser {
		public object Parse(Type targetType, object src) {
			throw new NotImplementedException();
		}
	}

	public class TextFileParser : IIniParser {
		public object Parse(Type targetType, object src) {
			throw new NotImplementedException();
		}
	}
}