using System.Drawing;
using echo.primary.logging;

namespace test;

public class LoggingTest {
	[Test]
	public void TestRotationAppender() {
		Console.WriteLine($"{Environment.CurrentDirectory}");

		Log.AddAppender(new RotationAppender(
			new RotationOptions(filename: "./logs/v.log", bysize: 8092)
		));

		var logger = Log.Root.GetLogger("A");

		var i = 0;
		var c = 0;
		while (true) {
			logger.Error($"Num: {i}");
			i++;
			if (i < 10000) continue;
			i = 0;
			Log.Flush();
			c++;
			if (c >= 10) {
				break;
			}
		}

		Log.Close();
	}


	[Test]
	public void TestColorful() {
		Log.AddAppender(new ColorfulConsoleAppender(
			"ColorfulConsole",
			Level.Trace,
			new() {
				{
					Level.Error, new ColorSchema(
						name: Color.Indigo,
						time: Color.DarkRed,
						level: Color.Red,
						message: Color.DodgerBlue
					)
				}
			}
		));

		var a = Log.Root.GetLogger("A");
		a.Error("xxx");

		var b = a.GetLogger("B");
		b.Error(new Dictionary<string, object> { { "name", "xxx" } });
	}
}