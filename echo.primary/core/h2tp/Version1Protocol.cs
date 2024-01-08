using System.Diagnostics;
using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;
using Uri = echo.primary.utils.Uri;

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

	[Toml(Optional = true, Aliases = new[] { "compression" })]
	public bool EnableCompression { get; set; } = false;

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
		var remain = options.ReadTimeout - (int)(Time.Unixmills() - begin);
		if (remain < 0) {
			throw new Exception($"bad request, reach {nameof(options.ReadTimeout)}");
		}

		return remain;
	}

	private async Task ServeConn(TcpConnection conn) {
		var reader = new ExtAsyncReader(
			conn,
			new BytesBuffer(
				conn.MemoryStreamMemoryStreamPool.Get(
					v => {
						v.Capacity = 4096;
						conn.OnClose += _ => conn.MemoryStreamMemoryStreamPool.Put(v);
					}
				)
			)
		);

		var readTmp = conn.MemoryStreamMemoryStreamPool.Get(
			v => v.Capacity = options.StreamReadBufferSize
		);

		var ctx = new RequestCtx {
			TcpConnection = conn,
			ReadTmp = readTmp,
			Request = {
				Body = conn.MemoryStreamMemoryStreamPool.Get()
			},
			Response = {
				Body = conn.MemoryStreamMemoryStreamPool.Get()
			}
		};

		conn.OnClose += _ => {
			conn.MemoryStreamMemoryStreamPool.Put(readTmp);
			conn.MemoryStreamMemoryStreamPool.Put((ReusableMemoryStream)ctx.Request.Body);
			conn.MemoryStreamMemoryStreamPool.Put((ReusableMemoryStream)ctx.Response.Body);
		};

		var req = ctx.Request;
		var readStatus = MessageReadStatus.None;
		long flBytesSize = 0;
		var headersCount = 0;

		var begin = Time.Unixmills();
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

					if (readTmp.Position <= 1) {
						CloseWithException("bad message, empty request method");
						break;
					}

					req.Flps[0] = Encoding.Latin1.GetString(
						readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 1)]
					).ToUpper();

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

					if (readTmp.Position <= 1) {
						CloseWithException("bad message, empty request path");
						break;
					}

					flBytesSize += readTmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						CloseWithException(
							$"bad request, reach {nameof(options.MaxFirstLineBytesSize)}"
						);
						break;
					}

					var isConnect = ctx.Request.IsConnect;

					var (uri, exc) = Uri.Parse(
						Encoding.Latin1.GetString(readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 1)]),
						allowAuthority: isConnect
					);
					if (exc != null) {
						CloseWithException(exc);
						break;
					}

					if (isConnect) {
						if (string.IsNullOrEmpty(uri!.Path)) {
							CloseWithException("empty path");
							break;
						}
					}
					else {
						if (string.IsNullOrEmpty(uri!.Host)) {
							CloseWithException("empty host");
							break;
						}
					}

					ctx.Request.InnerUri = uri;

					readStatus = MessageReadStatus.Fl2Ok;
					break;
				}
				case MessageReadStatus.Fl2Ok: {
					await reader.ReadUntil(
						readTmp, (byte)'\n',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: RemainMills(begin)
					);

					if (readTmp.Position <= 2) {
						CloseWithException("bad message, empty request proto version");
						break;
					}

					req.Flps[2] = Encoding.Latin1.GetString(
						readTmp.GetBuffer().AsSpan()[..(int)(readTmp.Position - 2)]
					).ToUpper();

					flBytesSize += readTmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						CloseWithException(
							$"bad request, reach {nameof(options.MaxFirstLineBytesSize)}"
						);
						break;
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
						if (idx < 0) {
							CloseWithException("bad message, unexpected header line");
							break;
						}

						req.Headers.Add(line[..idx].Trim(), line[(idx + 1)..].Trim());

						if (options.MaxHeadersCount < 1) continue;
						if (++headersCount <= options.MaxHeadersCount) continue;

						CloseWithException($"bad request, reach {nameof(options.MaxHeadersCount)}");
						break;
					}

					break;
				}
				case MessageReadStatus.HeaderOk: {
					var cls = req.Headers.GetAll("content-length");
					if (cls == null) {
						readStatus = MessageReadStatus.BodyOk;
						break;
					}

					if (!long.TryParse(cls.LastOrDefault(""), out var bodySize)) {
						CloseWithException("bad message, unexpected content-length");
						break;
					}

					if (bodySize < 1) {
						readStatus = MessageReadStatus.BodyOk;
						break;
					}

					if (options.MaxBodyBytesSize > 0 && bodySize > options.MaxBodyBytesSize) {
						CloseWithException($"bad request, reach {nameof(options.MaxBodyBytesSize)}");
						break;
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

					if (ctx.UpgradeFunc != null) {
						stop = true;
						ctx.UpgradeFunc(conn, reader, readTmp);
						break;
					}

					if (!ctx.KeepAlive) {
						conn.Close();
						stop = true;
						break;
					}

					ctx.Reset();
					readStatus = MessageReadStatus.None;
					flBytesSize = 0;
					headersCount = 0;
					begin = Time.Unixmills();
					continue;
				}
				default: throw new UnreachableException();
			}
		}

		return;

		void CloseWithException(string msg) {
			conn.Close(new Exception(msg));
			stop = true;
		}
	}

	private async Task HandleRequest(RequestCtx ctx) {
		CancellationTokenSource? cts = null;
		var sent = false;
		try {
			if (options.HandleTimeout > 0) {
				cts = new CancellationTokenSource();
				ctx.CancellationToken = cts.Token;
				ctx.CancellationToken.Value.Register(() => { _connection!.Close(new Exception("handle timeout")); });
				cts.CancelAfter(options.HandleTimeout);
			}

			ctx.Response.CompressType = options.EnableCompression ? ctx.Request.Headers.AcceptedCompressType : null;
			await handler.Handle(ctx);
			sent = true;
			await ctx.SendResponse(_connection!);
		}
		catch (Exception e) {
			var exception = ExceptionHelper.UnwrapFirst(e);
			if (!sent && exception is not SystemException) {
				ctx.Response.Reset();
				ctx.Response.StatusCode = (int)RfcStatusCode.InternalServerError;
				try {
					await ctx.SendResponse(_connection!);
				}
				catch {
					// ignored
				}
			}

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
			case SystemException: {
				Console.WriteLine("Connection Closed");
				return;
			}
			default: {
				Console.WriteLine($"Connection Lost, {exception}");
				break;
			}
		}
	}
}