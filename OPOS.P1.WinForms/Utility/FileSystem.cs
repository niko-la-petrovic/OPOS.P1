using DokanNet;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
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
                    if (Path.GetExtension(fileName) == ".wav")
                    {

                    }
                    MessageBox.Show(fileName);

                });
            };

            string fsMountPoint = @"b:\";
            string inputFolder = Path.Join(fsMountPoint, "input");
            string outputFolder = Path.Join(fsMountPoint, "output");
            Task.Run(() =>
            {
                fileSystem.Mount(fsMountPoint, DokanOptions.DebugMode | DokanOptions.StderrOutput, threadCount: dokanThreadCount, logger: null);
            });
            Thread.Sleep(200);

            System.IO.Directory.CreateDirectory(inputFolder);
            System.IO.Directory.CreateDirectory(outputFolder);

            Thread.Sleep(200);

            // TODO remove
            //System.IO.File.Copy(@"G:\downloads\kellen-riggin-U-Xa6K3Rfxk-unsplash.jpg", @"B:\output\inputkellen-riggin-U-Xa6K3Rfxk-unsplash.jpg");
            //System.IO.File.Copy(@"G:\downloads\kellen-riggin-U-Xa6K3Rfxk-unsplash1.jpg", @"B:\output\inputkellen-riggin-U-Xa6K3Rfxk-unsplash1.jpg");
        }
    }
}
