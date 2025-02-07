using System;
using System.IO;

namespace Evgenii.Ezhov.Altium.Sort;

internal class ChunkReader: IDisposable
{
	public ReaderFileLineStruct Current { get; private set; } = default;

	private const int MaxTotalBufferSize = 2_000_000;

	private int _bufferSize;

	private StreamReader _streamReader;

	private ReaderFileLineStruct[] _buffer;
	private int _bufferPosition;
	private int _bufferCurrentSize;

	private bool _noMoreLines = false;

	private int _readerNumber;

	public ChunkReader(string fileName, int number, int chunkAmount)
	{
		_bufferSize = MaxTotalBufferSize / chunkAmount;
		_streamReader = new StreamReader(fileName);

		_buffer = new ReaderFileLineStruct[_bufferSize];
		_bufferPosition = 1;
		_bufferCurrentSize = 0;

		_noMoreLines = false;
		_readerNumber = number;
		Next();
	}
	
	private void ReadBuffer()
	{
		_bufferPosition = 0;
		if (_noMoreLines)
		{
			_bufferCurrentSize = 0;
			return;
		}
		
		_bufferCurrentSize = _bufferSize - 1;
		for (int i = 0; i < _bufferSize; i++)
		{
			string? line = _streamReader.ReadLine();

			if (!string.IsNullOrEmpty(line))
			{
				_buffer[i] = ReaderFileLineStruct.Get(line, _readerNumber);
			}
			else
			{
				_bufferCurrentSize = i - 1;
				_noMoreLines = true;
				return;
			}
		}
	}

	public bool Next()
	{
		if (_bufferPosition > _bufferCurrentSize)
		{
			ReadBuffer();

			if (_bufferCurrentSize < 1)
			{
				Current = default;
				return false;
			}
		}
		Current = _buffer[_bufferPosition++];
		return true;
	}

	public void Dispose()
	{
		try
		{
			if (_streamReader?.BaseStream?.CanRead == true)
			{
				_streamReader.Close();
				_streamReader = null!;
			}

			_buffer = null!;
		}
		catch { }
	}
}