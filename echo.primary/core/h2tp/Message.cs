using System.Diagnostics;
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
	internal HttpHeaders? herders;
	internal MemoryStream? body;

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

	public HttpHeaders Headers {
		get {
			herders ??= new HttpHeaders();
			return herders;
		}
	}

	internal new void Reset() {
		base.Reset();
	}
}

internal class FileRef(string filename, Tuple<long, long>? range = null, bool viaSendFile = false) {
	public string filename = filename;
	public readonly FileInfo fileinfo = new(filename);
	public Tuple<long, long>? range = range;
	public bool viaSendFile = viaSendFile;
}

public enum BodyType {
	None,
	PlainText,
	Binary,
	JSON,
	Serializer,
	File,
	Stream,
}

public interface ISerializer {
	string? ContentType { get; }
	void Serialize(Stream stream, object val);
}

public class Response : Message {
	internal FileRef? _fileRef;
	internal Stream? _compressStream;
	private CompressType? _compressType;
	private int _miniCompressionSize;
	private BodyType? _bodyType;
	private Stream? _stream;

	public int StatusCode {
		get => string.IsNullOrEmpty(flps[1]) ? 0 : int.Parse(flps[1]);
		set {
			flps[1] = value.ToString();
			flps[2] = StatusToString.ToString((HttpRfcStatusCode)value);
		}
	}

	public string StatusText {
		get => string.IsNullOrEmpty(flps[2]) ? StatusToString.ToString((HttpRfcStatusCode)StatusCode) : flps[2];
		set => flps[2] = value;
	}

	public HttpHeaders Headers {
		get {
			herders ??= new HttpHeaders();
			return herders;
		}
	}

	internal void EnsureWriteStream(int miniCompressionSize, CompressType? compressType) {
		if (body == null) {
			body = new MemoryStream();
		}
		else {
			body.Position = 0;
		}

		_miniCompressionSize = miniCompressionSize;
		_compressType = compressType;

		if (_miniCompressionSize < 1) AutoCompressWriter();
	}

	private void ResetBodyTmp() {
		_bodyType = null;

		_compressStream?.Dispose();
		_compressStream = null;

		_stream?.Close();
		_stream?.Dispose();
		_stream = null;

		_fileRef = null;

		if (body != null) {
			body.Position = 0;
		}

		Headers.Del(HttpRfcHeader.ContentType);
		AutoCompressWriter();
	}

	public BodyType BodyType {
		get => _bodyType ?? BodyType.None;
		set {
			ResetBodyTmp();
			_bodyType = value;
		}
	}

	private void AutoCompressWriter() {
		if (_compressType == null) return;

		switch (_compressType) {
			case CompressType.Brotil:
				_compressStream = new BrotliStream(body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(HttpRfcHeader.ContentEncoding, "br");
				break;
			case CompressType.Deflate:
				_compressStream = new DeflateStream(body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(HttpRfcHeader.ContentEncoding, "deflate");
				break;
			case CompressType.GZip:
				_compressStream = new GZipStream(body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(HttpRfcHeader.ContentEncoding, "gzip");
				break;
			default:
				throw new UnreachableException();
		}
	}

	private void WriteBytes(ReadOnlySpan<byte> buf) {
		if (_compressStream != null) {
			_compressStream.Write(buf);
		}
		else {
			body ??= new(buf.Length);
			body.Write(buf);

			if (_compressType == null || body.Position < _miniCompressionSize) return;

			var data = new byte[body.Position];
			Array.Copy(body.ToArray(), data, body.Position);
			body.Position = 0;

			AutoCompressWriter();
			_compressStream!.Write(data);
		}
	}

	public void Write(byte[] buf) {
		if (_bodyType is not BodyType.Binary) {
			_bodyType = BodyType.Binary;
		}

		WriteBytes(buf.AsSpan());
	}

	public void Write(string txt, Encoding? encoding = null) {
		if (_bodyType is not BodyType.PlainText) {
			_bodyType = BodyType.PlainText;
		}

		WriteBytes((encoding ?? Encoding.UTF8).GetBytes(txt).AsSpan());
	}

	public void Write(StringBuilder sb) => Write(sb.ToString());

	public void WriteJSON(object val, JsonSerializerOptions? options = null) {
		if (_bodyType is not BodyType.JSON) {
			_bodyType = BodyType.JSON;
		}

		AutoCompressWriter();
		JsonSerializer.Serialize(_compressStream ?? body!, val, options);
	}

	public void WriteObject(object val, ISerializer serializer) {
		if (_bodyType is not BodyType.Serializer) {
			_bodyType = BodyType.Serializer;
		}

		var contentType = serializer.ContentType;
		if (!string.IsNullOrEmpty(contentType)) {
			Headers.ContentType = contentType;
		}

		AutoCompressWriter();
		serializer.Serialize(_compressStream ?? body!, val);
	}

	public void WriteFile(string path, Tuple<long, long>? range = null, bool viaSendFile = false) {
		_bodyType = BodyType.File;
		_fileRef = new FileRef(path, range, viaSendFile);
	}

	public void WriteStream(Stream stream) {
		_bodyType = BodyType.Stream;
		_stream = stream;
	}

	internal new void Reset() {
		base.Reset();
		ResetBodyTmp();
	}
}