using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using Tomlyn.Model;

namespace echo.primary.utils;

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

	public class ByteSizeParser : ITomlDeserializer {
		public object Parse(Type targetType, object src) {
			if (!Reflection.IsIntType(targetType)) {
				throw new Exception("the prop is not an int");
			}

			var items = new List<UnitItem>();

			switch (src) {
				case TomlTable table: {
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

	public class TimeDurationParser : ITomlDeserializer {
		public object Parse(Type targetType, object src) {
			if (!Reflection.IsIntType(targetType) && targetType != typeof(TimeSpan)) {
				throw new Exception("the prop is not an int");
			}

			var items = new List<UnitItem>();

			switch (src) {
				case string txt: {
					items = Items(txt);
					break;
				}
			}

			ulong bv = 0;
			foreach (var item in items) {
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

			return Reflection.IsIntType(targetType)
				? Reflection.ObjectToInt(bv, targetType)
				: TimeSpan.FromMilliseconds(bv);
		}
	}

	internal class ColorParser : ITomlDeserializer {
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
			if (targetType != typeof(Color)) {
				throw new Exception("the prop is not a color");
			}

			switch (src) {
				case string txt: {
					txt = txt.Trim();
					if (txt.StartsWith('#')) {
						txt = txt[1..].Trim();
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