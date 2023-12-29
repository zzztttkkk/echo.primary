using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;

namespace echo.primary.utils;

public interface ITomlDeserializer {
	object Parse(Type targetType, object? src);
}

public interface ITomlDeserializable {
	void FromTomlValueObject(object obj);
}

[AttributeUsage(AttributeTargets.Property)]
public class Toml : Attribute {
	public string Name = "";
	public object? Aliases = null;
	public bool Ingored = false;
	public bool Optional = false;
	public string Description = "";
	public Type? ParserType = null;

	internal ITomlDeserializer Parser {
		get {
			if (!typeof(ITomlDeserializer).IsAssignableFrom(ParserType!)) {
				throw new Exception($"{ParserType!.FullName} is not a {nameof(ITomlDeserializer)}");
			}

			return (ITomlDeserializer)Activator.CreateInstance(ParserType!)!;
		}
	}
}

internal static class TomlValueHelper {
	private static readonly JsonNamingPolicy[] AllNamingPolicy = [
		JsonNamingPolicy.CamelCase,
		JsonNamingPolicy.KebabCaseLower,
		JsonNamingPolicy.KebabCaseUpper,
		JsonNamingPolicy.SnakeCaseLower,
		JsonNamingPolicy.SnakeCaseUpper
	];

	internal static object? Get(TomlTable table, string key, List<string>? aliases = null) {
		var names = new List<string> { key, key.ToUpper(), key.ToLower() };
		names.AddRange(AllNamingPolicy.Select(policy => policy.ConvertName(key)));
		if (aliases != null) {
			foreach (var alias in aliases) {
				names.AddRange(AllNamingPolicy.Select(policy => policy.ConvertName(alias)));
			}
		}

		foreach (var name in names) {
			if (table.TryGetValue(name, out var val)) {
				return val;
			}
		}

		return null;
	}
}

public static class TomlLoader {
	private static bool IsSimpleType(Type t) {
		return t.IsPrimitive || new List<Type> {
			typeof(string), typeof(Color), typeof(TimeSpan), typeof(DateTime)
		}.Contains(t);
	}

	private static object ToSimpleType(object val, Type type) {
		if (Reflection.IsIntType(type)) {
			try {
				return Reflection.ObjectToInt(val, type);
			}
			catch (Exception) {
				switch (val) {
					case string txt: {
						try {
							return Reflection.StringToInt(txt, type, frombase: 10);
						}
						catch (Exception) {
							return Reflection.StringToInt(txt, type, frombase: 16);
						}
					}
					default: {
						throw;
					}
				}
			}
		}

		return val;
	}

	public delegate object ParseFunc(object val);

	private static readonly Dictionary<Type, ParseFunc> ParseFuncs = new();

	public static void Register(Type type, ParseFunc func) {
		ParseFuncs[type] = func;
	}

	private static object ToClsType(object val, Type type) {
		if (type.IsAssignableFrom(typeof(ITomlDeserializable))) {
			var ins = Activator.CreateInstance(type);
			if (ins == null) {
				throw new Exception(
					$"failed create instance for {type.FullName} via {nameof(Activator)}.{nameof(Activator.CreateInstance)}"
				);
			}

			((ITomlDeserializable)ins).FromTomlValueObject(val);
			return ins;
		}

		ParseFuncs.TryGetValue(type, out var parse);
		if (parse != null) return parse(val);

		if (val.GetType() != typeof(TomlTable)) {
			throw new Exception("bad toml value type, expected a toml table");
		}

		var obj = Activator.CreateInstance(type);
		if (obj == null) throw new Exception($"failed create instance: {type.FullName}");
		Bind((TomlTable)val, obj);
		return obj;
	}

	private static Type GetRealType(Type type) {
		var rt = Nullable.GetUnderlyingType(type) ?? type;
		if (Nullable.GetUnderlyingType(rt) != null) {
			throw new Exception("-_-!");
		}

		return rt;
	}

