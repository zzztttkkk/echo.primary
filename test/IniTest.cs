﻿using System.Drawing;
using echo.primary.core.h2tp;
using echo.primary.utils;

namespace test;

class Obj {
	public Color Color { get; set; }

	[Ini(ParserType = typeof(Parsers.ByteSizeParser))] public int SizeA { get; set; }

	public TimeSpan MaxAliveDuration { get; set; }
}

public class IniTest {
	[Test]
	public void TestLoad() {
		var obj = IniLoader.Parse<Obj>($"{EchoPrimaryProject.ProjectRoot()}/test/v.ini");
		Console.WriteLine(JSON.Stringify(obj));
	}

	[Test]
	public void TestBuffer() {
		var headers = new Headers();
		headers.Add(RfcHeader.Accept, "*/*");
		Console.WriteLine($"{headers.GetFirst(RfcHeader.Accept)}");
	}
}