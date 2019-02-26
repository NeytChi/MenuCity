using System.IO;
using System.Net.Sockets;

namespace Yonder.Functional.FileWork
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
        public bool SendingFileId(int uid, int fid, Socket handleSocket)
		{
            /*Account account = database.SelectAccountId(uid);
            if (account != null)
            {
                FileStruct file = database.SelectFileIdAccountId(fid, uid);
                if (file != null)
                {
                    var requestAnswer = Encoding.ASCII.GetBytes(SendImageRequest(file));
                    handleSocket.Send(requestAnswer, requestAnswer.Length, 0);
                    handleSocket.SendFile(file.Path + file.Name);
                    return true;
                }
                else 
                {
                    return false;
                }
            }
			else return false;*/
            return true;
		}
        public string SendImageRequest(FileStruct file)
		{
			string responseHeaders = "HTTP/1.1 200\r\n" +
                                     "Content-Length: " + new FileInfo(file.Path + file.Name).Length + "\r\n" +
                                     "Content-Type: " + file.Type + "/" + file.Extention + "\r\n" +
			                         "Content-Disposition: attachment; filename=" + file.Name + "\r\n" +
                                     "\r\n";
            return responseHeaders;
		}
	}
}
