using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Evgenii.Ezhov.Altium.Sort;


internal class ChunkReader: IDisposable
{
	public ReaderFileLineStruct Current { get; private set; } = default;

	private const int MaxTotalBufferSize = 2_000_000;

	private int _bufferSize;

	private StreamReader _streamReader;

	//private Queue<ReaderFileLineStruct> _buffer;

	private ReaderFileLineStruct[] _buffer;
	private int _bufferPosition;

	private bool _noMoreLines = false;


	private int _readerNumber;

	public ChunkReader(string fileName, int number, int chunkAmount)
	{
		_bufferSize = MaxTotalBufferSize / chunkAmount;
		_streamReader = new StreamReader(fileName);
		//_buffer = new Queue<ReaderFileLineStruct>(_bufferSize);

		_buffer = new ReaderFileLineStruct[_bufferSize];
		_bufferPosition = -1;

		_noMoreLines = false;
		_readerNumber = number;
		Next();
	}
	
	private void ReadBuffer()
	{
		if (_noMoreLines) return;

		_bufferPosition = _bufferSize - 1;
		for (int i = 0; i < _bufferSize; i++)
		{
			string? line = _streamReader.ReadLine();

			if (!string.IsNullOrEmpty(line))
			{
				//_buffer.Enqueue(ReaderFileLineStruct.Get(line, _readerNumber));
				_buffer[i] = ReaderFileLineStruct.Get(line, _readerNumber);
			}
			else
			{
				_bufferPosition = i - 1;
				_noMoreLines = true;
				return;
			}
		}
	}

	public bool Next()
	{
		if (_bufferPosition < 0)
		{
			ReadBuffer();

			if (_bufferPosition < 0)
			{
				Current = default;
				return false;
			}
		}
		Current = _buffer[_bufferPosition--];
		return true;
		/*if (_buffer.Count == 0)
		{
			ReadBuffer();

			if (_buffer.Count == 0)
			{
				IsEmpty = true;
				Current = default;
				return false;
			}
		}
		Current = _buffer.Dequeue();
		return true;*/
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

