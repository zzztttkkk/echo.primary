namespace echo.primary.logging;

public static class Log {
	public static IRootLogger Root { get; set; } = new RootLogger("");

	public static void AddAppender(IAppender appender) => Root.AddAppender(appender);
	public static void DelAppender(string name) => Root.DelAppender(name);

	public static void Flush() => Root.Flush();
	public static void Close() => Root.Close();
}