﻿#if !NOFILEIO
using System;
using System.IO;
using System.Threading;

namespace ChainUtils
{
	public enum FileLockType
	{
		Read,
		ReadWrite
	}
	public class FileLock : IDisposable
	{
		FileStream _fs = null;
		public FileLock(string filePath, FileLockType lockType)
		{
			if(filePath == null)
				throw new ArgumentNullException("filePath");
			if(!File.Exists(filePath))
				try
				{
					File.Create(filePath).Close();
				}
				catch
				{
				}
			var source = new CancellationTokenSource();
			source.CancelAfter(20000);
			while(true)
			{
				try
				{
					if(lockType == FileLockType.Read)
						_fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
					if(lockType == FileLockType.ReadWrite)
						_fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					break;
				}
				catch(IOException)
				{
					Thread.Sleep(50);
					source.Token.ThrowIfCancellationRequested();
				}
			}
		}


		public void Dispose()
		{
			_fs.Dispose();
		}


		//public void SetString(string str)
		//{
		//	_Fs.Position = 0;
		//	StreamWriter writer = new StreamWriter(_Fs);
		//	writer.Write(str);
		//	writer.Flush();
		//}

		//public string GetString()
		//{
		//	_Fs.Position = 0;
		//	StreamReader reader = new StreamReader(_Fs);
		//	return reader.ReadToEnd();
		//}
	}
}
#endif