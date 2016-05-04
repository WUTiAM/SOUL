using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;


abstract class DataFileBase : IDisposable
{
	//------------------------------------------------------------------------------------------------------------------
	// SaveToDataFile
	//------------------------------------------------------------------------------------------------------------------

	public abstract string GetTextFileExt();

	protected enum DataType
	{
		Invalid,

		Bool,
		Int,
		Float,
		String,
		Array,
		Dict,
	}

	protected static readonly string[] DATA_TYPE_STRINGS = new string[]
	{
		"bool",
		"int",
		"float",
		"string",
		"array",
		"dict"
	};
	protected static readonly DataType[] DATA_TYPES = 
	{
		DataType.Bool,
		DataType.Int,
		DataType.Float,
		DataType.String,
		DataType.Array,
		DataType.Dict
	};

	protected string[] _columnTypeStrings;
	protected DataType[] _columnDataTypes;
	protected string[] _columnNames;
	protected string[] _columnDescriptions;

	protected KeyValuePair<int, int>[] _arrayInfo; // Key: DataType, value: length
	protected int[] _dictInfo;

	protected int _currentRow;
	protected int _currentColumn;
	protected int _currentLevel;

	public void SaveToDataFile(DataTable dt, string dataFilePath)
	{
		_InitializeAllColumnsDefs(dt);

		Encoding utf8WithoutBom = new UTF8Encoding(false);
		using (StreamWriter sw = new StreamWriter(dataFilePath, false, utf8WithoutBom))
		{
			_DoWriteFileBegin(sw);

			for (int r = 3, end = dt.Rows.Count; r < end; ++r)
			{
				_currentRow = r;

				if (_CheckEmptyRow(dt.Rows[r]))
				{
					continue;
				}

				_DoWriteDataRowBegin(sw);
				{
					_WriteDataRow(dt.Rows[r], sw);
				}
				_DoWriteDataRowEnd(sw);

			}

			_DoWriteFileEnd(sw);
		}
	}

	protected void _InitializeAllColumnsDefs(DataTable dt)
	{
		_columnTypeStrings = new string[dt.Columns.Count];
		_columnDataTypes = new DataType[_columnTypeStrings.Length];
		_columnNames = new string[_columnTypeStrings.Length];
		_columnDescriptions = new string[_columnTypeStrings.Length];

		_arrayInfo = new KeyValuePair<int, int>[_columnTypeStrings.Length];
		_dictInfo = new int[_columnTypeStrings.Length];

		for (int c = 0, end = dt.Columns.Count; c < end; ++c)
		{
			_InitializeColunmBasicDefs(dt, c);

			// 数组类型特殊处理
			// array[colunmCount:type]
			if (_columnDataTypes[c] == DataType.Array)
			{
				c += _InitializeColunmArrayDefs(c);
			}
			// 字典类型特殊处理
			// dict[colunmCount]
			else if (_columnDataTypes[c] == DataType.Dict)
			{
				int defStart = _columnTypeStrings[c].IndexOf('[');
				int defEnd = _columnTypeStrings[c].LastIndexOf(']');
				if (defStart == -1 || defEnd == -1)
				{
					throw new IllegalLuaDataFileException(
						string.Format("字典类型定义未找到配对的“[]”：“{0}”", _columnTypeStrings[c]),
						2, c, _columnNames[c]);
				}

				string def = _columnTypeStrings[c].Substring(defStart + 1, defEnd - defStart - 1);

				int length;
				if (!int.TryParse(def, out length))
				{
					throw new IllegalLuaDataFileException(
						string.Format("字典类型的元素个数定义非法：“{0}”", _columnTypeStrings[c]),
						2, c, _columnNames[c]);
				}

				_dictInfo[c] = length;

				int ac = 0;
				for (int i = c + 1, iend = c + length; i <= iend; ++i)
				{
					_InitializeColunmBasicDefs(dt, i);

					if (_columnDataTypes[i] == DataType.Array)
					{
						int a = _InitializeColunmArrayDefs(i);
						i += a;
						iend += a;
						ac += a;
					}
				}

				_columnTypeStrings[c] = _columnTypeStrings[c].Substring(0, defEnd + 1);

				c += length + ac;
			}
		}
	}

