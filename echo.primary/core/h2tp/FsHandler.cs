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

	[Toml(Ignored = true)] public FsHandler.PreCheckFunc? PreCheckFunc { get; set; } = null;
	[Toml(Ignored = true)] public FsHandler.IndexRenderFunc? IndexRenderFunc { get; set; } = null;
	[Toml(Ignored = true)] public FsHandler.ETagMakeFunc? ETagMakeFunc { get; set; } = null;
}

public class FsHandler(FsOptions opts) : IHandler {
	public delegate Task<bool> PreCheckFunc(RequestCtx ctx, FileSystemInfo info);

	public delegate Task IndexRenderFunc(FsHandler handler, RequestCtx ctx, DirectoryInfo dir);

	public delegate Task<string> ETagMakeFunc(FileInfo info);

	private static async Task DefaultIndexRender(FsHandler handler, RequestCtx ctx, DirectoryInfo dir) {
		ctx.Response.Headers.ContentType = "text/html";
		ctx.Response.Headers.Set(RfcHeader.LastModified, dir.LastWriteTime.ToString("R"));

		var fs = await Task.Run(dir.GetFileSystemInfos);

		ctx.Response.Write("<ul>");
		foreach (var info in fs) {
			ctx.Response.Write((info.Attributes & FileAttributes.Directory) != 0
				? $"<li class={Quote("dir")}><a href={Quote(handler.MakeUrl(info))}>{info.Name}/</a></li>"
				: $"<li class={Quote("file")}><a href={Quote(handler.MakeUrl(info))}>{info.Name}</a></li>");
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
	private static readonly string[] NoCacheValues = ["no-cache", "max-age=0", "must-revalidate"];

	// ReSharper disable once MemberCanBePrivate.Global
	public string MakeUrl(FileSystemInfo fp) {
		var tmp = $"{opts.Prefix}{fp.FullName[_root.FullName.Length..]}";
		if ((fp.Attributes & FileAttributes.Directory) != 0) {
			tmp += "/";
		}

		return tmp;
	}

	public async Task Handle(RequestCtx ctx) {
		var path = ctx.Request.Path.ToString();
		if (!path.StartsWith(opts.Prefix)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
			return;
		}

		path = ctx.Request.Path.ToString()[opts.Prefix.Length..];

		// todo more safe
		path = path.Replace("..", "")
			.Replace("~", "");

		var fileinfo = new FileInfo($"{_root.FullName}{path}");

		if (!await Task.Run(() => fileinfo.Exists)) {
			if (fileinfo.Directory != null) {
				if (await Task.Run(() => fileinfo.Directory.Exists)) {
					if (opts.PreCheckFunc != null && !await opts.PreCheckFunc(ctx, fileinfo)) {
						ctx.Response.StatusCode = (int)RfcStatusCode.Forbidden;
						return;
					}

					// todo cache dir
					await (opts.IndexRenderFunc ?? DefaultIndexRender)(this, ctx, fileinfo.Directory);
					return;
				}
			}

			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
			return;
		}

		if (opts.PreCheckFunc != null && !await opts.PreCheckFunc(ctx, fileinfo)) {
			ctx.Response.StatusCode = (int)RfcStatusCode.Forbidden;
			return;
		}

		var lwt = fileinfo.LastWriteTime;
		ctx.Response.Headers.Set(RfcHeader.LastModified, lwt.ToString("R"));
		ctx.Response.Headers.Set(RfcHeader.CacheControl, $"max-age={opts.CacheMaxAgeSeconds}");

		var noCache = false;
		var ccs = ctx.Request.Headers.GetAll(RfcHeader.CacheControl);
		if (ccs != null) {
			noCache = ccs.Any(cc => NoCacheValues.Any(cc.Contains));
		}

		if (!noCache) {
			var ifModifiedSince = ctx.Request.Headers.GetLast(RfcHeader.IfModifiedSince);
			if (!string.IsNullOrEmpty(ifModifiedSince)) {
				if (
					DateTime.TryParseExact(
						ifModifiedSince,
						format: "R",
						provider: null, style: DateTimeStyles.None,
						out var cliTime
					)
				) {
					if ((long)lwt.Subtract(cliTime).TotalSeconds <= 0) {
						ctx.Response.StatusCode = (int)RfcStatusCode.NotModified;
						return;
					}
				}
			}
		}

		ctx.Response.NoCompression = fileinfo.Length <= 4096;
		ctx.Response.WriteFile(fileinfo.FullName, fileinfo: fileinfo, viaSendFile: true);
	}
}