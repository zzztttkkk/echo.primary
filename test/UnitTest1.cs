using echo.primary.core.tcp;

namespace test;

public class Tests {
	[SetUp]
	public void Setup() {
	}

	private static async Task sleep(int idx) {
		await Task.Delay(3000);
		Console.WriteLine($"{idx}: {Environment.CurrentManagedThreadId}");
	}

	[Test]
	public void Test1() {
		var logger = NLog.LogManager.GetLogger("root");

		Server server = new(logger);

		var stop = false;

		Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs args) {
			args.Cancel = true;
			stop = true;
		};

		server.Start("127.0.0.1", 8080);

		while (!stop) {
			Thread.Yield();
		}
	}
}