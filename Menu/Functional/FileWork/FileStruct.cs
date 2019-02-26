using System;
using System.IO;

namespace Yonder.Functional.FileWork
{
    public class FileStruct
    {
        public int ID = 0;
        public int UID = -1;
        public string Path = Directory.GetCurrentDirectory();
        public string Name = "";
        public string Type = "";
        public string Extention = "";
        public int position;
    }
}
