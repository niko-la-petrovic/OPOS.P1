using AR.P2.Algo;
using Plotly.NET;
using System;
using System.IO;
using System.Linq;
using Xunit;
using static Plotly.NET.ImageExport.ChartExtensions;
using static AR.P2.Algo.Operations;

namespace OPOS.P1.Lib.Test
{
    public class UnitTests
    {
        string inputMonoFilePath = "input_44100_mono_16b.wav";

        [Fact]
        public void CanParseWavHeader()
        {
            using var fs = File.OpenRead(inputMonoFilePath);

            var wavHeader = fs.ParseWavHeader();

            Assert.True(wavHeader.SamplingRate == 44100);
            Assert.True(wavHeader.BitDepth == 16);
            Assert.True(wavHeader.DataSectionByteCount == 88200);
            Assert.True(wavHeader.ChannelCount == 1);
        }

        [Fact]
        public void CanConvertSignalMono()
        {
            using var fs = File.OpenRead(inputMonoFilePath);

            var signal = fs.GetWavSignalMono();
            var signalPart = signal.AsSpan().Slice(0, 1000);
            var x = Enumerable.Range(0, signalPart.Length).Select(i => Convert.ToDouble(i)).ToArray();

            var chart = Chart2D.Chart.Point<double, double, string>(x: x, y: signalPart.ToArray());
            chart
                .WithTraceName($"Parsed signal from {inputMonoFilePath}", true)
                .WithXAxisStyle(title: Title.init("Sample index"), ShowGrid: false, ShowLine: true)
                .WithYAxisStyle(title: Title.init("Amplitude"), ShowGrid: false, ShowLine: true)
                .Show();

            var savePngFunc = SavePNG($"signal_{Path.GetFileNameWithoutExtension(inputMonoFilePath)}", Width: 1440, Height: 2560);
            savePngFunc.Invoke(chart);
        }


    }
}
