﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evgenii.Ezhov.Altium.Sort;

public class Sorter
{
	private const int ChunkSize = 2_000_000;

	private long _lineCount;
	private List<string> _chunkFiles;

	private string _inputFileName;
	private string _outputFileName;
	private string _tempFolderPath;

	private SemaphoreSlim readSemaphore = new(1);
	private int chunkCounter = 0;
	private bool _noMoreLines = false;

	private CancellationToken _cancellationToken;

	public Sorter(string inputFileName, string outputFileName)
	{
		_lineCount = 0;
		_chunkFiles = new();

		_inputFileName = inputFileName;
		_outputFileName = outputFileName;
		_tempFolderPath = Path.Combine(Path.GetDirectoryName(outputFileName)!, "temp");
	}

	public void Sort(CancellationToken cancellationToken)
	{
		_cancellationToken = cancellationToken;
		var startTime = DateTime.Now;

		if (File.Exists(_outputFileName))
		{
			File.Delete(_outputFileName);
		}

		if (!Directory.Exists(_tempFolderPath))
		{
			Directory.CreateDirectory(_tempFolderPath);
		}

		CreateChunks();

		Console.WriteLine($"Chunks part duration {(DateTime.Now - startTime).TotalSeconds} seconds");

		MergeChunks();

		if (false && !Directory.Exists(_tempFolderPath))
		{
			Directory.CreateDirectory(_tempFolderPath);
		}

		if (cancellationToken.IsCancellationRequested && File.Exists(_outputFileName))
		{
			File.Delete(_outputFileName);
			Console.WriteLine($"Process was broken, file {_outputFileName} was removed");
		}
		else
		{
			Console.WriteLine($"Process was done, file {_outputFileName} was created");
			Console.WriteLine($"Duration {(DateTime.Now - startTime).TotalSeconds} seconds");
		}
	}

	private int ReadChunk(StreamReader reader, FileLineStruct[] buffer)
	{
		string? line;
		var t = DateTime.Now;
		for (int i = 0; i < ChunkSize; i++)
		{
			if (_cancellationToken.IsCancellationRequested) return 0;
			line = reader.ReadLine();
			if (line == null) return i;
			buffer[i] = FileLineStruct.Get(line);
		}
		Console.WriteLine($"Read duration: {(DateTime.Now - t).TotalSeconds}");
		return ChunkSize;
	}

	private void SortChunk(FileLineStruct[] buffer, int? size = null)
	{
		var t = DateTime.Now;

		if (size.HasValue)
		{
			Array.Sort(buffer, 0, size.Value, new FileLineStruct.Comparer());
		}
		else
		{
			Array.Sort(buffer, FileLineStruct.Compare);
		}

		Console.WriteLine($"Sort duration: {(DateTime.Now - t).TotalSeconds}");
	}

	private void WriteChunk(FileLineStruct[] lines, int chunkIndex, int size)
	{
		var t = DateTime.Now;
		string chunkFile = Path.Combine(_tempFolderPath, $"chunk_{chunkIndex}.txt");

		using (StreamWriter writer = new StreamWriter(chunkFile))
		{
			for (int i = 0; i < size; i++)
			{
				if (_cancellationToken.IsCancellationRequested) return;
				writer.WriteLine(lines[i].Text);
			}
		}

		_chunkFiles.Add(chunkFile);

		Console.WriteLine($"Write duration: {(DateTime.Now - t).TotalSeconds}");
	}




	private async Task<(int, FileLineStruct[])> ExecuteChunk(StreamReader reader, FileLineStruct[] buffer, int? size = null)
	{
		int readAmount = -1;
		int chunkIndex = -1;
		try
		{
			await readSemaphore.WaitAsync(_cancellationToken);
			
			if (_cancellationToken.IsCancellationRequested) return (0, buffer);
			if (_noMoreLines) return (0, buffer);
			chunkIndex = chunkCounter++;
			readAmount = ReadChunk(reader, buffer);
			if (readAmount < ChunkSize)
			{
				_noMoreLines = true;
			}
		}
		finally { readSemaphore.Release(); }

		if (_cancellationToken.IsCancellationRequested) return (0, buffer);
		if (readAmount < 1) return (0, buffer);

		SortChunk(buffer, readAmount);
		if (_cancellationToken.IsCancellationRequested) return (0, buffer);

		WriteChunk(buffer, chunkIndex, readAmount);

		return (readAmount, buffer);
	}

	private void CreateChunks()
	{
		const int MaxAmount = 4;

		_chunkFiles = new List<string>();

		List<FileLineStruct[]> buffers = new();
		Queue<Task<(int, FileLineStruct[])>> tasks = new();

		for (int i = 0; i < MaxAmount; i++)
		{
			buffers.Add(new FileLineStruct[ChunkSize]);
		}

		using (StreamReader reader = new StreamReader(_inputFileName))
		{
			for (int i = 0; i < MaxAmount; i++)
			{
				var index = i;
				var task = Task.Run(() => ExecuteChunk(reader, buffers[index]));
				tasks.Enqueue(task);
			}

			var currentTask = tasks.Dequeue();

			while (currentTask != null)
			{
				currentTask.Wait();

				if (_cancellationToken.IsCancellationRequested) return;
				var (linesCount, buffer) = currentTask.Result;

				if (linesCount > 0)
				{
					_lineCount += linesCount;
					
					if (!_noMoreLines)
					{
						var newTask = Task.Run(() => ExecuteChunk(reader, buffer));
						tasks.Enqueue(newTask);
					}
				}

				if (tasks.Count > 0)
				{
					currentTask = tasks.Dequeue();
				}
				else
				{ 
					currentTask = null; 
				}
				
			}
		}		
	}


	private void MergeChunks()
	{
		const int logPeriod = 500_000;

		using (StreamWriter writer = new StreamWriter(_outputFileName))
		{
			var readers = new List<ChunkReader>();

			var sortedReaders = new SortedDictionary<ReaderFileLineStruct, ChunkReader>(new ReaderFileLineStruct.Comparer());

			var semaphoreSlim = new SemaphoreSlim(1);

			try
			{
				int readerCount = _chunkFiles.Count;

				for (int i = 0; i < _chunkFiles.Count; i++)
				{
					var chunkReader = new ChunkReader(_chunkFiles[i], i, readerCount);
					sortedReaders.Add(chunkReader.Current, chunkReader);
				}

				long currentLineCount = 0;
				ChunkReader minLineReader;
				while (readerCount > 0)
				{
					if (_cancellationToken.IsCancellationRequested) return;
					minLineReader = sortedReaders.First().Value;
					sortedReaders.Remove(minLineReader.Current);

					writer.WriteLine(minLineReader.Current.Text);

					if (!minLineReader.Next())
					{
						readers.Remove(minLineReader);
						readerCount--;
					}
					else
					{
						sortedReaders.Add(minLineReader.Current, minLineReader);
					}

					if (++currentLineCount % logPeriod == 0)
					{
						Console.WriteLine($"Writing progress: { Math.Round( 100 * ((float)currentLineCount / _lineCount), 4) } %" +
							$" {currentLineCount} out of {_lineCount}");
					}
				}
			}
			finally
			{
				foreach (var reader in readers)
					reader.Dispose();
			}
		}
	}

}
