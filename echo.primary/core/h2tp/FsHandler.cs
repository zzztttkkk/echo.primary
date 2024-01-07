using System.Globalization;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class FsOptions {
	[Toml(Optional = true)] public string Root { get; set; } = "./static";
	[Toml(Optional = true)] public string Prefix { get; set; } = "/static/";

	// ReSharper disable once MemberCanBePrivate.Global
	[Toml(Optional = true, ParserType = typeof(TomlParsers.DurationParser))]
	public uint CacheMaxAge { get; set; } = 7 * 86400_000;

	public uint CacheMaxAgeSeconds => CacheMaxAge / 1000;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public long MinRangeFileSize { get; set; } = 0;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public long MaxZeroCopyFileSize { get; set; } = 0;

	[Toml(Optional = true)] public bool PreferZeroCopy { get; set; } = false;

	[Toml(Ignored = true)] public FsHandler.PreCheckFunc? PreCheckFunc { get; set; } = null;
	[Toml(Ignored = true)] public FsHandler.IndexRenderFunc? IndexRenderFunc { get; set; } = null;
	[Toml(Ignored = true)] public FsHandler.ETagMakeFunc? ETagMakeFunc { get; set; } = null;

	[Toml(Ignored = true)] public FsHandler.ETagEqualFunc? ETagEqualFunc { get; set; } = null;
}

public class FsHandler(FsOptions opts) : IHandler {
	public delegate Task<bool> PreCheckFunc(RequestCtx ctx, FileSystemInfo info);

	public delegate Task IndexRenderFunc(FsHandler handler, RequestCtx ctx, DirectoryInfo dir);

	public delegate Task<string?> ETagMakeFunc(FileSystemInfo info);

	public delegate Task<bool> ETagEqualFunc(string clipart, string servpart, bool weakcmp);

	private static async Task DefaultIndexRender(FsHandler handler, RequestCtx ctx, DirectoryInfo dir) {
		ctx.Response.Headers.ContentType = "text/html";
		ctx.Response.Headers.Set(RfcHeader.LastModified, dir.LastWriteTime.ToString("R"));

		var fs = await Task.Run(dir.GetFileSystemInfos);

		ctx.Response.Write("<ul>");
		foreach (var info in fs) {
			ctx.Response.Write((info.Attributes & FileAttributes.Directory) != 0
				? $"<li><a href={Quote(handler.MakeUrl(info))}>{info.Name}/</a></li>"
				: $"<li><a href={Quote(handler.MakeUrl(info))}>{info.Name}</a></li>");
		}

		ctx.Response.Write("</ul>");
		return;

		string Quote(string v) {
			return "\"" + v + "\"";
		}
	}

	public static Task NotFoundIndexRender(FsHandler _, RequestCtx ctx, DirectoryInfo __) {
		ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
		return Task.CompletedTask;
	}

	// ReSharper disable once UnusedMember.Local
	private readonly InitFunc _ = new(() => {
		opts.Prefix = opts.Prefix.Trim();
		opts.Root = opts.Root.Trim();

		if (string.IsNullOrEmpty(opts.Prefix) || !opts.Prefix.StartsWith('/') || !opts.Prefix.EndsWith('/')) {
			throw new Exception($"bad prefix for {nameof(FsHandler)}: {opts.Prefix}");
		}
	});

	private readonly DirectoryInfo _root = new(opts.Root);
	private static readonly string[] NoCacheValues = ["no-cache"];

	// ReSharper disable once MemberCanBePrivate.Global
	public string MakeUrl(FileSystemInfo fp) {
		var tmp = $"{opts.Prefix}{fp.FullName[_root.FullName.Length..]}";
		if ((fp.Attributes & FileAttributes.Directory) != 0) {
			tmp += "/";
		}

		return tmp.Replace('\\', '/');
	}

	// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-None-Match
	private async Task<bool> ETagEqual(string clipart, string servpart) {
		if (clipart.Length < servpart.Length) return false;

		var useWeakCmp = false;
		var clipartview = clipart.AsMemory();
		if (clipart.StartsWith("W/")) {
			clipartview = clipartview[3..];
			useWeakCmp = true;
		}

		var tmp = new List<char>(clipartview.Length);
		var begin = false;

		for (var i = 0; i < clipartview.Length; i++) {
			var c = clipartview.Span[i];
			if (!begin) {
				switch (c) {
					case '\t':
					case ' ': {
						continue;
					}
					case '"': {
						begin = true;
						continue;
					}
					default: {
						return false;
					}
				}
			}

			if (c != '"') {
				tmp.Add(c);
				continue;
			}

			var val = string.Join(null, tmp);
			if (val == servpart) return true;

			if (opts.ETagEqualFunc != null && await opts.ETagEqualFunc(val, servpart, useWeakCmp)) {
				return true;
			}

			begin = false;
			tmp.Clear();
		}

		return false;
	}

