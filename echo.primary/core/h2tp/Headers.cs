namespace echo.primary.core.h2tp;

public class Headers {
	private Dictionary<string, List<string>>? data = null;

	public void Clear() {
		data?.Clear();
	}

	public int KeySize => data?.Count ?? 0;

	public bool Empty => KeySize < 1;

	public List<string>? GetAll(string key) {
		if (data == null) return null;
		data.TryGetValue(key, out var lst);
		return lst;
	}

	public string? GetFirst(string key) {
		var lst = GetAll(key);
		if (lst == null || lst.Count < 1) return null;
		return lst.First();
	}

	public string? GetLast(string key) {
		var lst = GetAll(key);
		if (lst == null || lst.Count < 1) return null;
		return lst.Last();
	}
}