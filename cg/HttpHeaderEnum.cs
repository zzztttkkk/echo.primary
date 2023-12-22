using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace cg {
	[Generator]
	public class HttpHeaderEnum : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) {
		}

		public void Execute(GeneratorExecutionContext context) {
			/*
			 * JSON.stringify(
			 *		Array.from(document.querySelectorAll("table tbody tr"))
			 *			.filter(v => Array.from(v.querySelectorAll("td")).length >= 3)
			 *			.map(
			 *				v => {
			 *					var tds = Array.from(v.querySelectorAll("td"));
			 *					return {name: encodeURI(tds[0].innerHTML), desc: encodeURI(tds[1].innerHTML), exa: encodeURI(tds[2].innerHTML) }};
			 *			)
			 * )
			 */
			
			

		}
	}
}