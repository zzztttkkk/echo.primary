using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;

namespace echo.primary.utils;

public interface ITomlDeserializer {
	object Parse(Type targetType, object src);
};

[AttributeUsage(AttributeTargets.Property)]
public class Toml : Attribute {
	public string Name = "";
	public List<string>? Aliases = null;
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

public static class TomlLoader {
	public delegate object ParseFunc(object val);

	private static object NotContainer(object val) {
		switch (val) {
			case TomlTable:
			case TomlArray: {
				throw new Exception("");
			}
			default: {
				return val;
			}
		}
	}

	private static readonly Dictionary<Type, ParseFunc> Parses = new() {
		{
			typeof(short), val => Reflection.ObjectToInt(val, typeof(short))
		}, {
			typeof(int), val => Reflection.ObjectToInt(val, typeof(int))
		}, {
			typeof(long), val => Reflection.ObjectToInt(val, typeof(long))
		}, {
			typeof(float), val => Reflection.StringToFloat(val.ToString()!, typeof(float))
		}, {
			typeof(double), val => Reflection.StringToFloat(val.ToString()!, typeof(double))
		}, {
			typeof(string), val => {
				switch (val) {
					case TomlTable:
					case TomlArray: {
						throw new Exception("bad value type");
					}
					default: {
						return val.ToString()!;
					}
				}
			}
		}, {
			typeof(bool), val => bool.Parse(val.ToString()!)
		}
	};

	public static void Register(Type ft, ParseFunc func) => Parses.Add(ft, func);

	private static readonly JsonNamingPolicy[] AllNamingPolicy = [
		JsonNamingPolicy.CamelCase,
		JsonNamingPolicy.KebabCaseLower,
		JsonNamingPolicy.KebabCaseUpper,
		JsonNamingPolicy.SnakeCaseLower,
		JsonNamingPolicy.SnakeCaseUpper
	];

	private static bool IsSimpleType(Type t) {
		return t.IsPrimitive || t == typeof(string);
	}

	private static object ToSimpleType(object? val, Type type) {
		return "";
	}

	private static object ToClsType(object? val, Type type) {
		return "";
	}

	private static void BindForSimpleType(object dst, object val, PropertyInfo property, Type type) {
	}


	private static void Bind(TomlTable table, object obj) {
		foreach (var property in obj.GetType().GetProperties()) {
			if (!property.CanWrite) continue;

			var attr = property.GetCustomAttributes<Toml>(true).FirstOrDefault(new Toml());
			if (attr.Ingored) continue;
			attr.Aliases ??= [];
			foreach (var policy in AllNamingPolicy) {
				attr.Aliases.Add(policy.ConvertName(property.Name));
			}

			if (Nullable.GetUnderlyingType(property.PropertyType) != null) {
				attr.Optional = true;
			}

			var names = new List<string>();
			if (!string.IsNullOrEmpty(attr.Name)) {
				names.Add(attr.Name);
			}

			names.Add(property.Name);
			names.AddRange(attr.Aliases);

			object? pv = null;
			foreach (var name in names.Where(name => table.TryGetValue(name, out pv))) {
				break;
			}

			if (pv == null) {
				if (attr.Optional) {
					continue;
				}

				throw new Exception($"missing required property: {property.Name}");
			}

			if (attr.ParserType != null) {
				property.SetValue(obj, attr.Parser.Parse(property.PropertyType, pv));
				continue;
			}

			var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			if (IsSimpleType(type)) {
				property.SetValue(obj, ToSimpleType(pv, type));
			}
			else {
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
					var eleType = type.GetGenericArguments()[0];
					var lst = Activator.CreateInstance(type);

					switch (pv) {
						case TomlArray ary: {
							foreach (var ele in ary) {
								if (IsSimpleType(eleType)) {
									type.GetMethod("Add")!.Invoke(lst, [ToSimpleType(ele, eleType)]);
								}
								else {
									type.GetMethod("Add")!.Invoke(lst, [ToClsType(ele, eleType)]);
								}
							}

							break;
						}
						case TomlTableArray ary: {
							foreach (var ele in ary) {
								if (IsSimpleType(eleType)) {
									type.GetMethod("Add")!.Invoke(lst, [ToSimpleType(ele, eleType)]);
								}
								
								
								else {
									type.GetMethod("Add")!.Invoke(lst, [ToClsType(ele, eleType)]);
								}
							}

							break;
						}
						default: {
							throw new Exception("");
						}
					}
				}
				else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
					
				}
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