using echo.primary.logging;

namespace test;

public class LoggingTest {
	[Test]
	public void TestRotationAppender() {
		Console.WriteLine($"{Environment.CurrentDirectory}");
		RotationOptions rotationOptions = new();
		rotationOptions.FileName = "./logs/v.log";
		rotationOptions.ByDaily = true;
		var logger = new Logger().AddAppender(new RotationAppender(rotationOptions));
		logger.Error("xxxxx11");
		logger.Flush();
	}
}