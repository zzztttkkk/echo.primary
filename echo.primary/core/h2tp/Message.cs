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

internal record FileRef(string filename, Tuple<long, long>? range = null) {
	FileInfo? fileinfo = new(filename);
}

public class Response : Message {
	internal FileRef? _fileRef = null;
	internal Stream? _compressStream = null;

	public int StatusCode {
		get => string.IsNullOrEmpty(flps[1]) ? 0 : int.Parse(flps[1]);
		set => flps[1] = value.ToString();
	}

	public Headers Headers {
		get {
			herders ??= new Headers();
			return herders;
		}
	}

	private void EnsureWriteBuffer(int size) {
		if (body == null) {
			body = new MemoryStream(size);
		}
		else if (body.Capacity < size) {
			body.SetLength(0);
			body.Capacity = size;
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
		EnsureWriteBuffer(0);
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