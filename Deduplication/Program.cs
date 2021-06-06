using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Deduplication
{
    class Program
    {
        static ConfigurationReader _config = new ConfigurationReader();

        static void Main(string[] args)
        {
            string target = _config.GetConfig<string>("TargetFolderPath");
            var source = _config.GetConfig<string[]>("SourceFolderPaths");
            var sample = _config.GetConfig<JObject>("ExtensionFolderMapping");

            string sourceFolderPaht = @"I:\视频照片备份合集";
            string targetFolderPath = @"H:\图片视频去重集合";
            string specificFolder = string.Empty;
            int totalSyncCount = 0;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //已经存在的文件名
            HashSet<string> existFileNames = new HashSet<string>();

            //目标扩展名集合
            HashSet<string> pictureExtensions = new HashSet<string>() { ".jpg", ".png", ".gif", ".jpeg" };
            HashSet<string> videoExtensions = new HashSet<string>() { ".mp4", ".3gp" };

            //遍历目标文件夹，把所有文件都记录下来，保证拷贝只是增量更新
            DirectoryInfo targetInfo = new DirectoryInfo(targetFolderPath);
            var targetFiles = targetInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);
            int totalTargetCount = targetFiles.Length;
            int currentTargetCount = 0;
            foreach (FileSystemInfo i in targetFiles)
            {
                currentTargetCount++;
                string lastModifiedTime = i.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                existFileNames.Add(lastModifiedTime);
                Console.WriteLine($"({currentTargetCount}/{totalTargetCount})           记录已有文件:{i.Name}");
            }

            //遍历资源文件夹
            DirectoryInfo sourceInfo = new DirectoryInfo(sourceFolderPaht);
            var sourceFiles = sourceInfo.GetFileSystemInfos("*", SearchOption.AllDirectories);
            int totalSourceCount = sourceFiles.Length;
            int currentCourceCount = 0;
            foreach (FileSystemInfo i in sourceFiles)
            {
                currentCourceCount++;
                //文件夹直接不要了
                if (i.Attributes == FileAttributes.Directory)
                    continue;

                //获取最近修改时间作为文件的新名称
                string lastModifiedTime = i.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                if (existFileNames.Contains(lastModifiedTime))
                {
                    Console.WriteLine($"去重: {i.FullName}");
                    continue;
                }
                else
                    existFileNames.Add(lastModifiedTime);

                //通过后缀区分文件，分别存到不同的文件夹
                if (pictureExtensions.Contains(i.Extension.ToLower()))
                    specificFolder = "图片";
                else if (videoExtensions.Contains(i.Extension.ToLower()))
                    specificFolder = "视频";
                else
                    specificFolder = "其他";

                //复制文件过去,如果包含名称中包含中文则保留原有名称
                string newFileName = Regex.IsMatch(i.Name, @"[\u4e00-\u9fa5]") && !i.Name.Contains("截图") ? i.Name : lastModifiedTime + i.Extension;
                string targetFullName = Path.Combine(targetFolderPath, specificFolder, newFileName);
                try
                {
                    File.Copy(i.FullName, targetFullName);
                    totalSyncCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                Console.WriteLine($"({currentCourceCount}/{totalSourceCount})           {targetFullName}");
            }
            watch.Stop();
            Console.WriteLine($"同步结束，共同步{totalSyncCount}个文件, 耗时:{watch.Elapsed.TotalMinutes}分钟");
            Console.ReadKey();
        }
    }
}
