using echo.primary.utils;

namespace echo.primary.logging;

public class Logger {
	private readonly List<IAppender> _appenders = new();

	public string Name { get; set; } = "";

	private void Emit(LogItem log) {
		foreach (var appender in _appenders.Where(e => log.level >= e.Level)) {
			appender.Append(log);
		}

		if (log.level < Level.FATAL) return;

		Flush();
		Environment.Exit(-1);
	}

	public void Flush() {
		foreach (var appender in _appenders) {
			appender.Flush();
		}
	}

	public Logger AddAppender(IAppender appender) {
		_appenders.Add(appender);
		return this;
	}

	public void TRACE(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.TRACE, msg, Time.unixnanos, args)
	);

	public void Trace(string msg, List<object>? args = null) => TRACE(msg, args);

	public void DEBUG(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.DEBUG, msg, Time.unixnanos, args)
	);

	public void Debug(string msg, List<object>? args = null) => DEBUG(msg, args);

	public void INFO(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.INFO, msg, Time.unixnanos, args)
	);

	public void Info(string msg, List<object>? args = null) => INFO(msg, args);

	public void WARN(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.WARN, msg, Time.unixnanos, args)
	);

	public void Warn(string msg, List<object>? args = null) => WARN(msg, args);

	public void ERROR(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.ERROR, msg, Time.unixnanos, args)
	);

	public void Error(string msg, List<object>? args = null) => ERROR(msg, args);

	public void FATAL(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.FATAL, msg, Time.unixnanos, args)
	);

	public void Fatal(string msg, List<object>? args = null) => FATAL(msg, args);
}