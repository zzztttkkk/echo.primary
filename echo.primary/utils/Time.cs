namespace echo.primary.utils;

public static class Time {
	private static readonly DateTime Zero = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static ulong Unixnanos(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(Zero).TotalNanoseconds;

	public static ulong Unixmills(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(Zero).TotalMilliseconds;

	public static ulong Unix(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(Zero).TotalSeconds;

	public static DateTime TruncateSecond(DateTime v) => new(v.Year, v.Month, v.Day, v.Hour, v.Minute, v.Second);
}