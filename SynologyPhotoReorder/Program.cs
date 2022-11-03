using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SynologyPhotoReorder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var quit = false;

            do
            {
                try
                {
                    Console.WriteLine("1. Move Files");
                    Console.WriteLine("2. Remove duplicates (with (0) after filename but in same directory)");
                    Console.WriteLine("3. Remove duplicates (exact same file in different location)");
                    Console.WriteLine("4. ReOrder files (move to correct year\\month folder)");
                    Console.WriteLine();
                    Console.WriteLine("0. Exit");

                    switch (Console.ReadKey().KeyChar)
                    {
                        case '1':
                            string fromFolder = null;
                            string toFolder = null;
                            string excludeFolder = null;
                            //fromFolder = @"\\{myNas}\photo\PhotoLibrary\temp\";
                            //fromFolder = @"\\{myNas}\home\photo\$Import\DCIM\Camera\";
                            //fromFolder = @"\\{myNas}\home\photo\Google Photos Backup\";
                            //fromFolder = @"\\{myNas}\photo\PhotoLibrary\";
                            fromFolder = @"D:\Downloads\shared";
                            toFolder = @"\\{myNas}\photo\Shared\";
                            excludeFolder = @"\\{myNas}\photo\By Me\";

                            fromFolder = Ask("Where is the From folder: ", fromFolder);
                            toFolder = Ask("Where is the To folder: ", toFolder);
                            excludeFolder = Ask("Where is the Exclude folder: ", excludeFolder, false);

                            MoveFilesWithNewFolderStructure(fromFolder, toFolder, excludeFolder);

                            break;
                        case '2':
                            RemoveDuplicateFiles(Ask("Which Folder: ", @"\\{myNas}\photo\PhotoLibrary\"));
                            break;
                        case '3':
                            LocateExactDuplicates(Ask("Which Folder: ", @"\\{myNas}\photo\PhotoLibrary\"));
                            break;
                        case '4':
                            ReOrderFilesInFolder(Ask("Which Folder: ", @"\\{myNas}\photo\PhotoLibrary\"));
                            break;
                        case '0':
                        case 'e':
                        case 'E':
                            quit = true;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    quit = true;
                    Console.ReadKey();
                }
            } while (!quit);
        }

        private static string Ask(string question, string defaultResponse, bool repeatUntilResponse = true)
        {
            string response;

            do
            {
                Console.WriteLine(question);
                System.Windows.Forms.SendKeys.SendWait(defaultResponse);
                response = Console.ReadLine();
            } while (repeatUntilResponse && string.IsNullOrEmpty(response));


            return response;
        }

        private static void MoveFilesWithNewFolderStructure(string fromFolder, string toFolder, string excludeFolder)
        {
            if (!Directory.Exists(fromFolder)) throw new DirectoryNotFoundException($"From folder must exist: {fromFolder}");
            if (!Directory.Exists(toFolder)) throw new DirectoryNotFoundException($"To folder must exist: {toFolder}");

            List<string> excludeFileNames = null;
            if (!string.IsNullOrEmpty(excludeFolder))
            {
                var excludeDirInfo = new DirectoryInfo(excludeFolder);
                if (!excludeDirInfo.Exists) throw new DirectoryNotFoundException($"Exclude folder must exist, if provided: {excludeFolder}");
                excludeFileNames = GetAllFiles(excludeDirInfo).Select(f => f.Name).ToList();
            }

            var dirInfo = new DirectoryInfo(fromFolder);
            foreach (var file in GetAllFiles(dirInfo, true))
            {
                MoveFileToCorrectFolder(file, toFolder, excludeFileNames);
            }
        }

        private static void MoveFileToCorrectFolder(FileInfo file, string toFolder, List<string> excludeFileNames = null)
        {
            string correctFolder = GetCorrectFolderForFile(toFolder, file);
            if (!File.Exists(Path.Combine(correctFolder, file.Name)) && (excludeFileNames == null || !excludeFileNames.Contains(file.Name)))
            {
                MoveFile(file, correctFolder);
            }
            else
            {
                Console.WriteLine($"Skipping {file.FullName}");
            }
        }

        private static string GetCorrectFolderForFile(string toFolder, FileInfo file)
        {
            var earliestDate = file.CreationTime;
            if (earliestDate > file.LastAccessTime) earliestDate = file.LastAccessTime;
            if (earliestDate > file.LastWriteTime) earliestDate = file.LastWriteTime;
            string correctFolder = Path.Combine(toFolder, earliestDate.ToString("yyyy"), earliestDate.ToString("MM"));
            return correctFolder;
        }

        private static void MoveFile(FileInfo file, string newFolder)
        {
            if (!Directory.Exists(newFolder)) Directory.CreateDirectory(newFolder);
            file.MoveTo(Path.Combine(newFolder, file.Name));
            Console.WriteLine($"Moving {file.Name} to {newFolder}");
        }

        private static void RemoveDuplicateFiles(string folder)
        {
            var dirInfo = new DirectoryInfo(folder);
            if (!dirInfo.Exists) throw new DirectoryNotFoundException($"Folder doesn't exist: {folder}");
            FileInfo prevFile = null;
            foreach (var file in GetAllFiles(dirInfo, orderByName: true))
            {
                if (prevFile == null)
                {
                    prevFile = file;
                    continue;
                }
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                var prevFileNameWithoutExtension = Path.GetFileNameWithoutExtension(prevFile.Name);
                var fileStartsWithPrev = System.Text.RegularExpressions.Regex.Match(file.Name, $@"{System.Text.RegularExpressions.Regex.Escape(prevFileNameWithoutExtension)} \(+\d(\).{prevFile.Extension})");
                var prevStartsWithFile = System.Text.RegularExpressions.Regex.Match(prevFileNameWithoutExtension, $@"{System.Text.RegularExpressions.Regex.Escape(fileNameWithoutExtension)} \(+\d(\).{file.Extension})");

                if (fileStartsWithPrev.Success)
                {
                    if (file.Length <= prevFile.Length)
                    {
                        Console.WriteLine($"Deleting {file.FullName}");
                        file.Delete();
                        continue;
                    }
                    else if (file.Length > prevFile.Length)
                    {
                        Console.WriteLine($"Deleting {prevFile.FullName}");
                        prevFile.Delete();
                    }
                }
                else if (fileStartsWithPrev.Success)
                {
                    if (prevFile.Length <= file.Length)
                    {
                        Console.WriteLine($"Deleting {prevFile.FullName}");
                        prevFile.Delete();
                    }
                    else if (prevFile.Length > file.Length)
                    {
                        Console.WriteLine($"Deleting {file.FullName}");
                        file.Delete();
                        continue;
                    }
                }
                prevFile = file;
            }
            Console.WriteLine("done");
        }

        private static void LocateExactDuplicates(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) throw new DirectoryNotFoundException($"Path doesn't exist: {path}");

            FileInfo prevFile = null;
            foreach (var file in GetAllFiles(dirInfo).OrderBy(f => f.Name))
            {
                if (prevFile == null)
                {
                    prevFile = file;
                    continue;
                }
                if (file.Length == prevFile.Length)
                {
                    Console.WriteLine($"File {file.Name} is found in {Path.GetDirectoryName(file.FullName)} and in {Path.GetDirectoryName(prevFile.FullName)}");
                }
            }

            var allFiles = GetAllFiles(dirInfo).OrderBy(f => f.Name);
        }

        private static void ReOrderFilesInFolder(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) throw new DirectoryNotFoundException($"Path doesn't exist: {path}");

            foreach (var file in dirInfo.EnumerateFiles())
            {
                MoveFileToCorrectFolder(file, path);
            }
        }

        private static IEnumerable<FileInfo> GetAllFiles(DirectoryInfo root, bool removeEmptyDir = false, bool orderByName = false, List<string> excludeFolders = null)
        {

            IEnumerable<FileSystemInfo> enumeration;
            if (orderByName)
            {
                enumeration = root.EnumerateFileSystemInfos().OrderBy(fsi => fsi.Name);
            }
            else
            {
                enumeration = root.EnumerateFileSystemInfos();
            }

            foreach (var fsi in enumeration)
            {
                if (fsi is FileInfo file)
                {
                    yield return file;
                }
                else if (fsi is DirectoryInfo dir)
                {
                    if (excludeFolders == null || !excludeFolders.Contains(dir.FullName))
                    {
                        foreach (var subfile in GetAllFiles(dir, removeEmptyDir))
                        {
                            yield return subfile;
                        };
                    }
                }
            }
            if (removeEmptyDir && !root.EnumerateFileSystemInfos().Any()) root.Delete();
        }
    }
}
