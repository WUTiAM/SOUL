using System;
using System.IO;


sealed class TxtDataFile : DataFileBase
{
    public override string GetTextFileExt()
    {
        return ".txt";
    }

    protected override void _DoWriteFileBegin(StreamWriter sw)
    {
        for (int c = 1, end = _columnTypeStrings.Length; c < end; ++c)
        {
            // 跳过可忽略的列
            if (string.IsNullOrEmpty(_columnNames[c]))
                continue;

            _WriteLineWithIndentation(
                0,
                string.Format("-- {0, -20} {1, -20} （{2}）", _columnTypeStrings[c], _columnNames[c], _columnDescriptions[c]),
                sw);

            // 跳过数组元素列
            if (_columnDataTypes[c] == DataType.Array)
            {
                Console.WriteLine(_arrayInfo[c].Value);
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
                        string.Format("--   {0, -22} {1, -16} （{2}）", _columnNames[i], _columnTypeStrings[i], _columnDescriptions[i]),
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
    }

    protected override void _DoWriteFileEnd(StreamWriter sw)
    {
        _WriteLineWithIndentation(0, "--", sw);
    }

    protected override void _DoWriteDataRowBegin(StreamWriter sw)
    {
        _WriteLineWithIndentation(0, "--", sw);
    }

    protected override void _DoWriteDataRowEnd(StreamWriter sw)
    {
    }

    protected override void _DoWriteDataRowIndex(string value, StreamWriter sw)
    {
        _WriteLineWithIndentation(
            _currentLevel,
            string.Format("{0}", value),
            sw);
    }

    protected override void _DoWriteData(bool value, StreamWriter sw)
    {
        _WriteLineWithIndentation(
            _currentLevel,
            string.Format("{0}", value ? "true" : "false"),
            sw);
    }

    protected override void _DoWriteData(int value, StreamWriter sw)
    {
        _WriteLineWithIndentation(
            _currentLevel,
            string.Format("{0}", value),
            sw);
    }

    protected override void _DoWriteData(float value, StreamWriter sw)
    {
        _WriteLineWithIndentation(
            _currentLevel,
            string.Format("{0}", value),
            sw);
    }
    protected override void _DoWriteData(string value, StreamWriter sw)
    {
        _WriteLineWithIndentation(
            _currentLevel,
            string.Format("{0}", value),
            sw);
    }

    protected override void _DoWriteArrayDataBegin(StreamWriter sw)
    {
        // TODO
    }

    protected override void _DoWriteArrayDataEnd(StreamWriter sw)
    {
        // TODO
    }

    protected override void _DoWriteDictDataBegin(StreamWriter sw)
    {
        // TODO
    }

    protected override void _DoWriteDictDataEnd(StreamWriter sw)
    {
        // TODO
    }

    public override void Dispose()
    {
    }
}