namespace echo.primary.core.sync;

public class Lock {
	private bool _locked;
	private readonly LinkedList<TaskCompletionSource> _waiters = new();

	public Task Acquire() {
		if (!_locked) {
			_locked = true;
			return Task.CompletedTask;
		}

		var tcs = new TaskCompletionSource();
		_waiters.AddLast(tcs);
		return tcs.Task;
	}

	public void Release() {
		var first = _waiters.First;
		if (first == null) {
			_locked = false;
			return;
		}

		var tcs = first.Value;
		_waiters.RemoveFirst();
		tcs.SetResult();
	}
}