using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;


class Program
{
	enum DataFileType
	{
		Lua,
		Txt,
	}

	static DataFileType _dataFileType = DataFileType.Txt;

	static void Main(string[] args)
	{
		bool printHelp = true;

		for (int i = 0, end = args.Length; i < end; ++i)
		{
			if (args[i].Equals("-lua", StringComparison.CurrentCultureIgnoreCase))
			{
				_dataFileType = DataFileType.Lua;
			}
			else
			{
				string srcPath = args[i];
				string destDirPath = args[++i];

				if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
				{
					Console.WriteLine("指定的源文件或源文件夹不存在！");
					return;
				}
				if (File.Exists(destDirPath))
				{
					Console.WriteLine("指定的目标路径不是文件夹！");
					return;
				}

				// 文件夹
				if ((File.GetAttributes(srcPath) & FileAttributes.Directory) == FileAttributes.Directory)
				{
					_ConvertAllXlsxFilesUnderDirectoryRecursively(srcPath, destDirPath);
				}
				// 文件
				else
				{
					if (Path.GetExtension(srcPath) != ".xlsx")
					{
						Console.WriteLine("指定的文件不是一个 .xlsx 文件！");
						return;
					}

					_ConvertXlsxFileToTextFile(srcPath, destDirPath);
				}

				printHelp = false;

				break;
			}
		}

		if (printHelp)
		{
			Console.WriteLine();
			Console.WriteLine("XlsxExporter [-lua] Source/Path/File.xlsx Dest/Path     转换指定的 .xlsx 文件");
			Console.WriteLine("XlsxExporter [-lua] Source/Path Dest/Path               转换指定文件夹下的所有 .xlsx 文件，包括子文件夹");
			Console.WriteLine();
		}
	}

	static void _ConvertAllXlsxFilesUnderDirectoryRecursively(string sourceDirPath, string destDirPath)
	{
		//if (Directory.Exists(destDirPath))
		//{
		//	Directory.Delete(destDirPath, true);
		//}

		HashSet<string> sourceFilenames = new HashSet<string>();

		// 转换所有 .xlsx 文件
		foreach (var file in Directory.GetFiles(sourceDirPath, "*.xlsx"))
		{
			bool isHidden = ((File.GetAttributes(file) & FileAttributes.Hidden) == FileAttributes.Hidden);
			if (isHidden)
				continue;

			sourceFilenames.Add( Path.GetFileNameWithoutExtension( file ) );

			_ConvertXlsxFileToTextFile(file, destDirPath);
		}
		// 删除所有没有对应 .xlsx 的目标文件
		foreach( var file in Directory.GetFiles( destDirPath, "*.*" ) )
		{
			bool isHidden = ( ( File.GetAttributes( file ) & FileAttributes.Hidden ) == FileAttributes.Hidden );
			if( isHidden )
				continue;

			if( !sourceFilenames.Contains( Path.GetFileNameWithoutExtension( file ) ) )
			{
				File.Delete( file );
			}
		}

		// 递归遍历所有子文件夹
		foreach (var directory in Directory.GetDirectories(sourceDirPath, "*"))
		{
			DirectoryInfo info = new DirectoryInfo(directory);
			bool isHidden = ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
			if (isHidden)
				continue;

			_ConvertAllXlsxFilesUnderDirectoryRecursively(directory, Path.Combine(destDirPath, Path.GetFileName(directory)));
		}
	}

	static void _ConvertXlsxFileToTextFile(string xlsxFilePath, string destDirPath)
	{
		DataFileBase dataFile;
		switch( _dataFileType )
		{
		case DataFileType.Lua:
			dataFile = new LuaDataFile();
			break;
		default:
			dataFile = new TxtDataFile();
			break;
		}

		string targetFilePath = Path.Combine(destDirPath, Path.GetFileNameWithoutExtension(xlsxFilePath) + dataFile.GetTextFileExt());
		if( File.GetLastWriteTime( xlsxFilePath ) <= File.GetLastWriteTime( targetFilePath ) )
			return;

		DataSet ds = _ReadXlsxFile(xlsxFilePath);
		if (ds == null)
			return;

		if (!Directory.Exists(destDirPath))
		{
			Directory.CreateDirectory(destDirPath);
		}
		using (dataFile)
		{
			try
			{
				dataFile.SaveToDataFile( ds.Tables[0], targetFilePath );
			}
			catch( IllegalLuaDataFileException e )
			{
				Utils.Log( string.Format( "{0} ({1})", e.Message, xlsxFilePath ) );

				Utils.ExitApplication( -1 );
			}
		}
	}

	static DataSet _ReadXlsxFile(string filePath)
	{
		if (!File.Exists(filePath))
		{
			Console.WriteLine(string.Format("指定 .xlsx 文件不存在！（{0}）", filePath));
			return null;
		}

		OleDbConnection conn = null;
		OleDbDataAdapter da = null;
		DataSet ds = null;

		try
		{
			// 初始化连接，并打开
			string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=\"Excel 12.0;HDR=NO;IMEX=1\"";

			conn = new OleDbConnection(connectionString);
			conn.Open();

			// 获取数据源的表定义元数据                       
			DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

			// 尝试获取第一个工作表
			string sheetName = null;
			for (int i = 0; i < dtSheet.Rows.Count; ++i)
			{
				sheetName = (string)dtSheet.Rows[i]["TABLE_NAME"];

				if( sheetName == "Sheet1$" )
					break;                   
			}
			if( string.IsNullOrEmpty( sheetName ) || sheetName != "Sheet1$" )
			{
				Console.WriteLine(string.Format("未找到要转换的工作表！请检查文件。（{0}）", filePath));
				return null;
			}

			// 初始化适配器
			da = new OleDbDataAdapter();
			da.SelectCommand = new OleDbCommand(String.Format("Select * FROM [{0}]", sheetName), conn);

			ds = new DataSet();
			da.Fill(ds, sheetName);
		}
		catch (Exception e)
		{
			Console.WriteLine("连接到 Excel 失败！");
			Console.WriteLine("你可能需要安装 \"2007 Office system 驱动程序：数据连接组件\": http://www.microsoft.com/zh-CN/download/details.aspx?id=23734");
			Console.WriteLine(e.ToString());
		}
		finally
		{
			// 关闭连接
			if (conn.State == ConnectionState.Open)
			{
				conn.Close();
				da.Dispose();
				conn.Dispose();
			}
		}

		return ds;
	}
}