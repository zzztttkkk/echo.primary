using echo.primary.utils;

namespace test;

class User {
	public string Name { get; set; } = "";
	public uint Age { get; set; } = 0;

	[Ini(Name = "Hp_Factor")] public float Factor { get; set; } = 0;

	[Ini(Name = "pc_name", Required = true)]
	public string PcName { get; set; }

	public Address Address { get; set; }

	public override string ToString() {
		return System.Text.Json.JsonSerializer.Serialize(this);
	}
}

class Address {
	public string City { get; set; } = "";
	public string Street { get; set; } = "";
	public string MailCode { get; set; } = "";
}

public class Tests {
	[SetUp]
	public void Setup() {
	}


	[Test]
	public void Test1() {
		var user = IniLoader.Parse<User>("../../../../example/v.ini");
		Console.WriteLine(user);
	}
}