using System.Collections.Generic;

namespace Evgenii.Ezhov.Altium.Sort;

/// <summary>
/// Decorator for FileLineStruct, contains Reader number for ambiguous resolving on SortedDictionary
/// </summary>
internal struct ReaderFileLineStruct
{
	public FileLineStruct Line;
	public int Reader;
	public static ReaderFileLineStruct Get(string line, int reader)
	{
		return new ReaderFileLineStruct 
		{
			Line = FileLineStruct.Get(line),
			Reader = reader,
		};
	}

	public static int Compare(ReaderFileLineStruct a, ReaderFileLineStruct b)
	{
		int cmp = FileLineStruct.Compare(a.Line, b.Line);
		if (cmp != 0) return cmp;
		return a.Reader.CompareTo(b.Reader);
	}

	public class Comparer : IComparer<ReaderFileLineStruct>
	{
		public int Compare(ReaderFileLineStruct a, ReaderFileLineStruct b)
		{
			return ReaderFileLineStruct.Compare(a, b);
		}
	}
}