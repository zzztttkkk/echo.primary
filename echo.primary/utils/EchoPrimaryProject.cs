namespace echo.primary.utils;

public static class EchoPrimaryProject {
	public static string ProjectRoot() {
		var tmp = Environment.CurrentDirectory;
		while (true) {
			if (File.Exists($"{tmp}/echo.primary.sln")) {
				break;
			}

			tmp = new DirectoryInfo(tmp).Parent!.FullName;
		}

		return tmp;
	}
}