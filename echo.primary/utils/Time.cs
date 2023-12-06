namespace echo.primary.utils;

public static class Time {
	private static readonly DateTime zero = new(1970, 1, 1);

	public static ulong unix => (ulong)DateTime.UtcNow.Subtract(zero).TotalSeconds;
	public static ulong unixmills => (ulong)DateTime.UtcNow.Subtract(zero).TotalMilliseconds;
	public static ulong unixnanos => (ulong)DateTime.UtcNow.Subtract(zero).TotalNanoseconds;
}