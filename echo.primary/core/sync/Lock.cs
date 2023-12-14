namespace echo.primary.core.sync;

public class Lock {
	private bool _locked;
	private readonly LinkedList<TaskCompletionSource> _waiters = new();

	public virtual Task Acquire() {
		if (!_locked) {
			_locked = true;
			return Task.CompletedTask;
		}

		var tcs = new TaskCompletionSource();
		_waiters.AddLast(tcs);
		return tcs.Task;
	}

	public virtual void Release() {
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

public class ThreadSafeLock : Lock, IDisposable {
	private readonly SemaphoreSlim _slim = new(1, 1);
	private bool _disposed;

	public override async Task Acquire() {
		await _slim.WaitAsync();
		try {
			await base.Acquire();
		}
		finally {
			_slim.Release();
		}
	}

	public override void Release() {
		_slim.WaitAsync().ContinueWith(_ => {
			try {
				base.Release();
			}
			finally {
				_slim.Release();
			}
		});
	}

	public void Dispose() {
		if (_disposed) return;
		_disposed = true;
		_slim.Dispose();
		GC.SuppressFinalize(this);
	}
}