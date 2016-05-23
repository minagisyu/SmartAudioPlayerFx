using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Quala
{
	public sealed partial class Preference : IDisposable
	{
		//= Preference
		void IDisposable.Dispose() { }

		//= Entry
		readonly ConcurrentDictionary<string, Entry> _entries = new ConcurrentDictionary<string, Entry>(StringComparer.InvariantCultureIgnoreCase);

		public Preference ClearEntry()
		{
			lock (_entries) { _entries.Clear(); }
			return this;
		}

		public Preference AddEntry(string key, string jsonFilePath)
		{
			lock (_entries) { _entries[key] = new Entry(jsonFilePath); }
			return this;
		}

		public Preference RemoveEntry(string key)
		{
			lock (_entries) { Entry v; _entries.TryRemove(key, out v); }
			return this;
		}

		public Entry this[string key]
		{
			get { lock (_entries) { return _entries[key]; } }
		}

		//= Save/Load
		public void LoadAll()
		{
			lock (_entries) { foreach (var e in _entries.Values) { e.Load(); } }
		}

		public void SaveAll()
		{
			lock (_entries) { foreach (var e in _entries.Values) { e.Save(); } }
		}

		//= Json
		/// <summary>
		/// Adds indentation and line breaks to output of JavaScriptSerializer
		/// http://stackoverflow.com/questions/5881204/how-to-set-formatting-with-javascriptserializer-when-json-serializing
		/// </summary>
		internal static string FormatOutput(string jsonString)
		{
			bool escaped = false;
			bool inquotes = false;
			int column = 0;
			int indentation = 0;
			Stack<int> indentations = new Stack<int>();
			int TABBING = 4;
			StringBuilder sb = new StringBuilder();
			foreach (char x in jsonString)
			{
				sb.Append(x);
				column++;
				if (escaped)
				{
					escaped = false;
				}
				else
				{
					if (x == '\\')
					{
						escaped = true;
					}
					else if (x == '\"')
					{
						inquotes = !inquotes;
					}
					else if (!inquotes)
					{
						if (x == ',')
						{
							// if we see a comma, go to next line, and indent to the same depth
							sb.Append("\r\n");
							column = 0;
							for (int i = 0; i < indentation; i++)
							{
								sb.Append(" ");
								column++;
							}
						}
						else if (x == '[' || x == '{')
						{
							// if we open a bracket or brace, indent further (push on stack)
							indentations.Push(indentation);
							indentation = column;
						}
						else if (x == ']' || x == '}')
						{
							// if we close a bracket or brace, undo one level of indent (pop)
							indentation = indentations.Pop();
						}
						else if (x == ':')
						{
							// if we see a colon, add spaces until we get to the next
							// tab stop, but without using tab characters!
							while ((column % TABBING) != 0)
							{
								sb.Append(' ');
								column++;
							}
						}
					}
				}
			}
			return sb.ToString();
		}
	}
}
