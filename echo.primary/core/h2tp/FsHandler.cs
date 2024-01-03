using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class FsHandlerOptions(string root, string prefix = "/static/") {
	[Toml(Optional = true)] public string Root { get; set; } = root;
	[Toml(Optional = true)] public string Prefix { get; set; } = prefix;

	[Toml(Ignored = true)] public FsHandler.PreCheck? PreCheck { get; set; } = null;
	[Toml(Ignored = true)] public FsHandler.IndexRender? IndexRender { get; set; } = null;
}

public class FsHandler(FsHandlerOptions opts) : IHandler {
	public delegate Task<bool> PreCheck(RequestCtx ctx, FileInfo info);

	public delegate Task IndexRender(FsHandler handler, RequestCtx ctx, DirectoryInfo dir);


	private static async Task DefaultIndexRender(FsHandler handler, RequestCtx ctx, DirectoryInfo dir) {
		ctx.Response.Headers.ContentType = "text/html";

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

	private readonly InitFunc _ = new(() => {
		opts.Prefix = opts.Prefix.Trim();
		opts.Root = opts.Root.Trim();

		if (string.IsNullOrEmpty(opts.Prefix) || !opts.Prefix.StartsWith('/') || !opts.Prefix.EndsWith('/')) {
			throw new Exception($"bad prefix for {nameof(FsHandler)}: {opts.Prefix}");
		}
	});

	private readonly DirectoryInfo _root = new(opts.Root);

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
					await (opts.IndexRender ?? DefaultIndexRender)(this, ctx, fileinfo.Directory);
					return;
				}
			}

			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;
			return;
		}

		ctx.Response.Headers.Set(RfcHeader.LastModified, fileinfo.LastWriteTime.ToString("R"));
		ctx.Response.NoCompression = fileinfo.Length <= 4096;
		ctx.Response.WriteFile(fileinfo.FullName, fileinfo: fileinfo);
	}
}