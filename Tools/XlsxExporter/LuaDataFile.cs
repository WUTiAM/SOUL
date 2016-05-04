using System;
using System.IO;


class LuaDataFile : DataFileBase
{
    bool _isCurrentAnonymous = false;
    bool _isLastAnonymous;
    bool _isWritingLine = true;

    public override string GetTextFileExt()
    {
        return ".txt";
    }

    protected override void _DoWriteFileBegin(StreamWriter sw)
    {
        for (int c = 0, end = _columnTypeStrings.Length; c < end; ++c)
        {
            // 跳过可忽略的列
            if (string.IsNullOrEmpty(_columnNames[c]))
                continue;

            _WriteLineWithIndentation(
                0,
                string.Format("-- {0, -24} {1, -16} {2}", _columnNames[c], _columnTypeStrings[c], _columnDescriptions[c]),
                sw);

            // 跳过数组元素列
            if (_columnDataTypes[c] == DataType.Array)
            {
                c += _arrayInfo[c].Value - 1;
            }
            // 字典元素列
            if (_columnDataTypes[c] == DataType.Dict)
            {
                int ac = 0;

                for (int i = c + 1, iend = c + _dictInfo[c]; i <= iend; ++i)
                {
                    _WriteLineWithIndentation(
                        0,
                        string.Format("--   {0, -22} {1, -16} {2}", _columnNames[i], _columnTypeStrings[i], _columnDescriptions[i]),
                        sw);

                    if (_columnDataTypes[i] == DataType.Array)
                    {
                        // 跳过数组元素列
                        int a = _arrayInfo[i].Value - 1;
                        i += a;
                        iend += a;
                        ac += a;
                    }
                }

                c += _dictInfo[c] + ac;
            }

        }
        sw.WriteLine();

        _WriteLineWithIndentation(_currentLevel, "return {", sw);
    }

    protected override void _DoWriteFileEnd(StreamWriter sw)
    {
        _WriteLineWithIndentation(_currentLevel, "}", sw);
        sw.WriteLine();
    }

    protected override void _DoWriteDataRowBegin(StreamWriter sw)
    {
    }

    protected override void _DoWriteDataRowEnd(StreamWriter sw)
    {
        _WriteLineWithIndentation(_currentLevel + 1, "},", sw);
    }

    protected override void _DoWriteDataRowIndex(string value, StreamWriter sw)
    {
        int i;
        if (int.TryParse(value, out i))
        {
            value = '[' + value + ']';
        }

        _WriteLineWithIndentation(
            _currentLevel + 1,
            string.Format("{0} = {{", value),
            sw);
    }

    protected override void _DoWriteData(bool value, StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? string.Format("{0},", value ? "true" : "false")
            : string.Format("{0} = {1},", _columnNames[_currentColumn], value ? "true" : "false");

        if (_isWritingLine)
            _WriteLineWithIndentation(_currentLevel + 2, line, sw);
        else
            _Write(line, sw);
    }

    protected override void _DoWriteData(int value, StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? string.Format("{0},", value)
            : string.Format("{0} = {1},", _columnNames[_currentColumn], value);

        if (_isWritingLine)
            _WriteLineWithIndentation(_currentLevel + 2, line, sw);
        else
            _Write(line, sw);
    }

    protected override void _DoWriteData(float value, StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? string.Format("{0},", value)
            : string.Format("{0} = {1},", _columnNames[_currentColumn], value);

        if (_isWritingLine)
            _WriteLineWithIndentation(_currentLevel + 2, line, sw);
        else
            _Write(line, sw);
    }

    protected override void _DoWriteData(string value, StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? string.Format("{0},", value)
            : string.Format("{0} = {1},", _columnNames[_currentColumn], value);

        if (_isWritingLine)
            _WriteLineWithIndentation(_currentLevel + 2, line, sw);
        else
            _Write(line, sw);
    }

    protected override void _DoWriteArrayDataBegin(StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? "{"
            : string.Format("{0} = {{", _columnNames[_currentColumn]);

        _WriteWithIndentation(_currentLevel + 2, line, sw);

        _isLastAnonymous = _isCurrentAnonymous;
        _isCurrentAnonymous = true;
        _isWritingLine = false;
    }

    protected override void _DoWriteArrayDataEnd(StreamWriter sw)
    {
        _isWritingLine = true;
        _isCurrentAnonymous = _isLastAnonymous;

        _WriteLine("},", sw);
    }

    protected override void _DoWriteDictDataBegin(StreamWriter sw)
    {
        string line = _isCurrentAnonymous
            ? "{"
            : string.Format("{0} = {{", _columnNames[_currentColumn]);

        _WriteLineWithIndentation(_currentLevel + 2, line, sw);
    }

    protected override void _DoWriteDictDataEnd(StreamWriter sw)
    {
        _WriteLineWithIndentation(_currentLevel + 2, "},", sw);
    }

    protected void _WriteWithIndentation(int indentation, string line, StreamWriter sw)
    {
        if (indentation > 0)
        {
            string indent = new string('\t', indentation);
            sw.Write(indent);
        }
        sw.Write(line);
    }

    protected void _Write(string line, StreamWriter sw)
    {
        sw.Write(' ');
        sw.Write(line);
    }

    protected void _WriteLine(string line, StreamWriter sw)
    {
        sw.Write(' ');
        sw.WriteLine(line);
    }

    public override void Dispose()
    {
    }
}