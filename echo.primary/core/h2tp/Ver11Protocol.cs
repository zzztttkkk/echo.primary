using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;

namespace echo.primary.core.h2tp;

public class Ver11Protocol : ITcpProtocol {
	public void Dispose() {
	}

	public void ConnectionMade(TcpConnection conn) {
		_ = ReadRequests(conn);
	}

	private async Task ReadRequests(TcpConnection conn) {
		var reader = new ExtAsyncReader(conn);
		var tmp = new MemoryStream(1024);

		var req = new Request();
		var readStatus = MessageReadStatus.None;
		long bodySize = 0;

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
					req.Headers.TryGetValues("content-length", out var cls);
					if (cls == null) {
						readStatus = MessageReadStatus.BODY_OK;
						break;
					}

					if (!long.TryParse(cls.LastOrDefault(""), out bodySize)) {
						throw new Exception("bad content-length");
					}

					if (bodySize < 1) {
						readStatus = MessageReadStatus.BODY_OK;
						break;
					}


					break;
				}
				case MessageReadStatus.BODY_OK: {
					Console.WriteLine(
						$"UserAgent: {req.Headers.UserAgent}; Host: {req.Headers.Host}; BodySize: {bodySize}"
					);
					conn.Close();
					return;
				}
			}
		}
	}

	public void ConnectionLost(Exception? exception) {
	}
}