	protected void _InitializeColunmBasicDefs(DataTable dt, int c)
	{
		_columnTypeStrings[c] = dt.Rows[2][c].ToString().Trim().Replace("\r", " ").Replace("\n", " ");
		_columnDataTypes[c] = _GetDataTypeByString(_columnTypeStrings[c]);
		_columnNames[c] = dt.Rows[1][c].ToString();
		_columnDescriptions[c] = dt.Rows[0][c].ToString().Replace("\r", " ").Replace("\n", " ");
	}

	protected int _InitializeColunmArrayDefs(int c)
	{
		int defStart = _columnTypeStrings[c].IndexOf('[');
		int defEnd = _columnTypeStrings[c].LastIndexOf(']');
		if (defStart == -1 || defEnd != _columnTypeStrings[c].Length - 1)
		{
			throw new IllegalLuaDataFileException(
				string.Format("数组类型定义未找到配对的“[]”：“{0}”", _columnTypeStrings[c]),
				2, c, _columnNames[c]);

				
		}

		string def = _columnTypeStrings[c].Substring(defStart + 1, defEnd - defStart - 1);

		string[] ss = def.Split(':');
		if (ss.Length != 2)
		{
			throw new IllegalLuaDataFileException(
				string.Format("数组类型定义非法：“{0}”", _columnTypeStrings[c]),
				2, c, _columnNames[c]);
		}

		int length;
		if (!int.TryParse(ss[0], out length))
		{
			throw new IllegalLuaDataFileException(
				string.Format("数组类型的元素个数定义非法：“{0}”", _columnTypeStrings[c]),
				2, c, _columnNames[c]);
		}

		DataType type = _GetDataTypeByString(ss[1]);
		if (type == DataType.Invalid)
		{
			throw new IllegalLuaDataFileException(
				string.Format("数组类型的元素类型定义非法：“{0}”", _columnTypeStrings[c]),
				2, c, _columnNames[c]);
		}

		_arrayInfo[c] = new KeyValuePair<int, int>((int)type, length);

		return length - 1;
	}

	protected DataType _GetDataTypeByString(string typeString)
	{
		for (int t = 0; t < DATA_TYPE_STRINGS.Length; ++t)
		{
			if (typeString.StartsWith(DATA_TYPE_STRINGS[t], StringComparison.CurrentCultureIgnoreCase))
			{
				return DATA_TYPES[t];
			}
		}

		return DataType.Invalid;
	}

	protected bool _CheckEmptyRow(DataRow dr)
	{
		bool empty = true;

		foreach (object cell in dr.ItemArray)
		{
			if (!string.IsNullOrWhiteSpace(cell.ToString()))
			{
				empty = false;
			}
		}

		return empty;
	}

	protected void _WriteDataRow(DataRow dr, StreamWriter sw)
	{
		_DoWriteDataRowIndex(dr.ItemArray[0].ToString(), sw);

		// 遍历每一列
		for (_currentColumn = 1; _currentColumn < dr.ItemArray.Length; ++_currentColumn)
		{
			// 跳过可忽略的列
			if (string.IsNullOrEmpty(_columnNames[_currentColumn]))
				continue;

			_currentLevel = 0;

			_currentColumn += _WriteCellDataRecursively(dr.ItemArray, _currentColumn, _columnDataTypes[_currentColumn], sw);
		}
	}

