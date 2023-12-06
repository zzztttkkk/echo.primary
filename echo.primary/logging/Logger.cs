using echo.primary.utils;

namespace echo.primary.logging;

public class Logger {
	private List<IAppender> _appenders = new();

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

	public void AddAppender(IAppender appender) => _appenders.Add(appender);

	public void TRACE(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.TRACE, msg, Time.unixnanos, args)
	);

	public void DEBUG(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.DEBUG, msg, Time.unixnanos, args)
	);

	public void INFO(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.INFO, msg, Time.unixnanos, args)
	);

	public void WARN(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.WARN, msg, Time.unixnanos, args)
	);

	public void ERROR(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.ERROR, msg, Time.unixnanos, args)
	);

	public void FATAL(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.FATAL, msg, Time.unixnanos, args)
	);
}