namespace echo.primary.logging;

public class LogItem(Level level, string msg, string loggername) {
	public readonly DateTime Time = DateTime.Now;
	public readonly Level Level = level;
	public readonly string Message = msg;

	public string LoggerName { get; set; } = loggername;
}