using System.Collections.Generic;

namespace Evgenii.Ezhov.Altium.Sort;



internal struct FileLineStruct 
{
	private const int HashSize = 8;

	public ulong Hash;
	public long Number;
	//public string Text;

	public static FileLineStruct Get(string line)
	{
		long number = 0;
		ulong hash = 0;
		int hashPos = 0;
		string text = "";
		
		int i;
		for (i = 0; i < line.Length; i++)
		{
			var c = line[i];
			
			if (c == '.')
			{
				i++;
				text = line.Substring(i + 1);
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
		return new FileLineStruct { Hash = hash, Number = number, Text = text };
	}
	
	public static int Compare(FileLineStruct a, FileLineStruct b)
	{
		int cmp = a.Hash.CompareTo(b.Hash);
		if (cmp == 0) cmp = a.Text.CompareTo(b.Text);
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