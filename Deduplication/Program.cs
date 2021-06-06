using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Deduplication
{
    class Program
    {
        static ConfigurationReader _config = new ConfigurationReader();

        static void Main(string[] args)
        {
            try
            {
                //读取配置
                var mapping = _config.GetMapping();
                IList<string> exceptKeyWords = _config.GetStringList("ExceptKeyWords");
                IList<string> sourceFolderPahts = _config.GetStringList("SourceFolderPaths");
                string targetFolderPath = _config.GetTargetFolderPath();
                bool discardOtherFiles = _config.GetConfig<bool>("DiscardOtherFiles");

                //在目标文件夹生成配置的分组
                GenerateGroupFolders();

                int totalSyncCount = 0;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                //已经存在的文件名
                HashSet<string> existFileNames = new HashSet<string>();

                //遍历目标文件夹，把所有文件都记录下来，保证拷贝只是增量更新
                RecordAllExistingFiles(targetFolderPath);

                //遍历资源文件夹
                DeduplicationAndCopyToTargetFolder(sourceFolderPahts);

                watch.Stop();
                Console.WriteLine($"同步结束，共同步{totalSyncCount}个文件, 耗时:{watch.Elapsed.TotalMinutes}分钟");


                void GenerateGroupFolders()
                {
                    foreach (string folder in mapping.Keys)
                    {
                        string folderPath = Path.Combine(targetFolderPath, folder);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                    }
                    if (!discardOtherFiles)
                        Directory.CreateDirectory(Path.Combine(targetFolderPath, "其他"));
                }

                void RecordAllExistingFiles(string path)
                {
                    DirectoryInfo targetInfo = new DirectoryInfo(path);
                    var targetFiles = targetInfo.GetFiles("*", SearchOption.AllDirectories);
                    int totalTargetCount = targetFiles.Length;
                    int currentTargetCount = 0;
                    foreach (FileInfo i in targetFiles)
                    {
                        currentTargetCount++;
                        //以最后修改时间和文件大小作为hash键，因为有些图片竟然修改时间是一样的，真奇怪。。
                        string ID = $"{i.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss")}+{i.Length}";
                        existFileNames.Add(ID);
                        Console.WriteLine($"({currentTargetCount}/{totalTargetCount})           记录已有文件:{i.Name}");
                    }
                }

                void DeduplicationAndCopyToTargetFolder(IList<string> paths)
                {
                    //把多个源文件夹的文件合并在一起备用
                    List<FileInfo> sourceFiles = new List<FileInfo>();
                    foreach (var path in paths)
                    {
                        DirectoryInfo sourceInfo = new DirectoryInfo(path);
                        sourceFiles.AddRange(sourceInfo.GetFiles("*", SearchOption.AllDirectories));
                    }
                    int totalSourceCount = sourceFiles.Count;
                    int currentCourceCount = 0;
                    foreach (FileInfo i in sourceFiles)
                    {
                        currentCourceCount++;
                        //获取最近修改时间作为文件的新名称
                        string lastModifiedTime = i.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                        //最近修改时间和文件大小做ID，但是只有最后修改时间作为复制后的文件名
                        string ID = $"{lastModifiedTime}+{i.Length}";
                        if (existFileNames.Contains(ID))
                        {
                            Console.WriteLine($"去重: {i.FullName}");
                            continue;
                        }
                        else
                            existFileNames.Add(ID);

                        //通过后缀区分文件，分别存到不同的文件夹
                        string specificFolder = string.Empty;
                        foreach (var m in mapping)
                        {
                            if (m.Value.Contains(i.Extension.ToLower()))
                            {
                                specificFolder = m.Key;
                                break;
                            }
                        }

                        //没有匹配到任意分区，而且不丢弃设置为false
                        if (discardOtherFiles)
                            continue;
                        if (string.IsNullOrEmpty(specificFolder))
                            specificFolder = "其他";

                        //复制文件过去,如果包含名称中包含中文则保留原有名称
                        string newFileName = Regex.IsMatch(i.Name, @"[\u4e00-\u9fa5]") && !exceptKeyWords.Any(s => i.Name.Contains(s)) ? i.Name : lastModifiedTime + i.Extension;
                        string targetFullName = Path.Combine(targetFolderPath, specificFolder, newFileName);
                        try
                        {
                            int duplicateCount = 1;
                            while (File.Exists(targetFullName))
                            {
                                duplicateCount++;
                                //如果不是默认就需要做replace，而不是insert
                                if (duplicateCount == 2)
                                    targetFullName = targetFullName.Insert(targetFullName.LastIndexOf('.'), $"__{duplicateCount}");
                                else
                                    targetFullName = targetFullName.Replace($"__{duplicateCount - 1}", $"__{duplicateCount}"); 

                            }
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"未捕获异常: {ex.ToString()}");
            }
            Console.ReadKey();
        }
    }
}