	protected int _WriteCellDataRecursively(object[] cellsInRow, int c, DataType dt, StreamWriter sw)
	{
		int _lastColumn = _currentColumn;
		_currentColumn = c;

		int n = 0;
		object cell = cellsInRow[c];

		switch (dt)
		{
			case DataType.Bool:
				{
					string boolValueStr = cell.ToString();
					if (boolValueStr == "1")
					{
						_DoWriteData(true, sw);
					}
					else if (boolValueStr == "0")
					{
						_DoWriteData(false, sw);
					}
					//else
					//{
					//	throw new IllegalLuaDataFileException(
					//		string.Format("布尔类型单元格的值非法：“{0}”", cell),
					//		_currentRow, c, _columnNames[c]);
					//}
				}
				break;
			case DataType.Int:
				{
					int i;
					if (int.TryParse(cell.ToString(), out i))
					{
						_DoWriteData(i, sw);
					}
					//else
					//{
					//	throw new IllegalLuaDataFileException(
					//		string.Format("整数类型单元格的值非法：“{0}”", cell),
					//		_currentRow, c, _columnNames[c]);
					//}
				}
				break;
			case DataType.Float:
				{
					float f;
					if (float.TryParse(cell.ToString(), out f))
					{
						_DoWriteData(f, sw);
					}
					//else
					//{
					//	throw new IllegalLuaDataFileException(
					//		string.Format("浮点数类型单元格的值非法：“{0}”", cell),
					//		_currentRow, c, _columnNames[c]);
					//}
				}
				break;
			case DataType.String:
				{
					string s = "\"" + Convert.ToString(cell) + "\"";

					_DoWriteData(s, sw);
				}
				break;
			case DataType.Array:
				{
					_DoWriteArrayDataBegin(sw);
					++_currentLevel;

					for (int i = 0; i < _arrayInfo[c].Value; ++i)
					{
						_WriteCellDataRecursively(cellsInRow, c + i, (DataType)_arrayInfo[c].Key, sw);
					}

					--_currentLevel;
					_DoWriteArrayDataEnd(sw);

					n = _arrayInfo[c].Value - 1;
				}
				break;
			case DataType.Dict:
				{
					if (string.IsNullOrEmpty(cellsInRow[c].ToString()))
					{
						_DoWriteDictDataBegin(sw);
						++_currentLevel;

						for (int i = c + 1, end = c + _dictInfo[c]; i <= end; ++i)
						{
							int a = _WriteCellDataRecursively(cellsInRow, i, _columnDataTypes[i], sw);
							i += a;
							end += a;
							n += a;
						}

						--_currentLevel;
						_DoWriteDictDataEnd(sw);
					}
					else
					{
						for (int i = c + 1, end = c + _dictInfo[c]; i <= end; ++i)
						{
							if (_columnDataTypes[i] == DataType.Array)
							{
								n += _arrayInfo[i].Value - 1;
							}
						}
					}

					n += _dictInfo[c];
				}
				break;
			default:
				{
					throw new IllegalLuaDataFileException(
						string.Format("列的类型不支持：“{0}”", cell),
						2, c, _columnNames[c]);
				}
		}

		_currentColumn = _lastColumn;

		return n;
	}

	protected abstract void _DoWriteFileBegin(StreamWriter sw);
	protected abstract void _DoWriteFileEnd(StreamWriter sw);
	protected abstract void _DoWriteDataRowBegin(StreamWriter sw);
	protected abstract void _DoWriteDataRowEnd(StreamWriter sw);
	protected abstract void _DoWriteDataRowIndex(string value, StreamWriter sw);
	protected abstract void _DoWriteData(bool value, StreamWriter sw);
	protected abstract void _DoWriteData(int value, StreamWriter sw);
	protected abstract void _DoWriteData(float value, StreamWriter sw);
	protected abstract void _DoWriteData(string value, StreamWriter sw);
	protected abstract void _DoWriteArrayDataBegin(StreamWriter sw);
	protected abstract void _DoWriteArrayDataEnd(StreamWriter sw);
	protected abstract void _DoWriteDictDataBegin(StreamWriter sw);
	protected abstract void _DoWriteDictDataEnd(StreamWriter sw);

	protected void _WriteLineWithIndentation(int indentation, string line, StreamWriter sw)
	{
		if (indentation > 0)
		{
			string indent = new string('\t', indentation);
			sw.Write(indent);
		}
		sw.WriteLine(line);
	}

	public abstract void Dispose();
}

class IllegalLuaDataFileException : Exception
{
	public IllegalLuaDataFileException(string message, int row, int column, string columnName)
		: base(string.Format("{0} - 第 {1} 行，第 {2} 列({3})", message, row + 1, column + 1, columnName))
	{
	}
}

