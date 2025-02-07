using System;
using System.CommandLine;
using System.IO;
using System.Threading;

namespace Evgenii.Ezhov.Altium.Generator
{
	internal class Program
	{
		private static CancellationTokenSource? tokenSource;

		/*
		  Use launchSettings.json for provide arguments or provide it from command line

	      Arguments: 
			  -o <o>          Output file (required)
			  -s <s>          Size in bytes (required)
		 * */
		static void Main(string[] args)
		{
			tokenSource = new CancellationTokenSource();

			var rootCommand = new RootCommand("Generator");
			var outputFileOption = new Option<FileInfo>("-o", "Output file");
			var sizeOption = new Option<long>("-s", "Size in bytes");

			rootCommand.AddOption(outputFileOption);
			rootCommand.AddOption(sizeOption);

			rootCommand.SetHandler(
				(outputFileOption, sizeOption) =>
				{
					var generator = new Generator();
					generator.Generate(outputFileOption.FullName, sizeOption, tokenSource.Token);
				}, outputFileOption, sizeOption);
			
			Console.CancelKeyPress += Console_CancelKeyPress;

			rootCommand.Invoke(args);
		}
		private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
		{
			if (tokenSource != null)
			{
				tokenSource.Cancel();
				e.Cancel = true;
			}
		}
	}
}
