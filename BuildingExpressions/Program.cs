using System;
using Microsoft.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using BuildingExpressions.Contracts;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CodeAnalysis.CSharp;

namespace BuildingExpressions
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Out.WriteLine(
				$"{nameof(Program.CalculateWithExpressions)} = {Program.CalculateWithExpressions(2.3)}");
			Console.Out.WriteLine(
				$"{nameof(Program.CalculateWithRoslyn)} = {Program.CalculateWithRoslyn(2.3)}");
			Console.Out.WriteLine(
				$"{nameof(Program.CalculateWithScripting)} = {Program.CalculateWithScripting(2.3)}");
			Console.Out.WriteLine(
				$"{nameof(Program.BuildWorkerWithRoslyn)} = {Program.BuildWorkerWithRoslyn(2.3)}");
		}

		private static double CalculateWithExpressions(double x)
		{
			var parameter =
			  Expression.Parameter(typeof(double));
			var method = Expression.Lambda(
			  Expression.Add(
				 Expression.Divide(
					Expression.Multiply(
					  Expression.Constant(3d), parameter),
					Expression.Constant(2d)),
				 Expression.Constant(4d)),
			  parameter).Compile() as Func<double, double>;

			return method(x);
		}

		private static double CalculateWithRoslyn(double x)
		{
			var expression =
				@"public static class Expression
				{
					public static double Evaluate(double x)
					{
						return ((3 * x) / 2) + 4;
					}
				}";

			var tree = SyntaxFactory.ParseSyntaxTree(expression);
			var compilation = CSharpCompilation.Create(
				"Expression.dll",
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
				syntaxTrees: new[] { tree },
				references: new[] { MetadataReference.CreateFromAssembly(typeof(object).Assembly) });

			Assembly assembly;
			using (var stream = new MemoryStream())
			{
				compilation.Emit(stream);
				assembly = Assembly.Load(stream.GetBuffer());
			}

			var method = assembly.GetType("Expression").GetMethod("Evaluate");
			return (double)method.Invoke(null, new object[] { x });
		}

		private static double CalculateWithScripting(double x)
		{
			var expression = $"((3 * {x}) / 2) + 4";
			return (double)CSharpScript.Create(expression).Run().ReturnValue;
		}

		private static double BuildWorkerWithRoslyn(double x)
		{
			var expression =
				@"using BuildingExpressions.Contracts;

				public sealed class Worker
					: IWorker
				{
					public double Work(double x)
					{
						return ((3 * x) / 2) + 4;
					}
				}";

			var tree = SyntaxFactory.ParseSyntaxTree(expression);
			var compilation = CSharpCompilation.Create(
				"Worker.dll",
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
				syntaxTrees: new[] { tree },
				references: new[]
				{
					MetadataReference.CreateFromAssembly(typeof(object).Assembly),
					MetadataReference.CreateFromAssembly(typeof(IWorker).Assembly)
				});

			Assembly assembly;
			using (var stream = new MemoryStream())
			{
				compilation.Emit(stream);
				assembly = Assembly.Load(stream.GetBuffer());
			}

			var worker = Activator.CreateInstance(assembly.GetType("Worker")) as IWorker;
			return worker.Work(x);
		}
	}
}