﻿using System.Text;
using echo.primary.logging;

namespace test;

public class LoggingTest {
	[Test]
	public void TestRotationAppender() {
		Console.WriteLine($"{Environment.CurrentDirectory}");

		var logger =
			new Logger().AddAppender(
				new RotationAppender(
					new RotationOptions(FileName: "./logs/v.log", BySize: 8092)
				)
			);

		var i = 0;
		var c = 0;
		while (true) {
			logger.Error($"Num: {i}");
			i++;
			if (i < 10000) continue;
			i = 0;
			logger.Flush();
			c++;
			if (c >= 10) {
				break;
			}
		}

		logger.Close();
	}


	[Test]
	public void TestColorful() {
		var logger =
			new Logger().AddAppender(
				new ColorfulConsoleAppender(
					"ColorfulAppender",
					Level.TRACE,
					new ColorOptions(
						Time: ConsoleColor.Red,
						Message: ConsoleColor.Green
					)
				)
			);

		logger.Error("0.0");

		logger.Close();
	}
}