using OPOS.P1.Fs.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using DokanNet;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace OPOS.P1.WinForms
{
    public partial class Form1 : Form
    {
        private const int dokanThreadCount = 1;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public Form1()
        {
            AllocConsole();

            var computerInfo = new ComputerInfo();

            Func<long> getTotalMemory = () =>
            {
                return (long)computerInfo.AvailablePhysicalMemory;
            };
            Func<long> getFreeMemory = () =>
            {
                return (long)computerInfo.AvailablePhysicalMemory - Environment.WorkingSet;
            };
            var fileSystem = new FileSystem(
                getTotalMemory: getTotalMemory,
                getFreeMemory: getFreeMemory
                );

            fileSystem.OnFileWrite += (s, e) =>
            {
                Task.Run(() =>
                {
                    string fileName = e.File.Name;
                    //if (Path.GetExtension(fileName) == ".wav")
                    //{

                    //}
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

            System.IO.File.Copy(@"G:\downloads\output1.txt", @"B:\input\output1.txt");
            //System.IO.File.Copy(@"G:\downloads\f5982351-8fe4-4a9f-b371-b0a0bee55823-results.txt", @"b:\input\f5982351-8fe4-4a9f-b371-b0a0bee55823-results.txt", true);

            InitializeComponent();

        }

    }
}
