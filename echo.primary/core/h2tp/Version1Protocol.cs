using System.Diagnostics;
using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class HttpOptions {
	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxFirstLineBytesSize { get; set; } = 4096;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxHeaderLineBytesSize { get; set; } = 4096;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxHeadersCount { get; set; } = 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxBodyBytesSize { get; set; } = 1024 * 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.DurationParser))]
	public int ReadTimeout { get; set; } = 10_000;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.DurationParser))]
	public int HandleTimeout { get; set; } = 0;

	[Toml(Optional = true)] public bool EnableCompression { get; set; } = false;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MinCompressionSize { get; set; } = 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int StreamReadBufferSize { get; set; } = 4096;
}

public class Version1Protocol(IHandler handler, HttpOptions options) : ITcpProtocol {
	private TcpConnection? _connection;

	public void Dispose() {
		_connection?.Dispose();
		GC.SuppressFinalize(this);
	}

	public void ConnectionMade(TcpConnection conn) {
		_connection = conn;
		_ = ServeConn(conn).ContinueWith(t => conn.Close(t.Exception));
	}

	private int RemainMills(ulong begin) {
		if (options.ReadTimeout < 1) return -1;
		var remain = options.ReadTimeout - (int)(Time.unixmills() - begin);
		if (remain < 0) {
			throw new Exception($"bad request, reach {nameof(options.ReadTimeout)}");
		}

		return remain;
	}

	private async Task ServeConn(TcpConnection conn) {
		var reader = new ExtAsyncReader(
			conn,
			new BytesBuffer(
				conn.MemoryStreamThreadLocalPool.Get(
					v => {
						v.Capacity = 4096;
						conn.OnClose += () => conn.MemoryStreamThreadLocalPool.Put(v);
					}
				)
			)
		);

		var readTmp = conn.MemoryStreamThreadLocalPool.Get(
			v => v.Capacity = options.StreamReadBufferSize
		);

		var ctx = new RequestCtx {
			TcpConnection = conn,
			ReadTmp = readTmp,
			Request = {
				Body = conn.MemoryStreamThreadLocalPool.Get()
			},
			Response = {
				Body = conn.MemoryStreamThreadLocalPool.Get()
			}
		};

		conn.OnClose += () => {
			conn.MemoryStreamThreadLocalPool.Put(readTmp);
			conn.MemoryStreamThreadLocalPool.Put((ReusableMemoryStream)ctx.Request.Body);
			conn.MemoryStreamThreadLocalPool.Put((ReusableMemoryStream)ctx.Response.Body);
		};

		var req = ctx.Request;
		var readStatus = MessageReadStatus.None;
		long flBytesSize = 0;
		var headersCount = 0;

		var begin = Time.unixmills();
		var stop = false;

		while (!stop) {
			switch (readStatus) {
				case MessageReadStatus.None: {
					flBytesSize = 0;
					await reader.ReadUntil(
						readTmp, (byte)' ',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: RemainMills(begin)
					);
					req.Flps[0] =
						Encoding.Latin1.GetString(readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 1)]);
					flBytesSize += readTmp.Position;
					readStatus = MessageReadStatus.Fl1Ok;
					break;
				}
				case MessageReadStatus.Fl1Ok: {
					await reader.ReadUntil(
						readTmp, (byte)' ',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: RemainMills(begin)
					);
					req.Flps[1] =
						Encoding.Latin1.GetString(readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 1)]);
					if (string.IsNullOrEmpty(req.Flps[1])) {
						req.Flps[1] = "/";
					}

