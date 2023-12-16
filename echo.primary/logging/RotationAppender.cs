using System.Text;
using System.Threading.Channels;
using echo.primary.utils;

namespace echo.primary.logging;

public class RotationOptions(string FileName = "", long BySize = 0, bool ByDate = false, int BufferSize = 4096) {
	[Ini] public string FileName { get; set; } = FileName;
	[Ini] public long BySize { get; set; } = BySize;
	[Ini] public bool ByDate { get; set; } = ByDate;
	[Ini] public int BufferSize { get; set; } = BufferSize;
}

public class RotationAppender : IAppender {
	private readonly RotationOptions _options;
	public Level Level { get; set; } = Level.TRACE;
	public string Name { get; set; } = "";
	public IRenderer Renderer { get; set; } = new SimpleLineRenderer();

	private FileStream? _stream;
	private long _fileSize;
	private readonly DirectoryInfo _directoryInfo;
	private readonly string _fileBaseName;
	private readonly string _fileExtName;

	private TaskCompletionSource? _flushDoneTcs;
	private bool _stopping;

	private readonly TaskCompletionSource _writeDoneTcs = new();
	private CancellationTokenSource? _readCts;

	private readonly Channel<LogItem> _channel = Channel.CreateUnbounded<LogItem>();

	private readonly StringBuilder _tmp;
	private Exception? _writeException;

	public RotationAppender(RotationOptions opts) {
		_options = opts;
		if (_options is { ByDate: false, BySize: < 1 } or { ByDate: true, BySize: > 1 }) {
			throw new Exception("ByDate or BySize all true or all false");
		}

		if (string.IsNullOrEmpty(_options.FileName)) throw new Exception("empty FileName");

		_fileBaseName = Path.GetFileNameWithoutExtension(_options.FileName);
		_fileExtName = Path.GetExtension(_options.FileName);
		var directoryInfo = new FileInfo(_options.FileName).Directory;
		if (directoryInfo == null) throw new Exception("empty directory info");
		directoryInfo.Create();
		_directoryInfo = directoryInfo;

		if (_options.BufferSize < 4096) _options.BufferSize = 4096;
		_tmp = new StringBuilder(_options.BufferSize);
		_ = WriteLoopTask().ContinueWith(t => {
			if (t.Exception != null) _writeException = t.Exception;
			_stopping = true;
			_writeDoneTcs.TrySetResult();
		});
	}

	private Task Write(LogItem log) {
		Renderer.Render(_tmp, Name, log);
		return _tmp.Length < _options.BufferSize ? Task.CompletedTask : WriteTmpToFile();
	}

	private async Task RenameBySize() {
		var nfn = $"{_fileBaseName}_{Time.unixnanos()}{_fileExtName}";
		await DoFlushReal();
		DoRename(nfn);
	}

	private async Task RenameByDate(DateTime time) {
		var nbasefn = $"{_fileBaseName}_{time.Year}_{time.Month}_{time.Day}";
		var nfn = $"{nbasefn}{_fileExtName}";

		if (File.Exists($"{_directoryInfo.FullName}/{nfn}")) {
			nfn = $"{nbasefn}.{Time.unixnanos()}{_fileExtName}";
		}

		await DoFlushReal();
		DoRename(nfn);
	}

	private void DoRename(string name) {
		_stream?.Dispose();
		_stream = null;
		_fileSize = 0;

		File.Move(_options.FileName, $"{_directoryInfo.FullName}/{name}");
	}

	private async Task<int> WriteTmpToFileReal() {
		var buf = Encoding.UTF8.GetBytes(_tmp.ToString());
		await _stream!.WriteAsync(buf);
		_tmp.Clear();
		return buf.Length;
	}

	private async Task WriteTmpToFile() {
		while (true) {
			if (_stream == null) {
				var info = new FileInfo(_options.FileName);
				if (info.Exists) {
					if (_options.ByDate) {
						var now = DateTime.Now;
						var begin = Time.unix(new DateTime(now.Year, now.Month, now.Day));
						var end = begin + 86399;
						var fct = Time.unix(info.CreationTime);
						if (fct < begin || fct > end) {
							await RenameByDate(info.CreationTime);
							continue;
						}
					}

					_stream = info.OpenWrite();
					_fileSize = info.Length;
					_stream.Seek(0, SeekOrigin.End);
				}
				else {
					_stream = info.Create();
					_fileSize = 0;
				}
			}

			var size = await WriteTmpToFileReal();
			if (_options.BySize > 0) {
				_fileSize += size;
				if (_fileSize > _options.BySize) {
					await RenameBySize();
				}
			}

			break;
		}
	}

	private async Task DoFlushReal() {
		if (_tmp.Length > 0) {
			await WriteTmpToFile();
		}

		if (_stream != null) {
			await _stream.FlushAsync();
		}
	}

	private async Task DoFlushByUser() {
		if (_flushDoneTcs == null) return;
		await DoFlushReal();
		_flushDoneTcs.SetResult();
		_flushDoneTcs = null;
	}

	private async Task WriteLoopTask() {
		DateTime? prevTime = null;
		while (!_stopping) {
			await DoFlushByUser();

			_readCts = new CancellationTokenSource();
			try {
				var log = await _channel.Reader.ReadAsync(_readCts.Token);
				if (_options.ByDate) {
					if (prevTime != null && log.time.Day != prevTime.Value.Day) {
						await RenameByDate(prevTime.Value);
					}

					prevTime = log.time;
				}

				await Write(log);
			}
			catch (TaskCanceledException) {
				break;
			}
		}

		while (true) {
			_channel.Reader.TryRead(out var log);
			if (log == null) break;
			await Write(log);
		}

		await DoFlushReal();
		_stream?.Close();
		_writeDoneTcs.SetResult();
	}

	public void Append(LogItem log) {
		if (_stopping) throw new Exception("the logger is stopped");
		if (_writeException != null) throw _writeException;
		_channel.Writer.WriteAsync(log).AsTask();
	}

	public void Flush() {
		if (_stopping) return;
		_flushDoneTcs ??= new();
		_flushDoneTcs.Task.Wait();
		_flushDoneTcs = null;
	}

	public void Close() {
		if (_stopping) return;
		_stopping = true;
		_readCts?.Cancel();
		_writeDoneTcs.Task.Wait();
	}
}