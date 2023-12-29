namespace echo.primary.utils;

public static class ThisProject {
	private static string _RootPath = "";

	private static string GetRootPath() {
		var tmp = Environment.CurrentDirectory;
		while (true) {
			if (File.Exists($"{tmp}/echo.primary.sln")) {
				break;
			}

			tmp = new DirectoryInfo(tmp).Parent!.FullName;
		}

		return tmp;
	}

	public static string RootPath {
		get {
			if (string.IsNullOrEmpty(_RootPath)) {
				_RootPath = GetRootPath();
			}

			return _RootPath;
		}
	}

	public static string TestPath => $"{RootPath}/test";
	public static string ExamplePath => $"{RootPath}/example";
}