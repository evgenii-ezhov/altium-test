using System.Collections.Generic;

namespace Evgenii.Ezhov.Altium.Sort;

internal struct FileLineStruct 
{
	/// <summary>
	/// Amount of string bytes for Hash
	/// </summary>
	private const int HashSize = 8;

	/// <summary>
	/// Hash for first comparison
	/// </summary>
	public ulong Hash;

	/// <summary>
	/// Numeric part of string
	/// </summary>
	public long Number;
	
	/// <summary>
	/// Source line
	/// </summary>
	public string Text;

	/// <summary>
	/// First text byte - used in comparator
	/// </summary>
	public int Offset;
	
	public static FileLineStruct Get(string line)
	{
		long number = 0;
		ulong hash = 0;
		int hashPos = 0;
		int offset = 0;
		int i;
		for (i = 0; i < line.Length; i++)
		{
			var c = line[i];
			
			if (c == '.')
			{
				i++;
				offset = i;
				break;
			}			
			number = number * 10 + (c - '0');
		}

		for (; i < line.Length; i++)
		{
			var c = line[i];
			if (hashPos++ >= HashSize) break;
			hash = hash * 0xFF + c;
			
		}

		if (hashPos < HashSize)
		{
			for (; hashPos < HashSize; hashPos++)
			{
				hash = hash * 0xFF;
			}
		}
		return new FileLineStruct { Hash = hash, Number = number, Text = line, Offset = offset };
	}
	
	public static int Compare(FileLineStruct a, FileLineStruct b)
	{
		int cmp = a.Hash.CompareTo(b.Hash);
		if (cmp != 0) return cmp;

		cmp = string.Compare(a.Text, a.Offset, b.Text, b.Offset, a.Text.Length);
		if (cmp != 0) return cmp;

		if (a.Number < b.Number) return -1;
		if (a.Number > b.Number) return 1;

		return 0;
	}

	public class Comparer : IComparer<FileLineStruct>
	{
		public int Compare(FileLineStruct a, FileLineStruct b)
		{
			return FileLineStruct.Compare(a, b);
		}
	}
}