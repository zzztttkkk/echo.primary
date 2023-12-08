namespace echo.primary.core.sync;

internal record Waiter(
	bool IsReading,
	TaskCompletionSource Tcs
);

public class RWLock {
	private readonly Lock _lock = new();
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


	public async Task AcquireRead() {
		await _lock.Acquire();
		try {
			if (_writing) {
				Waiter waiter = new(true, new());
				_waiters.AddLast(waiter);
				await waiter.Tcs.Task;
				return;
			}

			_readdings++;
		}
		finally {
			_lock.Release();
		}
	}

	public async Task ReleaseRead() {
		await _lock.Acquire();
		try {
			_readdings--;
			WakeNext();
		}
		finally {
			_lock.Release();
		}
	}

	public async Task AcquireWrite() {
		await _lock.Acquire();
		try {
			if (_writing || _readdings > 0) {
				Waiter waiter = new(false, new());
				_waiters.AddLast(waiter);
				await waiter.Tcs.Task;
				return;
			}

			_writing = true;
		}
		finally {
			_lock.Release();
		}
	}

	public async Task ReleaseWrite() {
		await _lock.Acquire();
		try {
			_writing = false;
			WakeNext();
		}
		finally {
			_lock.Release();
		}
	}
}