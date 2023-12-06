﻿using Microsoft.CodeAnalysis;

namespace vldsg {
	[Generator]
	public class MyClass : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) {
		}

		public void Execute(GeneratorExecutionContext context) {
			var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

			// Build up the source code
			string source = $@"// <auto-generated/>
using System;

namespace {mainMethod.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static partial void HelloFrom(string name) =>
            Console.WriteLine($""0.0Generator says: Hi from '{{name}}'"");
    }}
}}
";
			var typeName = mainMethod.ContainingType.Name;

			// Add the source code to the compilation
			context.AddSource($"{typeName}.g.cs", source);
		}
	}
}