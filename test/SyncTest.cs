using echo.primary.core.sync;

namespace test;

public class SyncTest {
	[Test]
	public void TestLock() {
		var task = Task.CompletedTask;
		task.Wait();
		task.Wait();
	}
}