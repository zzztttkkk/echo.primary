using echo.primary.logging;

var logger = new Logger();

logger.AddAppender(new ConsoleAppender("root", Level.TRACE));

logger.INFO("Hello World", new List<object> { 1, 34, null });

logger.Flush();

Environment.Exit(0);