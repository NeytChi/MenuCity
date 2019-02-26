using System;
using System.IO;
using Menu.Logger;
using System.Text;
using Menu.NDatabase;
using System.Diagnostics;
using System.Collections.Generic;
using Skeleton.NDatabase.FileData;
using System.Text.RegularExpressions;

namespace Skeleton.FileSystem
{
    public class LoaderFile
    {
        private Database database;
        private LogProgram logger;
        private Random random = new Random();
        public string CurrentDirectory = Directory.GetCurrentDirectory();
        public string PathToFiles = "/Files/";
        private Regex ContentDispositionPattern = new Regex("Content-Disposition: form-data;" +
                                                            " name=\"(.*)\"; filename=\"(.*)\"\r\n" +
                                                            "Content-Type: (.*)\r\n\r\n", RegexOptions.Compiled);
        public LoaderFile(Database database, LogProgram logProgram)
        {
            this.database = database;
            this.logger = logProgram;
        }
        public List<FileD> MultiLoading(string AsciiRequest, byte[] buffer, int bytes, int count_files)
        {
            bool endRequest = false;
            int last_position = 0;
            List<Match> dispositionsAscii = new List<Match>();
            List<Match> boundariesAscii = new List<Match>();
            List <FileD> files = new List<FileD>();
            if (CheckHeadersFileRequest(AsciiRequest))
            {
                string EndBoundaryAscii = "--" + GetBoundaryRequest(AsciiRequest);
                string EndBoundaryBase64 = EndBoundaryAscii;
                Regex endBoundaryPattern = new Regex(EndBoundaryAscii);
                while(!endRequest)
                {
                    Match contentFile = ContentDispositionPattern.Match(AsciiRequest, last_position);
                    if (contentFile.Success && boundariesAscii.Count < count_files)
                    {
                        last_position = contentFile.Index + contentFile.Length;
                        Match boundary = endBoundaryPattern.Match(AsciiRequest, last_position);
                        if (boundary.Success)
                        {
                            dispositionsAscii.Add(contentFile);
                            boundariesAscii.Add(boundary);
                        }
                    }
                    else
                    {
                        endRequest = true;
                    }
                }
                for (int i = 0; i < dispositionsAscii.Count; i++)
                {
                    byte[] fileBuffer = GetFileBufferByPosition(buffer, AsciiRequest, dispositionsAscii[i], boundariesAscii[i]);
                    CreateFileBinary(i.ToString(), CurrentDirectory + "/", fileBuffer);
                }
            }
            else
            {
                return null;
            }
            return files;
        }
        private byte[] GetFileBufferByPosition(byte[] buffer, string AsciiRequest, Match start, Match end)
        {
            try
            {
                byte[] binRequestPart = Encoding.ASCII.GetBytes(AsciiRequest.Substring(0, start.Index + start.Length));
                byte[] binBoundary = Encoding.ASCII.GetBytes(AsciiRequest.Substring(start.Index + start.Length, end.Index - start.Index - start.Length));
                int fileLength = end.Index - start.Index - start.Length;
                byte[] binFile = new byte[fileLength];
                Array.Copy(buffer, binRequestPart.Length, binFile, 0, fileLength);
                return binFile;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                logger.WriteLog(e.Message, LogLevel.Error);
                return null;
            }
        }
        public FileD GetFileStructRequest(string request) 
        {
            string subContentType = GetContentType(request.Substring(request.IndexOf("boundary=", StringComparison.Ordinal)));
            FileData file = new FileData
            {
                Type = IdentifyFileType(subContentType),
                ID = random.Next(100000000, 999999999),
                Name = GenerateIdName()
            };
            file.Path = IdentifyPathFromExtention(file.Type);
            return file;
        }
        public bool CheckHeadersFileRequest(string request)
        {
            if (request.Contains("Content-Type: multipart/form-data") || request.Contains("content-type: multipart/form-data"))
            {
                if (request.Contains("boundary="))
                {
                    if (request.Contains("Connection: keep-alive") || request.Contains("connection: keep-alive"))
                    {
                        return true;
                    }
                    else 
                    {
                        logger.WriteLog("Can not find (connection: keep-alive) in request, function CheckHeadersFileRequest", LogLevel.Error);
                        return false; 
                    }
                }
                else 
                {
                    logger.WriteLog("Can not find (boundary=) in request, function CheckHeadersFileRequest", LogLevel.Error);
                    return false; 
                }
            }
            else
            {
                logger.WriteLog("Can not find (Content-Type: multipart/form-data) in request, function CheckHeadersFileRequest", LogLevel.Error);
                return false;
            }
        }
        public byte[] GetBinaryRequest(string request, byte[] buffer, int bytes)
        {
            try
            {
                string boundary = GetBoundaryRequest(request);
                string endBoundary = "--" + boundary;
                string subContentType = GetContentType(request.Substring(request.IndexOf(boundary, StringComparison.Ordinal)));
                if (subContentType == "") { return null; }
                int startBinaryFile = request.IndexOf(subContentType, StringComparison.Ordinal) + subContentType.Length + "\r\n\r\n".Length;
                string fileWithLastBoundary = Encoding.ASCII.GetString(buffer, bytes - 100, 100);
                if (fileWithLastBoundary == "") { return null; }
                if (!fileWithLastBoundary.Contains(endBoundary)) { return null; }

                byte[] binRequestPart = Encoding.ASCII.GetBytes(request.Substring(0, startBinaryFile));
                byte[] binBoundaryLast = Encoding.ASCII.GetBytes(endBoundary);
                int fileLength = bytes - binRequestPart.Length - binBoundaryLast.Length;
                byte[] binFile = new byte[fileLength];
                Array.Copy(buffer, binRequestPart.Length, binFile, 0, fileLength);
                return binFile;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with getFileFromPost(). Message: " + e.Message);
                return null;
            }
        }
        public string GenerateIdName()
        {
            int firstArg = random.Next(100000000, 999999999);
            int secondArg = random.Next(100000, 999999);
            string id = firstArg.ToString() + secondArg.ToString();
            return id;
        }
        public string IdentifyPathFromExtention(string extention)
        {
            switch (extention)
            {
                case "image":
                    Directory.CreateDirectory(PathToFiles + "Images/");
                    return PathToFiles + "Images/";
                case "video":
                    Directory.CreateDirectory(PathToFiles + "Videos/");
                    return PathToFiles + "Videos/";
                case "audio":
                    Directory.CreateDirectory(PathToFiles + "Audios/");
                    return PathToFiles + "Audios/";
                default: return PathToFiles;
            }
        }
        public string IdentifyFileExtention(string extention)
        {
            if (extention.Contains("image"))
            {
                return extention.Substring(extention.IndexOf("image/", StringComparison.Ordinal) + "image/".Length);
            }
            else if (extention.Contains("video"))
            {
                return extention.Substring(extention.IndexOf("video/", StringComparison.Ordinal) + "video/".Length);
            }
            else if (extention.Contains("audio"))
            {
                return extention.Substring(extention.IndexOf("audio/", StringComparison.Ordinal) + "audio/".Length);
            }
            else if (extention.Contains("application"))
            {
                return extention.Substring(extention.IndexOf("application/", StringComparison.Ordinal) + "application/".Length);
            }
            else return "";
        }
        public string IdentifyFileType(string extention)
        {
            if (extention.Contains("image"))
            {
                return "image";
            }
            else if (extention.Contains("video"))
            {
                return "video";
            }
            else if (extention.Contains("audio"))
            {
                return "audio";
            }
            else if (extention.Contains("application"))
            {
                return "application";
            }
            else return "";
        }
        public string GetFileName(string substring)
        {
            int first, last;
            first = (substring.IndexOf("filename=\"", StringComparison.Ordinal)) + "filename=\"".Length;
            last = substring.IndexOf("\"", first, StringComparison.Ordinal);
            string filename = substring.Substring(first, (last - first));
            return filename;
        }
        public string GetContentType(string substring)
        {
            bool exist = false;
            string contentType = "";
            int i = 0;
            int first = (substring.IndexOf("Content-Type: ", StringComparison.Ordinal)) + "Content-Type: ".Length;
            if (first == -1) { return ""; }
            string subRequest = substring.Substring(first);
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    contentType += subRequest[i];
                    i++;
                }
                if (i > 2000) { return ""; }
            }
            return contentType;
        }
        public bool CreateFileBinary(string name, string pathToSave, byte[] byteArray)
        {
            try
            {
                using (Stream fileStream = new FileStream(pathToSave + name, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(byteArray, 0, byteArray.Length);
                    fileStream.Close();
                }
                logger.WriteLog("Get file from request. File name " + name, LogLevel.FileWork);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exeception caught in createFileFromByte: {0}", e.Message);
                logger.WriteLog("Exeception caught in createFileFromByte:" + e.Message, LogLevel.Error);
                return false;
            }
        }
        public string GetBoundaryRequest(string request)
        {
            int i = 0;
            bool exist = false;
            string boundary = "";
            string subRequest = "";
            int first = request.IndexOf("boundary=", StringComparison.Ordinal);
            if (first == -1)
            {
                logger.WriteLog("Can not search boundary from request", LogLevel.Error);
                return "";
            }
            first += 9;                                     // boundary=.Length
            if (request.Length > 2500 + first)
            {
                subRequest = request.Substring(first, 2000);
            }
            else
            {
                subRequest = request.Substring(first);
            }
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    boundary += subRequest[i];
                    i++;
                }
                if (i > 2000)
                {
                    logger.WriteLog("Can not define end of boundary request", LogLevel.Error);
                    return "";
                }
            }
            return boundary;
        }
        public string SearchPathToFile(string nameFile, string startSearchFolder)
        {
            string findPathFile = "";
            string pathCurrent = startSearchFolder;
            string[] files = Directory.GetFiles(pathCurrent);
            foreach (string file in files)
            {
                if (file == pathCurrent + "/" + nameFile) { return file; }
            }
            string[] folders = Directory.GetDirectories(pathCurrent);
            foreach (string folder in folders)
            {
                FileAttributes attr = File.GetAttributes(folder);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    findPathFile = SearchPathToFile(nameFile, folder);
                }
            }
            return findPathFile;
        }
        public bool DeleteFile(FileD file)
        {
            if (File.Exists(file.file_path + file.file_name))
            {
                File.Delete(file.file_path + file.file_name);
            }
            else
            {
                logger.WriteLog("Input file->file_path+file_name not exists, function DeleteFile", LogLevel.Error);
                return false;
            }
            database.DeleteFileByID(file.file_id);
            logger.WriteLog("Delete file id=" + file.file_id, LogLevel.FileWork);
            return true;
        }
    }
}