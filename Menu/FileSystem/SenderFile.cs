using System.IO;
using System.Text;
using Menu.Logger;
using Menu.NDatabase;
using System.Net.Sockets;
using Skeleton.NDatabase.FileData;

namespace Skeleton.FileSystem
{
	public class SenderFile
	{
        private LogProgram logger;
        private Database database;

        public SenderFile(LogProgram logprogram, Database callDB)
		{
            this.logger = logprogram;
            this.database = callDB;
		}
        public string SendImageRequest(FileD file)
		{
			string responseHeaders = "HTTP/1.1 200\r\n" +
                                     "Content-Length: " + new FileInfo(file.file_path + file.file_name).Length + "\r\n" +
                                     "Content-Type: " + file.file_type + "/image\r\n" +
			                         "Content-Disposition: attachment; filename=" + file.file_name + "\r\n" +
                                     "\r\n";
            return responseHeaders;
		}
	}
}
