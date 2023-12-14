using echo.primary.core.sync;

namespace test;

public class SyncTest {
	[Test]
	public async Task TestLock() {
		var rwlock = new RWLock();
		var release = await rwlock.AcquireRead();
		release();
	}
}