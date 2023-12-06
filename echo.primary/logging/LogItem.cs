namespace echo.primary.logging;

public record LogItem(
	Level level, string msg, ulong time,
	List<object>? args = null,
	string action = "",
	string path = ""
);