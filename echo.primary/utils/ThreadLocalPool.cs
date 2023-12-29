namespace echo.primary.utils;

public interface IReusable : IDisposable {
	bool CanReuse { get; }
	void Reset();
}

public class ThreadLocalPool<T>(ThreadLocalPool<T>.Constructor constructor, int maxIdleSize = 24) where T : IReusable {
	public delegate T Constructor();

	public delegate void Prepare(T obj);

	private readonly ThreadLocal<List<T>> _idle = new(() => new List<T>());

	private List<T> List {
		get {
			_idle.Value ??= new();
			return _idle.Value!;
		}
	}

	private T GetObj() {
		if (List.Count < 1) return constructor();
		var ele = List[^1];
		List.RemoveAt(List.Count - 1);
		return ele;
	}

	public T Get(Prepare? prepare = null) {
		var obj = GetObj();
		prepare?.Invoke(obj);
		return obj;
	}

	public void Put(T obj) {
		if (!obj.CanReuse || List.Count >= maxIdleSize) {
			obj.Dispose();
			return;
		}

		obj.Reset();
		List.Add(obj);
	}
}

public class ReusableMemoryStream(int size = 0) : MemoryStream(size), IReusable {
	public bool CanReuse => Capacity <= 32768; // 32KB

	public void Reset() {
		Position = 0;
		SetLength(0);
	}
}