					flBytesSize += readTmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxFirstLineBytesSize)}");
					}

					readStatus = MessageReadStatus.Fl2Ok;
					break;
				}
				case MessageReadStatus.Fl2Ok: {
					await reader.ReadUntil(
						readTmp, (byte)'\n',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: RemainMills(begin)
					);
					req.Flps[2] =
						Encoding.Latin1.GetString(readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 2)]);
					flBytesSize += readTmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxFirstLineBytesSize)}");
					}

					readStatus = MessageReadStatus.Fl3Ok;
					headersCount = 0;
					break;
				}
				case MessageReadStatus.Fl3Ok: {
					while (true) {
						var line = (
							await reader.ReadLine(
								readTmp,
								maxBytesSize: options.MaxHeaderLineBytesSize,
								timeoutMills: RemainMills(begin),
								encoding: Encoding.Latin1
							)
						).Trim();
						if (line.Length < 1) {
							readStatus = MessageReadStatus.HeaderOk;
							break;
						}

						var idx = line.IndexOf(':');
						req.Headers.Add(line[..idx].Trim(), line[(idx + 1)..].Trim());

						if (options.MaxHeadersCount < 1) continue;
						if (++headersCount > options.MaxHeadersCount) {
							throw new Exception($"bad request, reach {nameof(options.MaxHeadersCount)}");
						}
					}

					break;
				}
				case MessageReadStatus.HeaderOk: {
					var host = req.Headers.GetLast(RfcHeader.Host) ?? "localhost";
					var protocol = conn.IsOverSsl ? "https" : "http";
					req._uri = new Uri($"{protocol}://{host}{req.Flps[1]}");

					var cls = req.Headers.GetAll("content-length");
					if (cls == null) {
						readStatus = MessageReadStatus.BodyOk;
						break;
					}

					if (!long.TryParse(cls.LastOrDefault(""), out var bodySize)) {
						throw new Exception("bad content-length");
					}

					if (bodySize < 1) {
						readStatus = MessageReadStatus.BodyOk;
						break;
					}

					if (options.MaxBodyBytesSize > 0 && bodySize > options.MaxBodyBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxBodyBytesSize)}");
					}

					req.Body.SetLength(0);
					req.Body.Capacity = (int)bodySize;

					while (true) {
						var rtmp = readTmp.GetBuffer();
						if (bodySize < rtmp.Length) {
							rtmp = rtmp[..(int)bodySize];
						}

						await reader.ReadExactly(rtmp, timeoutMills: RemainMills(begin));

						req.Body.Write(rtmp);
						bodySize -= rtmp.Length;
						if (bodySize >= 1) continue;

						req.Body.Position = 0;
						readStatus = MessageReadStatus.BodyOk;
						break;
					}

					break;
				}
				case MessageReadStatus.BodyOk: {
					await HandleRequest(ctx);
					stop = ctx.Hijacked;
					if (!stop) {
						if (ctx.ShouldKeepAlive) {
							ctx.Reset();
							readStatus = MessageReadStatus.None;
							flBytesSize = 0;
							headersCount = 0;
							begin = Time.unixmills();
							continue;
						}

						await conn.Flush();
						conn.Close();
						stop = true;
					}

					break;
				}
				default: throw new UnreachableException();
			}
		}
	}

	private async Task HandleRequest(RequestCtx ctx) {
		CancellationTokenSource? cts = null;
		try {
			if (options.HandleTimeout > 0) {
				cts = new CancellationTokenSource();
				ctx.CancellationToken = cts.Token;
				ctx.CancellationToken.Value.Register(() => {
					ctx.HandleTimeout = true;
					_connection!.Close(new Exception("handle timeout"));
				});
				cts.CancelAfter(options.HandleTimeout);
			}

			ctx.Response.EnsureWriteStream(
				options.MinCompressionSize,
				options.EnableCompression ? ctx.Request.Headers.AcceptedCompressType : null
			);
			await handler.Handle(ctx);
			await ctx.SendResponse(_connection!);
			// todo keep-alive
			ctx.Reset();
		}
		catch (Exception e) {
			_connection!.Close(e);
		}
		finally {
			cts?.Dispose();
		}
	}

	public void ConnectionLost(Exception? exception) {
		exception = ExceptionHelper.UnwrapFirst(exception);
		if (exception == null) return;

		switch (exception) {
			case IOException: {
				Console.WriteLine($"Connection Lost");
				return;
			}
			default: {
				Console.WriteLine($"Connection Lost, {exception}");
				break;
			}
		}
	}
}