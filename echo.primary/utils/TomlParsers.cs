﻿using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using Tomlyn.Model;

namespace echo.primary.utils;

public static class TomlParsers {
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

	public class ByteSizeParser : ITomlDeserializer {
		public object Parse(Type targetType, object? src) {
			if (!Reflection.IsIntType(targetType)) {
				throw new Exception("the prop is not an int");
			}

			if (src == null) return 0;

			if (Reflection.IsIntType(src.GetType())) {
				return Reflection.ObjectToInt(src, targetType);
			}

			var items = src switch {
				string txt => Items(txt),
				_ => throw new Exception("expected a string or a int number")
			};

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

	public class DurationParser : ITomlDeserializer {
		public object Parse(Type targetType, object? src) {
			if (src == null) {
				throw new Exception("bad toml value type, expected a string or int number");
			}

			if (!Reflection.IsIntType(targetType) && targetType != typeof(TimeSpan)) {
				throw new Exception("the prop is not an int");
			}

			ulong bv = 0;

			if (Reflection.IsIntType(src.GetType())) {
				bv = (ulong)Reflection.ObjectToInt(src, typeof(ulong));
			}
			else {
				if (src.GetType() != typeof(string)) {
					throw new Exception("bad toml value type, expected a string or int number");
				}

				foreach (var item in Items((string)src)) {
					switch (item.Unit.Trim().ToUpper()) {
						case "":
						case "MS":
						case "MILLS": {
							bv += Convert.ToUInt64(item.Nums.Trim());
							break;
						}
						case "S":
						case "SEC": {
							bv += Convert.ToUInt64(item.Nums.Trim()) * 1000;
							break;
						}
						case "M":
						case "MIN": {
							bv += Convert.ToUInt64(item.Nums.Trim()) * 1000 * 60;
							break;
						}
						case "H":
						case "HOUR": {
							bv += Convert.ToUInt64(item.Nums.Trim()) * 1000 * 60 * 60;
							break;
						}
						case "D":
						case "DAY": {
							bv += Convert.ToUInt64(item.Nums.Trim()) * 1000 * 60 * 60 * 24;
							break;
						}
						default: {
							throw new Exception("bad value, can not cast to byte size");
						}
					}
				}
			}

			return targetType == typeof(TimeSpan)
				? TimeSpan.FromMilliseconds(bv)
				: Reflection.ObjectToInt(bv, targetType);
		}
	}

	public class ColorParser : ITomlDeserializer {
		private static int? s2i(object? v) {
			if (v == null) return null;
			if (Reflection.IsIntType(v.GetType())) {
				return (int)Reflection.ObjectToInt(v, typeof(int));
			}

			if (v.GetType() != typeof(string)) {
				throw new Exception();
			}

			try {
				return Convert.ToInt32((string)v);
			}
			catch {
				return Convert.ToInt32((string)v, fromBase: 16);
			}
		}

		public object Parse(Type targetType, object? src) {
			if (targetType != typeof(Color)) {
				throw new Exception("the prop is not a color");
			}

			if (src == null) return Color.Black;

			switch (src) {
				case TomlTable table: {
					var r = s2i(TomlValueHelper.Get(table, "r"));
					var g = s2i(TomlValueHelper.Get(table, "g"));
					var b = s2i(TomlValueHelper.Get(table, "b"));
					var a = s2i(TomlValueHelper.Get(table, "a"));
					return Color.FromArgb(a ?? 255, r ?? 0, g ?? 0, b ?? 0);
				}
				case string txt: {
					txt = txt.Trim();
					if (txt.StartsWith('#')) {
						txt = txt[1..].Trim();
					}

					if (txt.Length == 6) {
						txt = $"ff{txt}";
					}

					return Color.FromName(txt);
				}
				default: {
					throw new UnreachableException();
				}
			}
		}
	}
}