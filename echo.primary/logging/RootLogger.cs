using System.Text;

namespace echo.primary.logging;

internal static class FmtHelper {
	internal static string Dict(IDictionary<string, object> info) {
		var sb = new StringBuilder();
		sb.Append('{');
		foreach (var pair in info) {
			sb.Append(pair.Key);
			sb.Append(": ");
			sb.Append(pair.Value);
			sb.Append(", ");
		}

		sb.Append('}');
		return sb.ToString();
	}

	internal static string List(IEnumerable<object> info) {
		var sb = new StringBuilder();
		sb.Append('[');
		foreach (var item in info) {
			sb.Append(item);
			sb.Append(", ");
		}

		sb.Append(']');
		return sb.ToString();
	}
}

public interface ILogger {
	string Name { get; }
	IRootLogger Root { get; }

	void Trace(string msg);
	void Debug(string msg);
	void Info(string msg);
	void Warn(string msg);
	void Error(string msg);
	void Fatal(string msg);

	void Trace(IDictionary<string, object> info) => Trace(FmtHelper.Dict(info));
	void Trace(IEnumerable<object> info) => Trace(FmtHelper.List(info));
	void Debug(IDictionary<string, object> info) => Debug(FmtHelper.Dict(info));
	void Debug(IEnumerable<object> info) => Debug(FmtHelper.List(info));
	void Info(IDictionary<string, object> info) => Info(FmtHelper.Dict(info));
	void Info(IEnumerable<object> info) => Info(FmtHelper.List(info));
	void Warn(IDictionary<string, object> info) => Warn(FmtHelper.Dict(info));
	void Warn(IEnumerable<object> info) => Warn(FmtHelper.List(info));
	void Error(IDictionary<string, object> info) => Error(FmtHelper.Dict(info));
	void Error(IEnumerable<object> info) => Error(FmtHelper.List(info));
	void Fatal(IDictionary<string, object> info) => Fatal(FmtHelper.Dict(info));
	void Fatal(IEnumerable<object> info) => Fatal(FmtHelper.List(info));

	ILogger GetLogger(string name);
}

public interface IRootLogger : ILogger, IDisposable {
	void AddAppender(IAppender appender);
	void DelAppender(string name);
	void Flush();
	void Close();

	void Trace(string msg, string loggername);
	void Debug(string msg, string loggername);
	void Info(string msg, string loggername);
	void Warn(string msg, string loggername);
	void Error(string msg, string loggername);
	void Fatal(string msg, string loggername);
}

internal class RootLogger(string name = "Logger") : IRootLogger {
	private List<IAppender> _appenders = new();
	public string Name { get; set; } = name;

	public IRootLogger Root => this;

	private void Emit(LogItem log) {
		foreach (var appender in _appenders.Where(e => log.Level >= e.Level)) {
			appender.Append(log);
		}

		if (log.Level < Level.Fatal) return;

		Flush();
		Environment.Exit(-1);
	}

	public void Flush() {
		foreach (var appender in _appenders) {
			appender.Flush();
		}
	}

	public void Close() {
		Flush();

		foreach (var appender in _appenders) {
			appender.Close();
		}
	}

	public void AddAppender(IAppender appender) {
		appender.Name = $"{Name}.{appender.Name}";
		_appenders.Add(appender);
	}

	public void DelAppender(string name) {
		_appenders = _appenders.Where(v => v.Name != name).ToList();
	}

	public void Trace(string msg, string loggername) => Emit(new LogItem(Level.Trace, msg, loggername));
	public void Debug(string msg, string loggername) => Emit(new LogItem(Level.Debug, msg, loggername));
	public void Info(string msg, string loggername) => Emit(new LogItem(Level.Info, msg, loggername));
	public void Warn(string msg, string loggername) => Emit(new LogItem(Level.Warn, msg, loggername));
	public void Error(string msg, string loggername) => Emit(new LogItem(Level.Error, msg, loggername));
	public void Fatal(string msg, string loggername) => Emit(new LogItem(Level.Fatal, msg, loggername));

	public void Trace(string msg) => Trace(msg, Name);
	public void Debug(string msg) => Debug(msg, Name);
	public void Info(string msg) => Info(msg, Name);
	public void Warn(string msg) => Warn(msg, Name);
	public void Error(string msg) => Error(msg, Name);
	public void Fatal(string msg) => Fatal(msg, Name);

	public ILogger GetLogger(string name) => new SubLogger(this, $"{Name}.{name}");

	public void Dispose() {
		Close();
		GC.SuppressFinalize(this);
	}
}

internal class SubLogger(IRootLogger root, string name) : ILogger {
	public string Name { get; } = name;
	public IRootLogger Root => root;
	public void Trace(string msg) => root.Trace(msg, Name);
	public void Debug(string msg) => root.Debug(msg, Name);
	public void Info(string msg) => root.Info(msg, Name);
	public void Warn(string msg) => root.Warn(msg, Name);
	public void Error(string msg) => root.Error(msg, Name);
	public void Fatal(string msg) => root.Fatal(msg, Name);

	public ILogger GetLogger(string name) => new SubLogger(root, $"{Name}.{name}");
}