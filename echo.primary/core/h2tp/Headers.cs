namespace echo.primary.core.h2tp;

public class Headers {
	private Dictionary<string, List<string>>? data;

	public void Clear() {
		data?.Clear();
	}

	public int KeySize => data?.Count ?? 0;
	public bool Empty => KeySize < 1;

	public List<string>? GetAll(string key) {
		if (data == null) return null;
		data.TryGetValue(key.ToLower(), out var lst);
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

	public void Add(string key, string val) {
		var lst = GetAll(key);
		if (lst != null) {
			lst.Add(val);
			return;
		}

		data ??= new();
		data[key.ToLower()] = [val];
	}

	public void Set(string key, string val) {
		data ??= new();
		data[key.ToLower()] = [val];
	}

	public void Del(string key) {
		data?.Remove(key.ToLower());
	}

	public delegate void Visitor(string key, List<string> lst);

	public void Each(Visitor visitor) {
		if (data == null) return;
		foreach (var pair in data) {
			visitor(pair.Key, pair.Value);
		}
	}
}