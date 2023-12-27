﻿namespace echo.primary.core.sync;

public interface IReuseable : IDisposable {
	bool CanReuse { get; }
	void Reset();
}

public class Pool<T>(Pool<T>.Constructor constructor, int maxIdleSize = 16) where T : IReuseable {
	public delegate T Constructor();

	public delegate T Prepare(T obj);

	private readonly ThreadLocal<LinkedList<T>> _idle = new(() => new LinkedList<T>());

	private LinkedList<T> List {
		get {
			_idle.Value ??= new();
			return _idle.Value!;
		}
	}

	private T GetObj() {
		var first = List.First;
		if (first == null) {
			return constructor();
		}

		List.RemoveFirst();
		return first.Value;
	}

	public T Get(Prepare? prepare = null) {
		var obj = GetObj();
		if (prepare != null) {
			prepare(obj);
		}

		return obj;
	}

	public void Put(T obj) {
		if (!obj.CanReuse || List.Count >= maxIdleSize) {
			obj.Dispose();
			return;
		}

		obj.Reset();
		List.AddLast(obj);
	}
}

public class ReuseableMemoryStream(int size = 0) : MemoryStream(size), IReuseable {
	public bool CanReuse => Capacity <= 1024_00;

	public void Reset() {
		Position = 0;
	}
}