using echo.primary.core.tcp;
using echo.primary.logging;

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
	}
}