using System;
using System.CommandLine;
using System.IO;
using System.Threading;

namespace Evgenii.Ezhov.Altium.Sort
{
	internal class Program
	{
		private static CancellationTokenSource? tokenSource;

		// Use launchSettings.json for provide arguments or provide it from command line
		static void Main(string[] args)
		{
			tokenSource = new CancellationTokenSource();

			var rootCommand = new RootCommand("Sort");
			var outputFileOption = new Option<FileInfo?>("-o", "Output file");
			var inputFileOption = new Option<FileInfo>("-i", "Input file");
			

			rootCommand.AddOption(outputFileOption);
			rootCommand.AddOption(inputFileOption);

			rootCommand.SetHandler(
				(outputFileOption, inputFileOption) =>
				{
					string outputFileName;
					if (outputFileOption == null)
					{
						outputFileName = inputFileOption + ".sorted";
					}
					else
					{
						outputFileName = outputFileOption.FullName;
					}
					var sorter = new Sorter(inputFileOption.FullName, outputFileName);
					sorter.Sort(tokenSource.Token);
				}, outputFileOption, inputFileOption);

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
