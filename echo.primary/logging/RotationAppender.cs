using System.Text;
using System.Threading.Channels;
using echo.primary.utils;

namespace echo.primary.logging;

public class RotationOptions {
	[Ini(Required = true)] public string FileName { get; set; } = "";
	[Ini(Required = true)] public long BySize { get; set; } = 0;
	[Ini(Required = true)] public bool ByDaily { get; set; } = false;
	[Ini] public int BufferSize { get; set; } = 4096;
}

public class RotationAppender : IAppender {
	private readonly RotationOptions _options;
	public Level Level { get; set; } = Level.TRACE;
	public string Name { get; set; } = "";
	public IRenderer Renderer { get; set; } = new SimpleLineRenderer();

	private FileStream? _stream;
	private long _fileSize;
	private DirectoryInfo? _directoryInfo;
	private readonly string filebasename;
	private readonly string fileextname;

	private bool _stopping;
	private readonly TaskCompletionSource _writeDoneTcs = new();
	private CancellationTokenSource? _readCts;

	private readonly Channel<LogItem> _channel = Channel.CreateUnbounded<LogItem>();

	private readonly StringBuilder tmp;

	public RotationAppender(RotationOptions opts) {
		_options = opts;
		if (_options is { ByDaily: false, BySize: < 1 } or { ByDaily: true, BySize: > 1 }) {
			throw new Exception("ByDaily or BySize all true or all false");
		}

		if (string.IsNullOrEmpty(_options.FileName)) {
			throw new Exception("empty FileName");
		}

		var idx = _options.FileName.LastIndexOf('.');
		if (idx < 0) {
			filebasename = _options.FileName;
			fileextname = "";
		}
		else {
			filebasename = _options.FileName[..idx].Trim();
			fileextname = _options.FileName[(idx + 1)..].Trim();
		}

		if (_options.BufferSize < 4096) _options.BufferSize = 4096;
		tmp = new StringBuilder(_options.BufferSize);
		_ = WriteLoopTask();
	}

	private Task Write(string line) {
		tmp.Append(line);
		return tmp.Length < _options.BufferSize ? Task.CompletedTask : WriteTmpToFile();
	}

	private void RenameBySize() {
		long idx = 0;
		if (_directoryInfo == null) {
			idx = (long)Time.unixmills();
		}
		else {
			foreach (var name in _directoryInfo.GetFiles().Select(v => v.Name).Where(v => v.StartsWith(filebasename))) {
				if (fileextname.Length > 0 && !name.EndsWith(fileextname)) {
					continue;
				}

				try {
					var txt = name[filebasename.Length..^fileextname.Length];
					var num = Convert.ToInt64(txt);
					if (num > idx) idx = num;
				}
				catch {
					// ignored
				}
			}

			idx++;
		}

		var nfn = $"{filebasename}{idx}";
		if (fileextname.Length > 0) {
			nfn += $".{fileextname}";
		}

		DoRename(nfn);
	}

	private void RenameByDay(DateTime time) {
		var nbasefn = $"{filebasename}_{time.Year}_{time.Month}_{time.Day}";
		var nfn = nbasefn;
		if (fileextname.Length > 0) {
			nfn += $"{nfn}.{fileextname}";
		}

		if (File.Exists(nfn)) {
			nfn = $"{nbasefn}.{Time.unixmills}";
			if (fileextname.Length > 0) {
				nfn += $"{nfn}.{fileextname}";
			}
		}

		DoRename(nfn);
	}

	private void DoRename(string name) {
		_stream!.Flush();
		_stream = null;
		_directoryInfo = null;
		_fileSize = 0;
		File.Move(_options.FileName, name);
	}

	private async Task<int> WriteTmpToFileReal() {
		var buf = Encoding.UTF8.GetBytes(tmp.ToString());
		await _stream!.WriteAsync(buf);
		tmp.Clear();
		return buf.Length;
	}

	private async Task WriteTmpToFile() {
		while (true) {
			if (_stream == null) {
				var info = new FileInfo(_options.FileName);
				_directoryInfo = info.Directory;
				if (info.Exists) {
					if (_options.ByDaily) {
						var now = DateTime.Now;
						var begin = Time.unix(new DateTime(now.Year, now.Month, now.Day));
						var end = begin + 86399;
						var fct = Time.unix(info.CreationTime);
						if (fct < begin || fct > end) {
							RenameByDay(info.CreationTime);
							continue;
						}
					}

					_stream = info.OpenWrite();
					_fileSize = info.Length;
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
					RenameBySize();
				}
			}

			break;
		}
	}

	private async Task WriteLoopTask() {
		DateTime? prevTime = null;
		while (!_stopping) {
			_readCts = new CancellationTokenSource();
			try {
				var log = await _channel.Reader.ReadAsync(_readCts.Token);
				if (_options.ByDaily) {
					if (prevTime != null && log.time.Day != prevTime.Value.Day) {
						if (tmp.Length > 0) {
							await WriteTmpToFile();
						}

						RenameByDay(prevTime.Value);
					}

					prevTime = log.time;
				}

				await Write(Renderer.Render(Name, log));
			}
			catch (TaskCanceledException) {
				break;
			}
		}

		while (true) {
			_channel.Reader.TryRead(out var log);
			if (log == null) break;
			await Write(Renderer.Render(Name, log));
		}

		if (tmp.Length > 0) {
			await WriteTmpToFile();
		}

		if (_stream != null) {
			await _stream.FlushAsync();
		}

		_writeDoneTcs.SetResult();
	}

	public void Append(LogItem log) {
		if (_stopping) return;
		_channel.Writer.WriteAsync(log).AsTask();
	}

	public void Flush() {
		_stopping = true;
		_readCts?.Cancel();
		_writeDoneTcs.Task.Wait();
	}
}