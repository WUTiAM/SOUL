using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;

namespace XlsxExporterTest
{
    [TestClass()]
    public class LuaDataFileTest
    {
        [TestMethod()]
        public void GetTextFileExtTest()
        {
            LuaDataFile target = new LuaDataFile(); 

            string expected = ".txt"; 
                         
            string actual = target.GetTextFileExt();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void _DoWriteArrayDataBeginTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            target._currentLevel = 0;
            target._currentColumn = 0;
            target._columnNames = new string[] { "是否存储数" };

            string expectedString = "\t\t是否存储数 = {";

            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms))
            {
                target._DoWriteArrayDataBegin(sw);
                sw.Flush();
                string actualString = Encoding.UTF8.GetString(ms.ToArray());

                Assert.AreEqual(expectedString, actualString);

                ms.Flush();
                sw.Close();
            }          
        }

        [TestMethod()]
        public void _DoWriteDataTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            target._currentLevel = 0;
            target._currentColumn = 0;
            target._columnNames = new string[] { "是否存储数" };

            string[] expectedStrings = new string[] 
            { 
                "\t\t是否存储数 = False,\r\n",
                "\t\t是否存储数 = 23,\r\n",
                "\t\t是否存储数 = 0.56,\r\n",
                "\t\t是否存储数 = 是否存储数,\r\n"
            };

            ArrayList data = new ArrayList() { false, 23, 0.56, "是否存储数" };
            for (int i = 0; i < data.Count; ++i )
            {
                using (MemoryStream ms = new MemoryStream())
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    target._DoWriteData(data[i].ToString(), sw);                    
                    sw.Flush();
                    string actualString = Encoding.UTF8.GetString(ms.ToArray());
                   
                    Assert.AreEqual(expectedStrings[i], actualString);

                    ms.Flush();
                    sw.Close();
                }
            }             
        }

        [TestMethod()]
        public void _DoWriteDataRowIndexTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor(); 
            target._currentLevel = 0;

            string expectedString = "\t[23] = {\r\n";

            string value = "23";
            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms))
            {
                target._DoWriteDataRowIndex(value, sw);
                sw.Flush();
                string actualString = Encoding.UTF8.GetString(ms.ToArray());

                Assert.AreEqual(expectedString, actualString);

                ms.Flush();
                sw.Close();
            }                        
        }

        [TestMethod()]
        public void _DoWriteDictDataBeginTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor(); 
            target._currentLevel = 0;
            target._currentColumn = 0;
            target._columnNames = new string[] { "是否存储数" };
         
            string expectedString = "\t\t是否存储数 = {\r\n";

            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms))
            {
                target._DoWriteDictDataBegin(sw);
                sw.Flush();
                string actualString = Encoding.UTF8.GetString(ms.ToArray());

                Assert.AreEqual(expectedString, actualString);
                ms.Flush();
                sw.Close();
            }        
        }

        [TestMethod()]
        public void _WriteWithIndentationTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor(); 

            string[] expectedStrings = new string[]
            {
                "地图片段组",
                "地图片段组",
                "\t",
                "\t地图片段组"
            };

            KeyValuePair<int, string>[] testInfo = new KeyValuePair<int, string>[4];
            testInfo[0] = new KeyValuePair<int, string>(-1, "地图片段组");
            testInfo[1] = new KeyValuePair<int, string>(0, "地图片段组");
            testInfo[2] = new KeyValuePair<int, string>(1, "");
            testInfo[3] = new KeyValuePair<int, string>(1, "地图片段组");

            for (int i = 0; i < testInfo.Length; ++i)
            {
                using (MemoryStream ms = new MemoryStream())
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    target._WriteWithIndentation(testInfo[i].Key, testInfo[i].Value, sw);
                    sw.Flush();
                    string actualString = Encoding.UTF8.GetString(ms.ToArray());

                    Assert.AreEqual(expectedStrings[i], actualString);
                    ms.Flush();
                    sw.Close();
                }                                    
            }                    
        }

        [TestMethod()]
        public void _WriteCellDataRecursivelyTest()
        {
            using (LuaDataFile target = new LuaDataFile())
            {
                PrivateObject po = new PrivateObject(target);
                LuaDataFile_Accessor dsa = new LuaDataFile_Accessor(po);
                DataTable dt = _GetTestDataTable();
                dsa._InitializeAllColumnsDefs(dt);
                dsa._currentLevel = 0;
                dsa._currentRow = 1;

                // 测试正确数据
                int expected = 2;
                string expectedString = "\t\tmapSegments = { 1, 2, 3, },\r\n";

                object[] cellsInRow = 
                { 
                    "1",
                    "Demo测试地图",
                    "1",
                    "2",
                    "3",
                    "1",
                    "18",
                    "2",
                    "",
                    "1",
                    "1",
                    "9",
                    "0",
                    "1",
                    "0.05",
                    "0.05",
                    "金手指畅想"
                };

                using (MemoryStream ms = new MemoryStream())
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    int c = 2;
                    DataFileBase_Accessor.DataType datatype = DataFileBase_Accessor.DataType.Array;
                    int actual = dsa._WriteCellDataRecursively(cellsInRow, c, datatype, sw);

                    Assert.AreEqual(expected, actual);

                    sw.Flush();
                    string actualString = Encoding.UTF8.GetString(ms.ToArray());
                    ms.Flush();

                    Assert.AreEqual(expectedString, actualString);
                }

                // 测试错误数据
                string[] expectedErrorMessages = new string[] 
                { 
                    "布尔类型单元格的值非法：“3” - 第 2 行，第 1 列(id)", 
                    "整数类型单元格的值非法：“string1” - 第 2 行，第 2 列()",
                    "浮点数类型单元格的值非法：“string2” - 第 2 行，第 3 列(mapSegments)"
                };

                object[] errorCellsInRow = { "3", "string1", "string2" };
                DataFileBase_Accessor.DataType[] errorDatatypes = new DataFileBase_Accessor.DataType[] 
                { 
                    DataFileBase_Accessor.DataType.Bool, 
                    DataFileBase_Accessor.DataType.Int, 
                    DataFileBase_Accessor.DataType.Float 
                };

                for (int i = 0; i < 3; ++i)
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream())
                        using (StreamWriter sw = new StreamWriter(ms))
                        {
                            int errorActual = dsa._WriteCellDataRecursively(errorCellsInRow, i, errorDatatypes[i], sw);
                            sw.Flush();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Assert.AreEqual(expectedErrorMessages[i], ex.Message);
                    }
                }
            }           
        }

        [TestMethod()]
        public void _InitializeColunmBasicDefsTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            // 获得测试数据
            DataTable dt = _GetTestDataTable();
            target._InitializeAllColumnsDefs(dt);
            
            string[] expectedColumnTypeStrings = new string[] 
            {
                "int",
                "",
                "array[3:int]",
                "",
                "",
                "bool",
                "int",
                "int",
                "dict[4]",
                "int",
                "int",
                "array[2:int]",
                "",
                "int",
                "float",
                "float",
                "string"
            };
            string[] expectedColumnNames = new string[] 
            {
                "id",
                "",
                "mapSegments",
                "",
                "",
                "bCacheData",
                "nItemType",
                "nItemNum",
                "npc1",
                "npcId",
                "spawnMoment",
                "spawnPoint",
                "",
                "spawnType",
                "fPercent_1",
                "fPercent_2",
                "strDefaultTitle"
            };
            string[] expectedColumnDescriptions = new string[]
            {
                "索引",
                "风格描述",
                "地图片段组",
                "",
                "",
                "是否存储数",
                "奖励类型",
                "奖励数量",
                "怪物1",
                "怪物ID",
                "出生时机",
                "出生点",
                "",
                "出生类型",
                "概率1",
                "概率2",
                "默认标题"
            };
            DataFileBase_Accessor.DataType[] expectedColumnDataTypes = new DataFileBase_Accessor.DataType[]
            {
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Array,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Bool, 
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Int, 
                DataFileBase_Accessor.DataType.Dict,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Array, 
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Int, 
                DataFileBase_Accessor.DataType.Float,
                DataFileBase_Accessor.DataType.Float,
                DataFileBase_Accessor.DataType.String 
            };

            Assert.AreEqual(expectedColumnTypeStrings.Length,target._columnTypeStrings.Length);
            Assert.AreEqual(expectedColumnNames.Length, target._columnNames.Length);
            Assert.AreEqual(expectedColumnDescriptions.Length, target._columnDescriptions.Length);
            Assert.AreEqual(expectedColumnDataTypes.Length, target._columnDataTypes.Length);

            for (int i = 0; i < expectedColumnTypeStrings.Length; ++i)
            {
                target._InitializeColunmBasicDefs(dt, i);

                Assert.AreEqual(expectedColumnTypeStrings[i], target._columnTypeStrings[i]);
                Assert.AreEqual(expectedColumnDataTypes[i], target._columnDataTypes[i]);
                Assert.AreEqual(expectedColumnNames[i], target._columnNames[i]);
                Assert.AreEqual(expectedColumnDescriptions[i], target._columnDescriptions[i]);
            }
        }

        [TestMethod()]
        public void _InitializeColunmArrayDefsTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            DataTable dt = _GetTestDataTable();
            target._InitializeAllColumnsDefs(dt);
          
            // 测试正确数据

            int expected = 2;

            int c = 2;
            int actual = target._InitializeColunmArrayDefs(c);

            Assert.AreEqual(expected, actual);

            //测试错误数据

            target._columnTypeStrings = new string[] 
            { 
                "array",
                "array[",
                "array[3:int:4]",
                "array[ :int]",
                "array[3:invalid]"
            };

            string[] expectedErrorMessages = new string[]
            { 
                "数组类型定义未找到配对的“[]”：“array” - 第 3 行，第 1 列(id)",
                "数组类型定义未找到配对的“[]”：“array[” - 第 3 行，第 2 列()",
                "数组类型定义非法：“array[3:int:4]” - 第 3 行，第 3 列(mapSegments)",
                "数组类型的元素个数定义非法：“array[ :int]” - 第 3 行，第 4 列()",
                "数组类型的元素类型定义非法：“array[3:invalid]” - 第 3 行，第 5 列()"
            };

            for (int i = 0; i < target._columnTypeStrings.Length; ++i)
            {
                try
                {
                    target._InitializeColunmArrayDefs(i);
                }
                catch (System.Exception ex)
                {
                    Assert.AreEqual(expectedErrorMessages[i], ex.Message);
                }
            }
        }

        [TestMethod()]
        public void _InitializeAllColumnsDefsTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            DataTable dt = _GetTestDataTable();           
            target._InitializeAllColumnsDefs(dt);

            // 测试正确数据

            string[] expectedColumnTypeStrings = new string[] 
            { 
                "int",
                "",
                "array[3:int]",
                null,
                null,
                "bool",
                "int",
                "int",
                "dict[4]",
                "int",
                "int",
                "array[2:int]",
                null,
                "int",
                "float",
                "float",
                "string"
            };
            string[] expectedColumnNames = new string[] 
            { 
                "id", 
                "",
                "mapSegments",
                null,
                null,
                "bCacheData",
                "nItemType",
                "nItemNum",
                "npc1",
                "npcId",
                "spawnMoment",
                "spawnPoint",
                null,
                "spawnType",
                "fPercent_1",
                "fPercent_2",
                "strDefaultTitle" 
            };
            string[] expectedColumnDescriptions = new string[] 
            { 
                "索引",
                "风格描述",
                "地图片段组",
                null,
                null,
                "是否存储数",
                "奖励类型",
                "奖励数量",
                "怪物1",
                "怪物ID",
                "出生时机",
                "出生点",
                null,
                "出生类型",
                "概率1",
                "概率2",
                "默认标题"
            };
            int[] expectedDictInfo = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0 };
            KeyValuePair<int, int>[] expectedArrayInfo = new KeyValuePair<int, int>[17];
            for (int i = 0; i < 17; ++i)
            {
                if (i == 2)
                {
                    expectedArrayInfo[i] = new KeyValuePair<int, int>(2, 3);
                }
                else if (i == 11)
                {
                    expectedArrayInfo[i] = new KeyValuePair<int, int>(2, 2);

                }
                else
                {
                    expectedArrayInfo[i] = new KeyValuePair<int, int>(0, 0);
                }
            }
            DataFileBase_Accessor.DataType[] expectedColumnDataTypes = new DataFileBase_Accessor.DataType[]
            { 
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Array,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Bool,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Int, 
                DataFileBase_Accessor.DataType.Dict,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Array, 
                DataFileBase_Accessor.DataType.Invalid,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Float,
                DataFileBase_Accessor.DataType.Float,
                DataFileBase_Accessor.DataType.String 
            };            
          
            for (int i = 0; i < 17; ++i )
            {
                Assert.AreEqual(expectedColumnTypeStrings[i], target._columnTypeStrings[i]);
                Assert.AreEqual(expectedColumnDataTypes[i], target._columnDataTypes[i]);
                Assert.AreEqual(expectedColumnNames[i], target._columnNames[i]);
                Assert.AreEqual(expectedColumnDescriptions[i], target._columnDescriptions[i]);
                Assert.AreEqual(expectedDictInfo[i], target._dictInfo[i]);
                Assert.AreEqual(expectedArrayInfo[i].Key,target._arrayInfo[i].Key);
                Assert.AreEqual(expectedArrayInfo[i].Value, target._arrayInfo[i].Value);
            }

            // 测试字典错误数据

            DataTable errorDataTable1 = new DataTable();
            errorDataTable1.Columns.Add( "怪物1", Type.GetType( "System.String" ) );
            errorDataTable1.Rows.Add( new object[] { "怪物1" } );
            errorDataTable1.Rows.Add( new object[] { "npc1" } );
            errorDataTable1.Rows.Add( new object[] { "dict" } );

            DataTable errorDataTable2 = new DataTable();
            errorDataTable2.Columns.Add( "怪物2", Type.GetType( "System.String" ) );
            errorDataTable2.Rows.Add( new object[] { "怪物2" } );
            errorDataTable2.Rows.Add( new object[] { "npc2" } );
            errorDataTable2.Rows.Add( new object[] { "dict[" } );

            DataTable errorDataTable3 = new DataTable();
            errorDataTable3.Columns.Add( "怪物3", Type.GetType( "System.String" ) );
            errorDataTable3.Rows.Add( new object[] { "怪物3" } );
            errorDataTable3.Rows.Add( new object[] { "npc3" } );
            errorDataTable3.Rows.Add( new object[] { "dict[]" } );

            DataTable[] errorDataTables = new DataTable[] { errorDataTable1, errorDataTable2, errorDataTable3 };

            string[] expectedErrorMessages = new string[]
            {
                "字典类型定义未找到配对的“[]”：“dict” - 第 3 行，第 1 列(npc1)",
                "字典类型定义未找到配对的“[]”：“dict[” - 第 3 行，第 1 列(npc2)",
                "字典类型的元素个数定义非法：“dict[]” - 第 3 行，第 1 列(npc3)"
            };

            for( int i = 0; i < errorDataTables.Length; ++i )
            {
                try
                {
                    target._InitializeAllColumnsDefs( errorDataTables[i] );
                }
                catch (System.Exception ex)
                {
                    Assert.AreEqual( expectedErrorMessages[i], ex.Message );
                }
            }
        }

        [TestMethod()]
        public void _GetDataTypeByStringTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();

            DataFileBase_Accessor.DataType[] expectedTypes = new DataFileBase_Accessor.DataType[]
            { 
                DataFileBase_Accessor.DataType.Array,
                DataFileBase_Accessor.DataType.Dict,
                DataFileBase_Accessor.DataType.Float,
                DataFileBase_Accessor.DataType.String,
                DataFileBase_Accessor.DataType.Int,
                DataFileBase_Accessor.DataType.Bool,
                DataFileBase_Accessor.DataType.Invalid
            };

            // 测试类型
            string[] typeStrings = new string[] 
            { 
                "array[2:int]",
                "dict[4]",
                "float",
                "string",
                "int",
                "bool",
                "" 
            };

            for (int i = 0; i < typeStrings.Length; ++i)
            {
                DataFileBase_Accessor.DataType actual = target._GetDataTypeByString(typeStrings[i]);

                Assert.AreEqual(expectedTypes[i], actual);
            }
        }

        [TestMethod()]
        public void _WriteLineWithIndentationTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor(); 

            string[] expectedStrings = new string[] 
            { 
                "地图片段组\r\n",
                "地图片段组\r\n",
                "\t\r\n",
                "\t地图片段组\r\n"
            };
            // 测试数据
            KeyValuePair<int, string>[] testInfo = new KeyValuePair<int, string>[4];
            testInfo[0] = new KeyValuePair<int, string>( -1, "地图片段组" );
            testInfo[1] = new KeyValuePair<int, string>( 0, "地图片段组" );
            testInfo[2] = new KeyValuePair<int, string>( 1, "" );
            testInfo[3] = new KeyValuePair<int, string>( 1, "地图片段组" );

            for( int i = 0; i < testInfo.Length; ++i )
            {
                using (MemoryStream ms = new MemoryStream())
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    target._WriteLineWithIndentation( testInfo[i].Key, testInfo[i].Value, sw );
                    sw.Flush();
                    string actualString = Encoding.UTF8.GetString(ms.ToArray());

                    Assert.AreEqual(expectedStrings[i], actualString);

                    ms.Flush();
                    sw.Close();
                }
            }                  
        }

        [TestMethod()]
        public void _CheckEmptyRowTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();

            List<bool> expectedResults = new List<bool>();
            expectedResults.Add(true);
            expectedResults.Add(false);

            // 准备测试数据
            DataTable dt = new DataTable();
            dt.Columns.Add("id", Type.GetType("System.Int32"));
            dt.Columns.Add("name", Type.GetType("System.String"));
            // 添加一个空行
            dt.Rows.Add(new object[] { null, "" });
            // 添加一个有内容的行
            dt.Rows.Add(new object[] { 1, "镖师1" });

            for (int i = 0; i < dt.Rows.Count; ++i)
            {
                bool actual = target._CheckEmptyRow(dt.Rows[i]);

                Assert.AreEqual(expectedResults[i], actual);
            }
        }

        [TestMethod()]
        public void SaveToDataFileTest()
        {
            LuaDataFile_Accessor target = new LuaDataFile_Accessor();
            string path = System.IO.Directory.GetCurrentDirectory();            
            string dataFilePath = Path.Combine ( path + @"\..\..\testSaveToDataFile.txt"); 
            DataTable dt = _GetTestDataTable();         

            string expected_filename = Path.Combine ( path + @"\..\..\expectedSaveToDataFile.txt");
            List<string> Expected_fileLines = _ReadTxt(expected_filename, Encoding.Default);

            target.SaveToDataFile(dt, dataFilePath);
            Encoding utf8WithoutBom = new UTF8Encoding(false);            
            List<string> actual_fileLines = _ReadTxt(dataFilePath, utf8WithoutBom);

            Assert.AreEqual(Expected_fileLines.Count, actual_fileLines.Count);
            for (int i = 0; i < Expected_fileLines.Count; ++i)
            {
                Assert.AreEqual(Expected_fileLines[i], actual_fileLines[i]);
            }

            File.Delete(dataFilePath);
        }

        [TestMethod()]
        public void _DoWriteFileBeginTest()
        {
            TxtDataFile_Accessor target = new TxtDataFile_Accessor(); 

            string path = System.IO.Directory.GetCurrentDirectory();

            string expectedFilename = Path.Combine ( path + @"\..\..\expected_DoWriteFileBegin.txt");
            List<string> expectedFileLines = _ReadTxt(expectedFilename, Encoding.Default);

            // 获得测试数据
            DataTable dt = _GetTestDataTable();
            target._InitializeAllColumnsDefs(dt);

            string testFilename = Path.Combine ( path + @"\..\..\test_DoWriteFileBegin.txt");
            StreamWriter sw = new StreamWriter(testFilename, false, Encoding.Default);
            target._DoWriteFileBegin(sw);
            sw.Close();
            List<string> actualFileLines = _ReadTxt(testFilename, Encoding.Default);

            Assert.AreEqual(expectedFileLines.Count, actualFileLines.Count);
            for (int i = 0; i < expectedFileLines.Count; ++i)
            {
                Assert.AreEqual(expectedFileLines[i], actualFileLines[i]);
            }

            File.Delete(testFilename);
        }

        // 自定义的测试数据
        public static DataTable _GetTestDataTable()
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < 17; ++i)
            {
                dt.Columns.Add(i.ToString());
            }

            object[] oneLine = new object[17]
            {
                "索引",
                "风格描述",
                "地图片段组",
                null,
                null,
                "是否存储数",
                "奖励类型",
                "奖励数量",
                "怪物1",
                "怪物ID",
                "出生时机",
                "出生点",
                null,
                "出生类型",
                "概率1",
                "概率2",
                "默认标题" 
            };
            dt.Rows.Add(oneLine);

            oneLine = new object[17]
            { 
                "id",
                "",
                "mapSegments",
                null,
                null,
                "bCacheData",
                "nItemType",
                "nItemNum",
                "npc1",
                "npcId",
                "spawnMoment",
                "spawnPoint",
                null,
                "spawnType",
                "fPercent_1",
                "fPercent_2",
                "strDefaultTitle"
            };
            dt.Rows.Add(oneLine);

            oneLine = new object[17] 
            { 
                "int",
                "",
                "array[3:int]",
                null,
                null,
                "bool",
                "int",
                "int",
                "dict[4]",
                "int",
                "int",
                "array[2:int]",
                null,
                "int",
                "float",
                "float",
                "string"
            };
            dt.Rows.Add(oneLine);

            oneLine = new object[17] 
            {
                "1",
                "Demo测试地图",
                "1",
                "2",
                "3",
                "1",
                "18",
                "2",
                "",
                "1",
                "1",
                "9",
                "0",
                "1",
                "0.05",
                "0.05",
                "金手指畅想" 
            };
            dt.Rows.Add(oneLine);

            oneLine = new object[17] 
            { 
                "1000",
                "",
                "11",
                "2",
                "-1",
                "1",
                "1",
                "4",
                "-1",
                "",
                "",
                "",
                "",
                "",
                "0.02",
                "0.07",
                "呼朋唤友" 
            };
            dt.Rows.Add(oneLine);

            return dt;
        }

        // 读取文本内容
        public static List<string> _ReadTxt(string filename, Encoding encod)
        {
            List<string> fileLines = new List<string>();

            StreamReader sw = new StreamReader(filename, encod, false);
            while (!sw.EndOfStream)
            {
                string line = sw.ReadLine();
                fileLines.Add(line);
            }
            sw.Close();

            return fileLines;
        }
    }
}