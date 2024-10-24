using System;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.Cryptography;
namespace CopyFiles2Zip
{
 

    class Program
    {
        static void Main(string[] args)
        {
            // 处理命令行参数
            if (args.Length != 4)
            {
                Console.WriteLine("请提供四个参数，分别为参照路径1，路径2,临时存放目录，压缩文件全路径");
                Console.WriteLine("范例：Copyfiles2zip 'D:\\python' 'D:\\pythonbuild' 'D:\\temp' 'D:\\test\a.zip'");
                return;
            }

            string directoryA = args[0];
            string directoryB = args[1];
            string tempDirectory = args[2];
            string outputZipPath = args[3];

            // 执行主要逻辑
            CompareAndCopyFiles(directoryA, directoryB, tempDirectory);
            ZipAndCopy(tempDirectory, outputZipPath);
        }

        static void CompareAndCopyFiles(string directoryA, string directoryB, string tempDirectory)
        {
            directoryA = GetFullPath(directoryA);
            directoryB = GetFullPath(directoryB);
            tempDirectory = GetFullPath(tempDirectory);

            var filesInA = GetAllFilesRecursively(directoryA);

            foreach (var fileInA in filesInA)
            {
                string fileName = Path.GetFileName(fileInA);
                string relativePath = fileInA.Substring(directoryA.Length + 1);

                string fileInB = FindFileInDirectory(directoryB, fileName);

                if (fileInB != null)
                {
                    FileInfo fileInfoA = new FileInfo(fileInA);
                    FileInfo fileInfoB = new FileInfo(fileInB);

                    if (fileInfoB.LastWriteTime > fileInfoA.LastWriteTime)
                    {
                        string destinationPath = Path.Combine(tempDirectory, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        File.Copy(fileInB, destinationPath);
                    }
                }
            }
        }

        static string GetFullPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path);
            }
            return path;
        }

        static List<string> GetAllFilesRecursively(string directory)
        {
            List<string> files = new List<string>();
            GetAllFilesRecursivelyHelper(directory, files);
            return files;
        }

        static void GetAllFilesRecursivelyHelper(string directory, List<string> files)
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                files.Add(file);
            }

            foreach (string subDirectory in Directory.GetDirectories(directory))
            {
                GetAllFilesRecursivelyHelper(subDirectory, files);
            }
        }

        static string FindFileInDirectory(string directory, string fileName)
        {
            string[] files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        static void ZipAndCopy(string sourceDirectory, string outputZipPath)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), GenerateRandomFileName() + ".zip");

            using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(tempZipPath)))
            {
                AddDirectoryToZipWithRelativePath(sourceDirectory, zipStream, "");
            }

            File.Copy(tempZipPath, outputZipPath, true);
            File.Delete(tempZipPath);
        }

        static string GenerateRandomFileName()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[4];
                rng.GetBytes(data);
                return BitConverter.ToString(data).Replace("-", "");
            }
        }

        static void AddDirectoryToZipWithRelativePath(string directory, ZipOutputStream zipStream, string relativePath)
        {
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                string entryName = Path.Combine(relativePath, Path.GetFileName(file));
                ZipEntry entry = new ZipEntry(entryName);
                zipStream.PutNextEntry(entry);

                using (FileStream fileStream = File.OpenRead(file))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        zipStream.Write(buffer, 0, bytesRead);
                    }
                }

                zipStream.CloseEntry();
            }

            string[] subDirectories = Directory.GetDirectories(directory);
            foreach (string subDirectory in subDirectories)
            {
                string subRelativePath = Path.Combine(relativePath, Path.GetFileName(subDirectory));
                AddDirectoryToZipWithRelativePath(subDirectory, zipStream, subRelativePath);
            }
        }
    }
}