	private async Task<bool> CacheHit(RequestCtx ctx, FileSystemInfo info, string? etag) {
		if (!string.IsNullOrEmpty(etag)) {
			var cliEtag = ctx.Request.Headers.GetFirst(RfcHeader.IfNoneMatch);
			if (!string.IsNullOrEmpty(cliEtag) && cliEtag != "*" && await ETagEqual(cliEtag, etag)) {
				return true;
			}
		}

		var lwt = info.LastWriteTime;
		var cliTimeString = ctx.Request.Headers.GetLast(RfcHeader.IfModifiedSince);
		if (string.IsNullOrEmpty(cliTimeString)) return false;
		if (
			!DateTime.TryParseExact(
				cliTimeString,
				format: "R",
				provider: null, style: DateTimeStyles.None,
				out var cliTime
			)
		) {
			return false;
		}

		return (long)lwt.Subtract(cliTime).TotalSeconds <= 0;
	}

	private static Tuple<long, long>? ParseFirstRange(string txt, long filesize) {
		var view = txt.AsSpan().Trim();
		if (!view.StartsWith("bytes=")) return null;
		view = view[7..];

		var idx = view.IndexOf(',');
		if (idx > -1) {
			view = view[..idx].Trim();
		}

		idx = view.IndexOf('-');
		if (idx < 0) return null;

		var bv = view[..idx].Trim();
		var ev = view[(idx + 1)..].Trim();
		long begin = 0;
		var end = filesize;

		if (bv.Length > 0) {
			if (!long.TryParse(bv, out begin)) {
				return null;
			}
		}

		if (ev.Length <= 0) return new(begin, end);

		if (!long.TryParse(bv, out end)) {
			return null;
		}

		return new(begin, end);
	}

	public async Task Handle(RequestCtx ctx) {
		var path = ctx.Request.Uri.Path;
		if (string.IsNullOrEmpty(path) || !path.StartsWith(opts.Prefix)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
			return;
		}

		path = path[opts.Prefix.Length..];

		// todo more safe
		path = path.Replace("..", "").Replace("~", "");

		path = $"{_root.FullName}{path}";
		FileSystemInfo filesysinfo =
			path.EndsWith('/') || path.EndsWith('\\') ? new DirectoryInfo(path) : new FileInfo(path);

		if (!await Task.Run(() => filesysinfo.Exists)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
			return;
		}

		if (opts.PreCheckFunc != null && !await opts.PreCheckFunc(ctx, filesysinfo)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.Forbidden;
			return;
		}

		var acceptCache = true;
		var cliCacheControls = ctx.Request.Headers.GetAll(RfcHeader.CacheControl);
		if (cliCacheControls != null) {
			acceptCache = !cliCacheControls.Any(cc => NoCacheValues.Any(cc.Contains));
		}

		var lwt = filesysinfo.LastWriteTime;
		ctx.Response.Headers.Set(RfcHeader.LastModified, lwt.ToString("R"));

		string? etag = null;
		if (opts.ETagMakeFunc != null) {
			etag = await opts.ETagMakeFunc(filesysinfo);
			if (!string.IsNullOrEmpty(etag)) {
				ctx.Response.Headers.Set(RfcHeader.ETag, etag);
			}
		}

		ctx.Response.Headers.Set(RfcHeader.CacheControl, $"max-age={opts.CacheMaxAgeSeconds}");

		if (acceptCache && await CacheHit(ctx, filesysinfo, etag)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.NotModified;
			return;
		}

		if ((filesysinfo.Attributes & FileAttributes.Directory) != 0) {
			ctx.Response.NoCompression = true;
			await (opts.IndexRenderFunc ?? DefaultIndexRender)(this, ctx, (DirectoryInfo)filesysinfo);
			return;
		}

		var fileinfo = (FileInfo)filesysinfo;
		var reqrange = ctx.Request.Headers.GetFirst(RfcHeader.Range);
		Tuple<long, long>? range = null;
		if (!string.IsNullOrEmpty(reqrange)) {
			range = ParseFirstRange(reqrange, fileinfo.Length);
			if (range == null) {
				ctx.Response.StatusCode = (int)RfcStatusCode.BadRequest;
				return;
			}
		}

		if (range == null && opts.MinRangeFileSize > 0 && fileinfo.Length > opts.MinRangeFileSize) {
			range = new(0, 4096);
		}

		var fref = new FileRef(filesysinfo.FullName);
		if (range != null) {
			fref.Range = range;
		}
		else {
			ctx.Response.NoCompression = fileinfo.Length <= 4096;
			if (
				opts.PreferZeroCopy &&
				(
					opts.MaxZeroCopyFileSize < 1
					||
					fileinfo.Length <= opts.MaxZeroCopyFileSize
				)
			) {
				ctx.Response.NoCompression = true;
				fref.ViaSendFile = true;
			}
		}

		ctx.Response.WriteFile(fref);
	}
}