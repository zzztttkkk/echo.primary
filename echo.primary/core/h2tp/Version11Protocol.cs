using System.Net.Sockets;
using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class Version11Options {
	public int MaxFirstLineBytesSize { get; set; } = 4096;
	public int MaxHeaderLineBytesSize { get; set; } = 4096;
	public int MaxHeadersCount { get; set; } = 1024;
	public int MaxBodyBytesSize { get; set; } = 1024 * 1024;
	public int ReadTimeout { get; set; } = 10_000;
	public int HandleTimeout { get; set; } = 0;

	public bool EnableCompression { get; set; } = true;

	public int MinCompressionSize { get; set; } = 1024;
}

public class Version11Protocol(IHandler handler, Version11Options options) : ITcpProtocol {
	private TcpConnection? _connection;

	public Version11Protocol(IHandler handler) : this(handler, new()) {
	}

	public void Dispose() {
		_connection?.Dispose();
	}

	public void ConnectionMade(TcpConnection conn) {
		_connection = conn;
		_ = ReadRequests(conn).ContinueWith(t => {
			if (t.Exception != null && conn.IsAlive) {
				conn.Close(t.Exception);
			}
		});
	}

	private int remainMills(ulong begin) {
		if (options.ReadTimeout < 1) return -1;
		var remain = options.ReadTimeout - (int)(Time.unixmills() - begin);
		if (remain < 0) {
			throw new Exception($"bad request, reach {nameof(options.ReadTimeout)}");
		}

		return remain;
	}

	private async Task ReadRequests(TcpConnection conn) {
		var reader = new ExtAsyncReader(conn);
		var tmp = new MemoryStream(1024);

		var ctx = new RequestCtx();
		var req = ctx.Request;
		var readStatus = MessageReadStatus.None;
		long flBytesSize = 0;
		int headersCount = 0;

		var begin = Time.unixmills();

		while (conn.IsAlive) {
			switch (readStatus) {
				case MessageReadStatus.None: {
					flBytesSize = 0;
					await reader.ReadUntil(
						tmp, (byte)' ',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: remainMills(begin)
					);
					req.flps[0] = Encoding.Latin1.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 1)]);
					flBytesSize += tmp.Position;
					readStatus = MessageReadStatus.FL1_OK;
					break;
				}
				case MessageReadStatus.FL1_OK: {
					await reader.ReadUntil(
						tmp, (byte)' ',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: remainMills(begin)
					);
					req.flps[1] = Encoding.Latin1.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 1)]);
					if (string.IsNullOrEmpty(req.flps[1])) {
						req.flps[1] = "/";
					}

					flBytesSize += tmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxFirstLineBytesSize)}");
					}

					readStatus = MessageReadStatus.FL2_OK;
					break;
				}
				case MessageReadStatus.FL2_OK: {
					await reader.ReadUntil(
						tmp, (byte)'\n',
						maxBytesSize: options.MaxFirstLineBytesSize,
						timeoutMills: remainMills(begin)
					);
					req.flps[2] = Encoding.Latin1.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 2)]);
					flBytesSize += tmp.Position;
					if (options.MaxFirstLineBytesSize > 0 && flBytesSize >= options.MaxFirstLineBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxFirstLineBytesSize)}");
					}

					readStatus = MessageReadStatus.FL3_OK;
					headersCount = 0;
					break;
				}
				case MessageReadStatus.FL3_OK: {
					while (true) {
						var line = (
							await reader.ReadLine(
								tmp,
								maxBytesSize: options.MaxHeaderLineBytesSize,
								timeoutMills: remainMills(begin),
								encoding: Encoding.Latin1
							)
						).Trim();
						if (line.Length < 1) {
							readStatus = MessageReadStatus.HEADER_OK;
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
				case MessageReadStatus.HEADER_OK: {
					var host = req.Headers.GetLast(HttpRfcHeader.Host) ?? "localhost";
					var protocol = conn.IsOverSsl ? "https" : "http";
					req._uri = new Uri($"{protocol}://{host}{req.flps[1]}");

					var cls = req.Headers.GetAll("content-length");
					if (cls == null) {
						readStatus = MessageReadStatus.BODY_OK;
						break;
					}

					if (!long.TryParse(cls.LastOrDefault(""), out var bodySize)) {
						throw new Exception("bad content-length");
					}

					if (bodySize < 1) {
						readStatus = MessageReadStatus.BODY_OK;
						break;
					}

					if (options.MaxBodyBytesSize > 0 && bodySize > options.MaxBodyBytesSize) {
						throw new Exception($"bad request, reach {nameof(options.MaxBodyBytesSize)}");
					}

					if (req.body == null) {
						req.body = new((int)bodySize);
					}
					else {
						req.body.SetLength(0);
						req.body.Capacity = (int)bodySize;
					}

					while (true) {
						var rtmp = tmp.GetBuffer();
						if (bodySize < rtmp.Length) {
							rtmp = rtmp[..(int)bodySize];
						}

						await reader.ReadExactly(rtmp, timeoutMills: remainMills(begin));

						req.body.Write(rtmp);
						bodySize -= rtmp.Length;
						if (bodySize >= 1) continue;

						req.body.Position = 0;
						readStatus = MessageReadStatus.BODY_OK;
						break;
					}

					break;
				}
				case MessageReadStatus.BODY_OK: {
					await HandleRequest(ctx);
					break;
				}
			}
		}
	}

	private async Task HandleRequest(RequestCtx ctx) {
		CancellationTokenSource? cts = null;
		try {
			if (options.HandleTimeout > 0) {
				cts = new CancellationTokenSource();
				ctx._cancellationToken = cts.Token;
				ctx._cancellationToken.Value.Register(() => {
					ctx._handleTimeout = true;
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
		if (exception == null) return;

		var et = exception.GetType();
		if (et == typeof(SocketException) || et == typeof(IOException)) {
			Console.WriteLine($"Connection Lost, {exception.Message}");
			return;
		}

		Console.WriteLine($"Connection Lost, {exception}");
	}
}