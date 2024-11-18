using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts_AOT.Utility
{
    public static class FileHelper
    {
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }
        public static void FileClearWrite(string path, byte[] data)
        {
            if (FileHelper.FileExists(path))
            {
                // Delete the file   
                FileHelper.FileDelete(path);
            }

            string parentDir = path.Remove(path.LastIndexOf("/"));
            if (!FileHelper.DirectoryExists(parentDir))
            {
                FileHelper.CreateDirectory(parentDir);
            }

            FileHelper.FileWrite(path, data);
        }
        public static void FileWrite(string path, byte[] data)
        {
            BinaryWriter sw = new BinaryWriter(File.Open(path, FileMode.CreateNew));
            sw.Write(data);
            sw.Close();
        }
        public static void FileWrite(string path, string data)
        {
            StreamWriter sw = new StreamWriter(path);
            sw.Write(data);
            sw.Close();
        }


        public static string FileRead(string path, string encoding)
        {
            return File.ReadAllText(path);
        }
        public static byte[] FileRead(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static void FileDelete(string path)
        {
            File.Delete(path);
        }
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}
