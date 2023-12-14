namespace echo.primary.utils;

public static class Reflection {
	private static readonly List<Type> SIntTypes = [
		typeof(short),
		typeof(int),
		typeof(long),
	];

	private static readonly List<Type> UIntTypes = [
		typeof(ushort),
		typeof(uint),
		typeof(ulong),
	];

	public static bool IsSignedIntType(Type v) {
		return SIntTypes.Exists(e => e == v);
	}

	public static bool IsUnsignedIntType(Type v) {
		return UIntTypes.Exists(e => e == v);
	}

	public static bool IsIntType(Type v) {
		return IsSignedIntType(v) || IsUnsignedIntType(v);
	}

	public static bool IsFloatType(Type v) {
		return v == typeof(float) || v == typeof(double);
	}

	public static bool IsNumberType(Type v) {
		return IsIntType(v) || IsFloatType(v);
	}

	public static object StringToInt(string v, Type t, int frombase = 10) {
		if (t == typeof(short)) return Convert.ToInt16(v, frombase);
		if (t == typeof(ushort)) return Convert.ToUInt16(v, frombase);
		if (t == typeof(int)) return Convert.ToInt32(v, frombase);
		if (t == typeof(uint)) return Convert.ToUInt32(v, frombase);
		if (t == typeof(long)) return Convert.ToUInt64(v, frombase);
		if (t == typeof(ulong)) return Convert.ToUInt64(v, frombase);
		throw new Exception($"unsupported type: {t}");
	}

	public static object ObjectToInt(object v, Type t) {
		if (t == typeof(short)) return Convert.ToInt16(v);
		if (t == typeof(ushort)) return Convert.ToUInt16(v);
		if (t == typeof(int)) return Convert.ToInt32(v);
		if (t == typeof(uint)) return Convert.ToUInt32(v);
		if (t == typeof(long)) return Convert.ToUInt64(v);
		if (t == typeof(ulong)) return Convert.ToUInt64(v);
		throw new Exception($"unsupported type: {t}");
	}

	public static object StringToFloat(string v, Type t) {
		if (t == typeof(double)) return Convert.ToDouble(v);
		if (t == typeof(float)) return Convert.ToSingle(v);
		throw new Exception($"unsupported type: {t}");
	}
}