	private static void Bind(TomlTable table, object obj) {
		if (obj.GetType().IsAssignableFrom(typeof(ITomlDeserializable))) {
			var tmp = (ITomlDeserializable)obj;
			tmp.FromTomlValueObject(table);
			return;
		}


		foreach (var property in obj.GetType().GetProperties()) {
			if (!property.CanWrite) continue;

			Console.WriteLine(Nullable.GetUnderlyingType(property.PropertyType));

			var attr = property.GetCustomAttributes<Toml>(true).FirstOrDefault(new Toml());
			if (attr.Ingored) continue;
			if (string.IsNullOrEmpty(attr.Name)) {
				attr.Name = property.Name;
			}

			List<string>? aliases = null;
			if (attr.Aliases != null) {
				switch (attr.Aliases) {
					case string[] sa: {
						aliases = new List<string>();
						aliases.AddRange(sa);
						break;
					}
					default: {
						throw new Exception("aliases expect a string array");
					}
				}
			}

			var pv = TomlValueHelper.Get(table, attr.Name, aliases);

			if (attr.ParserType != null) {
				property.SetValue(obj, attr.Parser.Parse(property.PropertyType, pv));
				continue;
			}

			if (pv == null) {
				if (attr.Optional) {
					continue;
				}

				throw new Exception($"missing required property: {property.Name}");
			}


			var type = GetRealType(property.PropertyType);
			if (IsSimpleType(type)) {
				property.SetValue(obj, ToSimpleType(pv, type));
			}
			else {
				switch (type.IsGenericType) {
					case true when type.GetGenericTypeDefinition() == typeof(List<>): {
						var nullable = Nullable.GetUnderlyingType(type.GetGenericArguments()[0]) != null;
						var eleType = GetRealType(type.GetGenericArguments()[0]);
						var lst = Activator.CreateInstance(type)!;

						switch (pv) {
							case TomlArray ary: {
								foreach (
									var val in ary
										.Where(v => {
											if (v != null) return true;
											if (!nullable) throw new Exception("null");
											type.GetMethod("Add")!.Invoke(lst, [null]);
											return false;
										})
										.Select(
											ele => IsSimpleType(eleType)
												? ToSimpleType(ele!, eleType)
												: ToClsType(ele!, eleType)
										)
								) {
									type.GetMethod("Add")!.Invoke(lst, [val]);
								}

								break;
							}
							case TomlTableArray ary: {
								foreach (
									var val in ary
										.Select(
											ele => IsSimpleType(eleType)
												? ToSimpleType(ele, eleType)
												: ToClsType(ele, eleType)
										)
								) {
									type.GetMethod("Add")!.Invoke(lst, [val]);
								}

								break;
							}
							default: {
								throw new Exception("bad toml value type, require a array or table array");
							}
						}

						property.SetValue(obj, lst);
						continue;
					}
					case true when type.GetGenericTypeDefinition() == typeof(Dictionary<,>): {
						if (pv.GetType() != typeof(TomlTable)) {
							throw new Exception("bad toml value type, require a table");
						}

						var keyType = type.GetGenericArguments()[0];
						var eleType = GetRealType(type.GetGenericArguments()[1]);

						if (IsSimpleType(keyType)) {
							throw new Exception($"bad dict key type, {keyType.FullName}");
						}

						var dict = Activator.CreateInstance(type)!;

						foreach (var pair in (TomlTable)pv) {
							var key = ToSimpleType(pair.Key, keyType);
							var ele = IsSimpleType(eleType)
								? ToSimpleType(pair.Value, eleType)
								: ToClsType(pair.Value, eleType);
							eleType.GetMethod("Add")!.Invoke(dict, [key, ele]);
						}

						property.SetValue(obj, dict);
						continue;
					}
				}

				property.SetValue(obj, ToClsType(pv, type));
			}
		}
	}

	public static TomlTable ParseFile(string filename) {
		var doc = Tomlyn.Toml.Parse(File.ReadAllText(filename));
		if (doc.HasErrors) {
			throw new Exception(doc.Diagnostics[0].ToString());
		}

		return doc.ToModel();
	}

	public enum TomlArrayMergePolicy {
		ReplaceAll,
		Append,
		AppendUnique,
	}

	private static bool tomlValueEqual(object? a, object? b) {
		if (a == null && b == null) return true;
		if (a == null || b == null) return false;
		if (a.GetType() != b.GetType()) return false;

		return a switch {
			TomlTable => false,
			TomlArray => false,
			_ => a.Equals(b)
		};
	}

	private static bool tomlArrayContains(TomlArray ary, object? ele) {
		return ary.Any(v => tomlValueEqual(v, ele));
	}

	public static TomlTable Merge(
		IEnumerable<TomlTable> src,
		TomlArrayMergePolicy arrayMergePolicy = TomlArrayMergePolicy.ReplaceAll
	) {
		var dst = new TomlTable();

		foreach (var pair in src.SelectMany(v => v)) {
			switch (pair.Value) {
				case TomlTable pt: {
					if (!dst.TryGetValue(pair.Key, out var dpv)) {
						dpv = new TomlTable();
					}

					if (dpv.GetType() != typeof(TomlTable)) {
						throw new Exception("bad value type");
					}

					dst[pair.Key] = Merge([(TomlTable)dpv, pt], arrayMergePolicy);
					break;
				}
				case TomlArray pa: {
					switch (arrayMergePolicy) {
						case TomlArrayMergePolicy.ReplaceAll: {
							dst[pair.Key] = pa;
							break;
						}
						case TomlArrayMergePolicy.Append: {
							var dpv = GetArray(pair.Key);
							foreach (var ele in pa) {
								dpv.Add(ele);
							}

							break;
						}
						case TomlArrayMergePolicy.AppendUnique: {
							var dpv = GetArray(pair.Key);
							foreach (var ele in pa.Where(ele => !tomlArrayContains(dpv, ele))) {
								dpv.Add(ele);
							}

							break;
						}
						default: throw new UnreachableException();
					}

					break;
				}
				default: {
					dst[pair.Key] = pair.Value;
					break;
				}
			}
		}

		return dst;

		TomlArray GetArray(string key) {
			if (!dst.TryGetValue(key, out var dpv)) {
				dpv = new TomlArray();
				dst[key] = dpv;
			}

			if (dpv.GetType() != typeof(TomlArray)) {
				throw new Exception("bad value type");
			}

			return (TomlArray)dpv;
		}
	}

	public static T Load<T>(string filename) where T : class, new() {
		var obj = new T();
		Bind(ParseFile(filename), obj);
		return obj;
	}
}