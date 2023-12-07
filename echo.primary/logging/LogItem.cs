namespace echo.primary.logging;

public record LogItem(
	Level level,
	string msg,
	DateTime time,
	List<object>? args = null,
	string action = "",
	string path = ""
);