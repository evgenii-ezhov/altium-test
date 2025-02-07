using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evgenii.Ezhov.Altium.Generator
{
	public class Generator
	{
		private const int MinWordAmount = 1;
		private const int MaxWordAmount = 10;

		private const int WriteBufferSize = 100 * 1024 * 1024;

		private readonly Random _random;
		private readonly string[] _words;

		public Generator()
		{
			_random = new Random();
			_words = ReadResourceFile();
		}

		public void Generate(string fileName, long sizeInBytes, CancellationToken cancellationToken)
		{
			var startTime = DateTime.Now;
			if (File.Exists(fileName))
			{
				File.Delete(fileName);
			}

			StringBuilder stringBuilder = new();
			int size;
			long globalSize = 0;

			Task? writeTask = null; 

			while (globalSize < sizeInBytes && !cancellationToken.IsCancellationRequested)
			{
				stringBuilder.Clear();
				size = 0;

				while (size < WriteBufferSize && globalSize < sizeInBytes && !cancellationToken.IsCancellationRequested)
				{
					var line = GenerateOneString();	
					size += line.Length;
					globalSize += line.Length;
					stringBuilder.Append(line);
				}

				if (cancellationToken.IsCancellationRequested) break;
				if (writeTask != null)
				{
					try
					{
						writeTask.Wait();
					}
					catch (TaskCanceledException) { }
				}
				Console.WriteLine($"{(100 * globalSize / sizeInBytes)}%  {globalSize} out of {sizeInBytes}");

				try
				{
					writeTask = File.AppendAllTextAsync(fileName, stringBuilder.ToString(), cancellationToken);
				}
				catch (TaskCanceledException) { }
			}

			if (writeTask != null)
			{
				try
				{
					writeTask.Wait();
				}
				catch (TaskCanceledException) { }
			}

			if (cancellationToken.IsCancellationRequested && File.Exists(fileName))
			{
				File.Delete(fileName);
				Console.WriteLine($"Process was broken, file {fileName} was removed");
			}
			else
			{
				Console.WriteLine($"Process was done, file {fileName} was created");
				Console.WriteLine($"Duration {(DateTime.Now - startTime).TotalSeconds} seconds");
			}
		}

		private string GenerateOneString()
		{
			var result = _random.Next(Int16.MaxValue).ToString() + ". ";

			var amount = _random.Next(MinWordAmount, MaxWordAmount);
			var wordsAmount = _words.Length;
			for (int i = 0; i < amount; i++)
			{
				result += _words[_random.Next(wordsAmount - 1)] + " ";
			}
			return result + Environment.NewLine;
		}

		private string[] ReadResourceFile()
		{
			var result = new List<string>();
			var assembly = typeof(Generator).Assembly;
			var resourceName = $"{typeof(Generator).Namespace}.Resources.wordlist.txt";
			var contentStream = assembly.GetManifestResourceStream(resourceName);
			var reader = new StreamReader(contentStream!);

			string? line = null;
			do
			{
				line = reader.ReadLine();
				if (line != null)
				{
					result.Add(line);
				}
			}
			while (line != null);
			return result.ToArray();
		}
	}
}
