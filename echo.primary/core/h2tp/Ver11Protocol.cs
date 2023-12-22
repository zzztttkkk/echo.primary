using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;

namespace echo.primary.core.h2tp;

public class Ver11Config {
	public int MaxFirstLineBytesSize { get; set; } = 4096;
	public int MaxHeaderLineBytesSize { get; set; } = 4096;
	public int MaxHeadersCount { get; set; } = 1024;
	public int MaxBodyBytesSize { get; set; } = 1024 * 1024;
	public int ReadTimeout { get; set; } = 10_000;
}

public class Ver11Protocol : ITcpProtocol {
	public void Dispose() {
	}

	public void ConnectionMade(TcpConnection conn) {
		_ = ReadRequests(conn).ContinueWith(t => {
			if (t.Exception != null && conn.IsAlive) {
				conn.Close(t.Exception);
			}
		});
	}

	private async Task ReadRequests(TcpConnection conn) {
		var reader = new ExtAsyncReader(conn);
		var tmp = new MemoryStream(1024);

		var req = new Request();
		var readStatus = MessageReadStatus.None;

		while (conn.IsAlive) {
			switch (readStatus) {
				case MessageReadStatus.None: {
					await reader.ReadUntil(tmp, (byte)' ');
					req.flps[0] = Encoding.UTF8.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 1)]);
					readStatus = MessageReadStatus.FL1_OK;
					break;
				}
				case MessageReadStatus.FL1_OK: {
					await reader.ReadUntil(tmp, (byte)' ');
					req.flps[1] = Encoding.UTF8.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 1)]);
					readStatus = MessageReadStatus.FL2_OK;
					break;
				}
				case MessageReadStatus.FL2_OK: {
					await reader.ReadUntil(tmp, (byte)'\n');
					req.flps[2] = Encoding.UTF8.GetString(tmp.GetBuffer().AsSpan()[..(int)(tmp.Position - 2)]);
					readStatus = MessageReadStatus.FL3_OK;
					break;
				}
				case MessageReadStatus.FL3_OK: {
					while (true) {
						var line = (await reader.ReadLine(tmp)).Trim();
						if (line.Length < 1) {
							readStatus = MessageReadStatus.HEADER_OK;
							break;
						}

						var idx = line.IndexOf(':');
						req.Headers.Add(line[..idx].Trim(), line[(idx + 1)..].Trim());
					}

					break;
				}
				case MessageReadStatus.HEADER_OK: {
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

					if (req.body == null) {
						req.body = new((int)bodySize);
					}
					else {
						req.body.Capacity = (int)bodySize;
					}

					req.body.Position = 0;

					while (true) {
						var rtmp = tmp.GetBuffer();
						if (bodySize < rtmp.Length) {
							rtmp = rtmp[..(int)bodySize];
						}

						await reader.ReadExactly(rtmp, 0);

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
					Console.WriteLine($"{req.Method} {req.Uri} : {req.body?.Length ?? 0}");
					conn.Close();
					return;
				}
			}
		}
	}

	public void ConnectionLost(Exception? exception) {
		if (exception != null) {
			Console.WriteLine($"Connection Lost, {exception}");
		}
	}
}