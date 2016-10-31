using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	class Program
	{
		static void Main(string[] args)
		{
			// strconv vb
			//	string s = "１２３ＡｂＣあいウ亜伊右";
			//	var ret = Strings.StrConv(s, VbStrConv.Katakana | VbStrConv.Lowercase | VbStrConv.Narrow);
			//	Console.WriteLine($"from:{s}\nto  :{ret}");
			char start = 'Ａ', end = 'Ｚ';
			while (true)
			{
				Console.WriteLine(start);
				start++;
				if (start == end + 1) break;
			}
			Console.ReadLine();
		}
	}
}
