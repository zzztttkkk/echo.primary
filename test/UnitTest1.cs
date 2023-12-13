using System.Text.Json;
using echo.primary.core.net;
using echo.primary.utils;

namespace test;

class User {
	[IniPropAttr(Ingore = true)] public string Name { get; set; }
	public int Age { get; set; }
}

public class Tests {
	[SetUp]
	public void Setup() {
	}


	[Test]
	public void Test1() {
		IniLoader.Parse("../../../../example/v.ini", new User());
	}
}