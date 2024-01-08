using echo.primary.utils;
using Uri = echo.primary.utils.Uri;


namespace test;

public class AnyTest {
	[Test]
	public void HexToString() {
		var random = new Random();
		var c = 100_000;
		while (c > 0) {
			var num = (uint)random.Next();
			if (Hex.ToString(num) != num.ToString("X")) {
				throw new Exception($"{num}: {Hex.ToBytes(num)} {num:X}");
			}

			c--;
		}

		Console.WriteLine("OK");
	}

	[Test]
	public void Any() {
		Console.WriteLine(
			Uri.Parse("https://john.doe@www.example.com:123/forum/questions/?tag=networking&order=newest#top")
		);
		Console.WriteLine(Uri.Parse("ldap://[2001:db8::7]/c=GB?objectClass?one"));
		Console.WriteLine(Uri.Parse("mailto:John.Doe@example.com"));
		Console.WriteLine(Uri.Parse("news:comp.infosystems.www.servers.unix"));
		Console.WriteLine(Uri.Parse("tel:+1-816-555-1212"));
		Console.WriteLine(Uri.Parse("telnet://192.0.2.16:80/"));
		Console.WriteLine(Uri.Parse("urn:oasis:names:specification:docbook:dtd:xml:4.1.2"));
	}
}