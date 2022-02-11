using DokanNet;
using Microsoft.VisualBasic.Devices;
using OPOS.P1.Lib.Algo;
using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibFileSystem = OPOS.P1.Lib.FileSystem;

namespace OPOS.P1.WinForms.Utility
{
    public class FileSystem
    {
        public static void ConfigureInMemoryFileSystem(int dokanThreadCount)
        {
            string fsMountPointPath = @"b:\";
            string inputFolderPath = Path.Join(fsMountPointPath, "input");
            string outputFolderPath = Path.Join(fsMountPointPath, "output");

            var cpuCount = Environment.ProcessorCount;
            var schedulerSettings = new CustomSchedulerSettings { MaxConcurrentTasks = cpuCount, MaxCores = cpuCount };
            var scheduler = new CustomScheduler(schedulerSettings);

            var computerInfo = new ComputerInfo();
            Func<long> getTotalMemory = () =>
            {
                return (long)computerInfo.AvailablePhysicalMemory;
            };
            Func<long> getFreeMemory = () =>
            {
                return (long)computerInfo.AvailablePhysicalMemory - Environment.WorkingSet;
            };


            var fileSystem = new LibFileSystem.FileSystem(
                getTotalMemory: getTotalMemory,
                getFreeMemory: getFreeMemory
                );

            fileSystem.OnFileWrite += (s, e) =>
            {
                Task.Run(() =>
                {
                    string fileName = e.File.Name;
                    string filePath = e.File.FullName;
                    var parent = Directory.GetParent(filePath);
                    if (inputFolderPath != parent.FullName)
                        return;

                    if (Path.GetExtension(fileName) == ".wav")
                    {
                        var customTaskSettings = new CustomTaskSettings { Deadline = DateTime.Now.AddMinutes(10), MaxCores = cpuCount, MaxRunDuration = TimeSpan.FromMinutes(10), Parallelize = true, Priority = 0 };

                        CustomResourceFile inputFile = new CustomResourceFile(filePath);

                        var fftTask = new FftTask(customTaskSettings, customResources: ImmutableList.Create(inputFile));

                        scheduler.PrepareTask(fftTask);
                        fftTask.Start();
                    }
                });
            };

            scheduler.TaskStatusChanged += (s, e) =>
            {
                var task = e.Task;
                if (task is not FftTask fftTask)
                    return;

                if (e.Status is not TaskStatus.RanToCompletion)
                    return;

                var inputFile = fftTask.CustomResources.Where(r => r.Uri.Contains(".wav")).First();

                var inOutputFilePath = FftTask.GetOutputFilePath(inputFile as CustomResourceFile);
                var outputFilePath = Path.Join(outputFolderPath, Path.GetFileName(inOutputFilePath));

                File.Move(inOutputFilePath, outputFilePath);
            };

            Task.Run(() =>
            {
                fileSystem.Mount(DokanOptions.DebugMode | DokanOptions.StderrOutput, fsMountPointPath, threadCount: dokanThreadCount, logger: null);
            });
            Thread.Sleep(200);

            System.IO.Directory.CreateDirectory(inputFolderPath);
            System.IO.Directory.CreateDirectory(outputFolderPath);

        }
    }
}
