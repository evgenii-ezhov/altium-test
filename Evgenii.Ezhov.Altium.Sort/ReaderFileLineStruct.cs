using System.Collections.Generic;

namespace Evgenii.Ezhov.Altium.Sort;

internal struct ReaderFileLineStruct
{
	public FileLineStruct Line;
	public int Reader;
	public string Text;
	public static ReaderFileLineStruct Get(string line, int reader)
	{
		return new ReaderFileLineStruct 
		{
			Line = FileLineStruct.Get(line),
			Reader = reader,
			Text = line
		};
	}

	public static int Compare(ReaderFileLineStruct a, ReaderFileLineStruct b)
	{
		int cmp = a.Line.Hash.CompareTo(b.Line.Hash);
		if (cmp == 0 && a.Line.Text != null && b.Line.Text != null) cmp = a.Line.Text.CompareTo(b.Line.Text);
		if (cmp != 0) return cmp;

		if (a.Line.Number < b.Line.Number) return -1;
		if (a.Line.Number > b.Line.Number) return 1;

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