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

namespace OPOS.P1.WinForms
{
    public partial class Form1 : Form
    {
        private const int dokanThreadCount = 1;
        public Form1()
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
            var fileSystem = new FileSystem(
                getTotalMemory: getTotalMemory,
                getFreeMemory: getFreeMemory
                );

            string fsMountPoint = @"b:\";
            string inputFolder = Path.Join(fsMountPoint, "input");
            string outputFolder = Path.Join(fsMountPoint, "output");
            Task.Run(() =>
            {
                fileSystem.Mount(fsMountPoint, DokanOptions.DebugMode | DokanOptions.StderrOutput, threadCount: dokanThreadCount);
            });
            Thread.Sleep(200);

            System.IO.Directory.CreateDirectory(inputFolder);
            System.IO.Directory.CreateDirectory(outputFolder);

            InitializeComponent();

        }

    }
}
