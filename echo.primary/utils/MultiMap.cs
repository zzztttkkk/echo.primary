namespace echo.primary.utils;

public class MultiMap(bool ignoreCase = false) {
	private Dictionary<string, List<string>>? _data;

	public void Clear() {
		_data?.Clear();
	}

	public int KeySize => _data?.Count ?? 0;
	public bool Empty => KeySize < 1;

	public List<string>? GetAll(string key, bool isLowercase = false) {
		if (_data == null) return null;
		_data.TryGetValue(isLowercase || !ignoreCase ? key : key.ToLower(), out var lst);
		return lst;
	}

	public string? GetFirst(string key, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst == null || lst.Count < 1) return null;
		return lst.First();
	}

	public string? GetLast(string key, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst == null || lst.Count < 1) return null;
		return lst.Last();
	}

	public void Add(string key, string val, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst != null) {
			lst.Add(val);
			return;
		}

		_data ??= new();
		_data[isLowercase || !ignoreCase ? key : key.ToLower()] = [val];
	}

	public void Set(string key, string val, bool isLowercase = false) {
		_data ??= new();
		_data[isLowercase || !ignoreCase ? key : key.ToLower()] = [val];
	}

	public void Del(string key, bool isLowercase = false) {
		_data?.Remove(isLowercase || !ignoreCase ? key : key.ToLower());
	}

	public delegate void Visitor(string key, List<string> lst);

	public void Each(Visitor visitor) {
		if (_data == null) return;
		foreach (var pair in _data) {
			visitor(pair.Key, pair.Value);
		}
	}
}