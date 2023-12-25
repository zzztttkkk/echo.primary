using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace echo.primary.core.h2tp;

enum MessageReadStatus {
	None = 0,
	FL1_OK,
	FL2_OK,
	FL3_OK,
	HEADER_OK,
	BODY_OK,
}

public class Message {
	internal readonly string[] flps;
	internal Headers? herders;
	internal MemoryStream? body = null;

	protected Message() {
		flps = new string[3];
	}

	internal void Reset() {
		herders?.Clear();
		body?.SetLength(0);
	}
}

public class Request : Message {
	internal Uri? _uri;

	public string Method {
		get => flps[0];
		set => flps[0] = value;
	}

	public Uri Uri => _uri!;

	public string Version {
		get => flps[2];
		set => flps[2] = value;
	}

	public Headers Headers {
		get {
			herders ??= new Headers();
			return herders;
		}
	}

	internal new void Reset() {
		base.Reset();
	}
}

internal class FileRef(string filename, Tuple<long, long>? range = null) {
	public string filename = filename;
	public readonly FileInfo? fileinfo = new(filename);
	public Tuple<long, long>? range = range;
}

public class Response : Message {
	internal FileRef? _fileRef = null;
	internal Stream? _compressStream = null;

	public int StatusCode {
		get => string.IsNullOrEmpty(flps[1]) ? 0 : int.Parse(flps[1]);
		set {
			flps[1] = value.ToString();
			flps[2] = StatusToString.ToString((RfcStatusCode)value);
		}
	}

	public string StatusText {
		get => string.IsNullOrEmpty(flps[2]) ? StatusToString.ToString((RfcStatusCode)StatusCode) : flps[2];
		set => flps[2] = value;
	}

	public Headers Headers {
		get {
			herders ??= new Headers();
			return herders;
		}
	}

	internal void EnsureWriteStream(int size, CompressType? compressType) {
		if (body == null) {
			body = new MemoryStream(size);
		}
		else if (body.Capacity < size) {
			body.SetLength(0);
			body.Capacity = size;
		}

		switch (compressType) {
			case CompressType.Brotil:
				_compressStream = new BrotliStream(body, CompressionLevel.Optimal);
				Headers.Set(RfcHeader.ContentEncoding, "br");
				break;
			case CompressType.Deflate:
				_compressStream = new DeflateStream(body, CompressionLevel.Optimal);
				Headers.Set(RfcHeader.ContentEncoding, "deflate");
				break;
			case CompressType.GZip:
				_compressStream = new GZipStream(body, CompressionLevel.Optimal);
				Headers.Set(RfcHeader.ContentEncoding, "gzip");
				break;
			default:
				return;
		}
	}

	public void Write(byte[] buf) {
		if (_compressStream != null) {
			_compressStream.Write(buf);
		}
		else {
			body!.Write(buf);
		}
	}

	public void Write(string txt) => Write(Encoding.UTF8.GetBytes(txt));

	public void WriteJSON(object val) {
		JsonSerializer.Serialize(_compressStream ?? body!, val);
	}


	public void WriteFile(string path, Tuple<long, long>? range = null) {
		_fileRef = new FileRef(path, range);
	}

	internal new void Reset() {
		base.Reset();
		_fileRef = null;
	}
}