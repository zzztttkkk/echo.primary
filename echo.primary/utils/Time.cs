namespace echo.primary.utils;

public static class Time {
	private static readonly DateTime zero = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static ulong unixnanos(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(zero).TotalNanoseconds;

	public static ulong unixmills(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(zero).TotalMilliseconds;

	public static ulong unix(DateTime? v = null) =>
		(ulong)(v?.ToUniversalTime() ?? DateTime.UtcNow).Subtract(zero).TotalSeconds;
}