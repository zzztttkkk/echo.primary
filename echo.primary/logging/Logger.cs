namespace echo.primary.logging;

public class Logger {
	private List<IAppender> _appenders = new();

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

	public void Close() {
		foreach (var appender in _appenders) {
			appender.Close();
		}
	}

	public Logger AddAppender(IAppender appender) {
		appender.Name = $"{Name}.{appender.Name}";
		_appenders.Add(appender);
		return this;
	}

	public Logger DelAppender(string name) {
		_appenders = _appenders.Where(v => v.Name != name).ToList();
		return this;
	}

	public void TRACE(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.TRACE, msg, DateTime.Now, args)
	);

	public void Trace(string msg, List<object>? args = null) => TRACE(msg, args);

	public void DEBUG(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.DEBUG, msg, DateTime.Now, args)
	);

	public void Debug(string msg, List<object>? args = null) => DEBUG(msg, args);

	public void INFO(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.INFO, msg, DateTime.Now, args)
	);

	public void Info(string msg, List<object>? args = null) => INFO(msg, args);

	public void WARN(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.WARN, msg, DateTime.Now, args)
	);

	public void Warn(string msg, List<object>? args = null) => WARN(msg, args);

	public void ERROR(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.ERROR, msg, DateTime.Now, args)
	);

	public void Error(string msg, List<object>? args = null) => ERROR(msg, args);

	public void Error(Exception exception) {
		Console.WriteLine(exception);
	}

	public void FATAL(string msg, List<object>? args = null) => Emit(
		new LogItem(Level.FATAL, msg, DateTime.Now, args)
	);

	public void Fatal(string msg, List<object>? args = null) => FATAL(msg, args);
}