using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Yonder.EventModels;
using YonderMedia.DB;
using YonderMedia.Functional.FileWork;

namespace Yonder.Functional.FileWork
{
    public class LoaderFile
    {
        private DatabaseYonder database;
        private LogProgram logger;
        private Random random = new Random();
        public string Domen_Name = "";
        public string pathToFiles = "/Files/";
        public string UrlToMainScreen = "http://{0}/MainScreen/";
        public string UrlToFilesImage = "http://{0}/Files/Images/";
        public string DailyDirectory = Convert.ToString((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds) + "/";
        private string PathMainScreen = Directory.GetCurrentDirectory() + "/MainScreen/";
        private Regex ContentDisposition = new Regex("Content-Disposition: form-data;" +
                                                     " name=\"(.*)\"; filename=\"(.*)\"\r\n" +
                                                     "Content-Type: (.*)\r\n\r\n",RegexOptions.Compiled);
        public LoaderFile(DatabaseYonder callDB, LogProgram logProgram)
        {
            this.database = callDB;
            this.logger = logProgram;
            Config config = new Config();
            Domen_Name = config.getValueFromJson("domen");
            pathToFiles = config.currentDirectory + pathToFiles;
            UrlToMainScreen = string.Format(UrlToMainScreen, Domen_Name);
            UrlToFilesImage = string.Format(UrlToFilesImage, Domen_Name);
            Directory.CreateDirectory(PathMainScreen);
        }
        /// <summary>
        /// Adds the event image.
        /// </summary>
        /// <returns>The event image.</returns>
        /// <param name="request">Request.</param>
        /// <param name="buffer">Buffer.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="event_id">Event identifier. Set -1 to not get event id parameter</param>
        public EventImage AddEventImage(string request, byte[] buffer, int bytes, int event_id)
        {
            if (CheckHeadersFileRequest(request))
            {
                byte[] binFile = GetBinaryRequest(request, buffer, bytes);
                if (binFile == null)
                {
                    return null;
                }
                else
                {
                    EventImage image = new EventImage();
                    image.created_at = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 1, 1, 1)).TotalSeconds;
                    image.ImageName = Convert.ToString(random.Next(1, 2147483608));
                    image.ImagePathServer = IdentifyPathFromExtention("image") + image.ImageName;
                    image.ImagePathUrl = UrlToFilesImage + DailyDirectory + image.ImageName;
                    image.event_id = event_id; 
                    CreateFileBinary(image.ImagePathServer, binFile);
                    EventImage last_image = database.SelectImagesByEventId(event_id);
                    if (last_image != null)
                    {
                        DeleteFile(last_image.ImagePathServer);
                        database.DeleteEventImage(last_image.event_image_id);
                    }
                    database.AddEventImage(image);
                    image = database.SelectImagesByEventId(event_id);
                    return image;
                }
            }
            else { return null; }
        }
        public MainScreen AddMainScreen(string request, byte[] buffer, int bytes)
        {
            if (CheckHeadersFileRequest(request))
            {
                byte[] binFile = GetBinaryRequest(request, buffer, bytes);
                if (binFile == null)
                {
                    return null;
                }
                else
                {
                    string subContentType = GetContentType(request.Substring(request.IndexOf("boundary=", StringComparison.Ordinal)));
                    MainScreen screen = new MainScreen();
                    screen.MainScreenName = Convert.ToString(random.Next(1, 2147483608));
                    screen.MainScreenPathServer = PathMainScreen + screen.MainScreenName;
                    screen.MainScreenPathUrl = UrlToMainScreen + screen.MainScreenName;
                    CreateFileBinary(screen.MainScreenPathServer, binFile);
                    database.AddMainScreen(screen);
                    return screen;
                }
            }
            else { return null; }
        }
        public FileStruct AddFile(string request, byte[] buffer, int bytes, int id)
        {
            if (CheckHeadersFileRequest(request))
            {
                byte[] binFile = GetBinaryRequest(request, buffer, bytes);
                if (binFile == null)
                {
                    return null;
                }
                else
                {
                    FileStruct file = GetFileStructRequest(request);
                    file.UID = id;
                    CreateFileBinary(file.Name, file.Path, binFile);
                    database.AddFile(file);
                    return file;
                }
            }
            else { return null; }
        }
        public FileStruct AddFile(string request, byte[] buffer, int bytes, string pathToSave)
        {
            if (CheckHeadersFileRequest(request))
            {
                byte[] binFile = GetBinaryRequest(request, buffer, bytes);
                if (binFile == null)
                {
                    return null;
                }
                else
                {
                    FileStruct file = GetFileStructRequest(request);
                    file.Name = GetFileName(request);
                    file.Path = pathToSave;
                    CreateFileBinary(file.Name, file.Path, binFile);
                    database.AddFile(file);
                    return file;
                }
            }
            else { return null; }
        }
        public FileStruct GetFileStructRequest(string request) 
        {
            string subContentType = GetContentType(request.Substring(request.IndexOf("boundary=", StringComparison.Ordinal)));
            FileStruct file = new FileStruct
            {
                Type = IdentifyFileType(subContentType),
                Name = GenerateIdName(),
                Extention = IdentifyFileExtention(subContentType)
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
                        if (request.Contains("Content-Length:") || request.Contains("content-length:")) return true;
                        else { return false; }
                    }
                    else { return false; }
                }
                else { return false; }
            }
            else return false;
        }
        public byte[] GetBinaryRequest(string request, byte[] buffer, int bytes)
        {
            try
            {
                string boundary = GetBoundary(request);
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
                    Directory.CreateDirectory(pathToFiles + "Images/" + DailyDirectory);
                    return pathToFiles + "Images/" + DailyDirectory;
                case "video":
                    Directory.CreateDirectory(pathToFiles + "Videos/" + DailyDirectory);
                    return pathToFiles + "Videos/" + DailyDirectory;
                case "audio":
                    Directory.CreateDirectory(pathToFiles + "Audios/" + DailyDirectory);
                    return pathToFiles + "Audios/" + DailyDirectory;
                default: return pathToFiles;
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
        public static string GetBetween(string source, string init, string end)
        {
            int start;
            if (source.Contains(init) && source.Contains(end))
            {
                start = source.IndexOf(init, 0, StringComparison.Ordinal) + init.Length;
                int endI = source.IndexOf(end, start, StringComparison.Ordinal);
                return source.Substring(start, endI - start);
            }
            else { return ""; }
        }
        public string GetBoundary(string substring)
        {
            bool exist = false;
            string boundary = "";
            int i = 0;
            int first = (substring.IndexOf("boundary=", StringComparison.Ordinal)) + "boundary=".Length;
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
                    boundary += subRequest[i];
                    i++;
                }
                if (i > 2000) { return ""; }
            }
            return boundary;
        }
        public bool CreateFileBinary(string full_path, byte[] byteArray)
        {
            try
            {
                using (Stream fileStream = new FileStream(full_path, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(byteArray, 0, byteArray.Length);
                    fileStream.Close();
                }
                logger.WriteLog("Create file from request.", LogLevel.FileWork);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exeception caught in createFileFromByte: {0}", e.Message);
                logger.WriteLog("Exeception caught in createFileFromByte", LogLevel.Error);
                return false;
            }
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
        public string GetContentDisposition(string substring)
        {
            int first = (substring.IndexOf("Content-Disposition", StringComparison.Ordinal)) + "Content-Disposition:".Length;
            if (first == -1) { return ""; }
            int last = substring.IndexOf("Content-Type:", StringComparison.Ordinal);
            if (last == -1) { return ""; }
            string content_disposition = substring.Substring(first, (last - first));
            content_disposition = content_disposition.Replace("\t", "").Replace("\r", "").Replace("\n", "");
            return content_disposition;
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
        public bool DeleteFile(FileStruct file)
        {
            File.Delete(file.Path + file.Name);
            database.DeleteFileByID(file.ID);
            logger.WriteLog("Delete file id=" + file.ID, LogLevel.FileWork);
            return true;
        }
        public bool DeleteFile(string full_path)
        {
            if (File.Exists(full_path))
            {
                File.Delete(full_path);
                logger.WriteLog("Delete file from server", LogLevel.FileWork);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}