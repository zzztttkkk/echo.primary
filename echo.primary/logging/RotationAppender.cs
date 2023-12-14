namespace echo.primary.logging;

using Ini = utils.Ini;

public class RotationOptions {
	[Ini(Required = true)] public string FileName { get; set; } = "";
	[Ini(Required = true)] public ulong BySize { get; set; } = 0;
	[Ini(Required = true)] public bool ByDaily { get; set; } = false;
}

public class RotationAppender(RotationOptions opts) : IAppender {
	private RotationOptions _options = opts;
	public Level Level { get; set; } = Level.TRACE;
	public string Name { get; set; } = "";
	public IRenderer Renderer { get; set; } = new SimpleLineRenderer();

	private SemaphoreSlim _streamSlim = new(1, 1);
	private FileStream? _stream = null;

	private SemaphoreSlim _tmpSlim = new(1, 1);
	private LinkedList<LogItem> _tmp = new();

	private async Task AppendAsync(LogItem log) {
		await _tmpSlim.WaitAsync();
		try {
			_tmp.AddLast(log);
		}
		finally {
			_tmpSlim.Release();
		}
	}

	public void Append(LogItem log) {
		_ = AppendAsync(log);
	}

	public void Flush() {
		throw new NotImplementedException();
	}
}