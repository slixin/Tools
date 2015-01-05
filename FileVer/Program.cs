using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FileVer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new Exception("Please specify file path");

            string file = args[0];

            if (!System.IO.File.Exists(file))
                throw new Exception("File does not exists.");


            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(file);

            Console.WriteLine(myFileVersionInfo.FileVersion);
        }
    }
}
