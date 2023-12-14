namespace echo.primary.core.sync;

internal record Waiter(
	bool IsReading,
	TaskCompletionSource Tcs
);

public class RWLock {
	protected Lock _lock = new();
	private bool _writing;
	private int _readdings;
	private readonly LinkedList<Waiter> _waiters = new();

	private void WakeNext() {
		var head = _waiters.First;
		if (head == null) return;

		if (!head.Value.IsReading && _readdings == 0) {
			_writing = true;
			head.Value.Tcs.SetResult();
			_waiters.RemoveFirst();
			return;
		}

		while (true) {
			head = _waiters.First;
			if (head == null) return;

			if (head.Value.IsReading) {
				_readdings++;
				_waiters.RemoveFirst();
				head.Value.Tcs.SetResult();
				continue;
			}

			break;
		}
	}

	public async Task<Action> AcquireRead() {
		await _lock.Acquire();
		try {
			if (_writing) {
				Waiter waiter = new(true, new());
				_waiters.AddLast(waiter);
				await waiter.Tcs.Task;
				return ReleaseRead;
			}

			_readdings++;
			return ReleaseRead;
		}
		finally {
			_lock.Release();
		}
	}

	private void ReleaseRead() {
		_lock.Acquire().ContinueWith(_ => {
			try {
				_readdings--;
				WakeNext();
			}
			finally {
				_lock.Release();
			}
		});
	}

	public async Task<Action> AcquireWrite() {
		await _lock.Acquire();
		try {
			if (_writing || _readdings > 0) {
				Waiter waiter = new(false, new());
				_waiters.AddLast(waiter);
				await waiter.Tcs.Task;
				return ReleaseWrite;
			}

			_writing = true;
			return ReleaseWrite;
		}
		finally {
			_lock.Release();
		}
	}

	private void ReleaseWrite() {
		_lock.Acquire().ContinueWith(_ => {
			try {
				_writing = false;
				WakeNext();
			}
			finally {
				_lock.Release();
			}
		});
	}
}

public class ThreadSafeRWLock : RWLock, IDisposable {
	public ThreadSafeRWLock() {
		_lock = new ThreadSafeLock();
	}

	public void Dispose() {
		var tmp = (ThreadSafeLock)_lock;
		tmp.Dispose();
	}
}