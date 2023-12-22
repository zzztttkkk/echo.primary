using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace cg {
	[Generator]
	public class HttpHeaderEnum : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) {
		}

		public void Execute(GeneratorExecutionContext context) {
			System.Diagnostics.Process.Start("python ");
		}
	}
}