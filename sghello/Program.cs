namespace sghello;

partial class Program {
	static void Main(string[] args) {
		HelloFrom("SG");
	}

	static partial void HelloFrom(string name);
}