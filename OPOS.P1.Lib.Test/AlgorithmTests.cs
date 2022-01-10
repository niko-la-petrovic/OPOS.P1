using AR.P2.Algo;
using Plotly.NET;
using System;
using System.IO;
using System.Linq;
using Xunit;
using static Plotly.NET.ImageExport.ChartExtensions;
using static AR.P2.Algo.Operations;
using Xunit.Abstractions;
using System.Text.Json;
using AR.P2.Manager.Services;
using System.Collections.Generic;

namespace OPOS.P1.Lib.Test
{
    public static class AlgorithmTestsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fftResults"></param>
        /// <remarks>Cooley Tukey works such that the second half of the resulting spectral components is mirror reflection of the first half. Thus, only half of the resulting spectral components contain all the information.</remarks>
        /// <returns></returns>
        public static IEnumerable<FftResult> UsefulFftResults(this IEnumerable<FftResult> fftResults)
        {
            var halvedResults = fftResults.Select(res =>
            {
                return new FftResult
                {
                    WindowSize = res.WindowSize,
                    SamplingRate = res.SamplingRate,
                    SpectralComponents = res.SpectralComponents
                        .Take(res.SpectralComponents.Count / 2).ToList()
                };
            });

            return halvedResults;
        }

        public static void TestFftResults(
            this IEnumerable<FftResult> fftResults,
            out IEnumerable<FftResult> usefulFftResults,
            out IEnumerable<double> relativeErrors,
            out bool allWithinMargin)
        {
            usefulFftResults = fftResults.UsefulFftResults();
            relativeErrors = usefulFftResults.Select(RelativeError());
            allWithinMargin = relativeErrors.Select(res => res <= AlgorithmTests.MaxRelativeError)
                .All(s => s);
        }

        public static Func<FftResult, double> RelativeError()
        {
            return res =>
            {
                var max = res.SpectralComponents.Max();
                var diff = AlgorithmTests.Frequency - max.Frequency;
                var relativeError = Math.Abs(diff / AlgorithmTests.Frequency);

                return relativeError;
            };
        }
    }

    public class AlgorithmTests
    {
        private readonly ITestOutputHelper output;

        public const string InputMonoFilePath = "input_800hz_44100_mono_16b.wav";
        public const string LongInputMonoFilePath = "input_2s_800hz_44100_mono_16b.wav";
        public const int WindowSize = 4096;
        public const int SamplingRate = 44100;
        public const int BitDepth = 16;
        public const int ChannelCount = 1;
        public const double Frequency = 800;
        public const double MaxRelativeError = 0.05;

        private const int _dataSectionByteCount = SamplingRate * 2;

        public AlgorithmTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CanParseWavHeader()
        {
            using var fs = File.OpenRead(InputMonoFilePath);

            var wavHeader = fs.ParseWavHeader();

            output.WriteLine(JsonSerializer.Serialize(wavHeader));

            Assert.True(wavHeader.SamplingRate == SamplingRate);
            Assert.True(wavHeader.BitDepth == BitDepth);
            Assert.True(wavHeader.DataSectionByteCount == _dataSectionByteCount);
            Assert.True(wavHeader.ChannelCount == ChannelCount);
        }

        [Fact]
        public void CanConvertSignalMono()
        {
            const string inputFile = InputMonoFilePath;
            using var fs = File.OpenRead(inputFile);

            var signal = fs.GetWavSignalMono();
            var signalPart = signal.AsSpan().Slice(0, 1000);
            var x = Enumerable.Range(0, signalPart.Length).Select(i => Convert.ToDouble(i));

            var chart = Chart2D.Chart.Line<double, double, string>(x: x, y: signalPart.ToArray());
            chart
                .WithTraceName($"Parsed signal from {InputMonoFilePath}", true)
                .WithXAxisStyle(title: Title.init("Sample index"), ShowGrid: true, ShowLine: true)
                .WithYAxisStyle(title: Title.init("Amplitude"), ShowGrid: true, ShowLine: true)
                .Show();

            SaveChart(inputFile, "signal", chart);
        }

        [Fact]
        public void CanFftSequential()
        {
            const string inputFile = InputMonoFilePath;
            using var fs = File.OpenRead(inputFile);

            var signal = fs.GetWavSignalMono();

            Algo.Fft.FftSequential(signal, WindowSize, SamplingRate, out var fftResults);

            fftResults.TestFftResults(out var usefulFftResults, out var relativeErrors, out var allWithinMargin);
            var chart = GetFftResultsChart(usefulFftResults);
            SaveFftResults(inputFile, "sequential", chart);

            Assert.True(allWithinMargin);
        }

        [Fact]
        public void CanFftParallelInner()
        {
            const string inputFile = InputMonoFilePath;
            using var fs = File.OpenRead(inputFile);

            var signal = fs.GetWavSignalMono();

            var kvps = Algo.Fft.ParallelInner(0, signal.AsSpan(), WindowSize, signal.Length, SamplingRate);

            var fftResults = kvps.Select(kvp => kvp.Value);

            fftResults.TestFftResults(out var usefulFftResults, out var relativeErrors, out var allWithinMargin);

            var chart = GetFftResultsChart(usefulFftResults);
            SaveFftResults(inputFile, "parallel-inner", chart);

            Assert.True(allWithinMargin);
        }

        [Fact]
        public void CanFftParallel()
        {
            const string inputFile = LongInputMonoFilePath;
            using var fs = File.OpenRead(inputFile);

            var signal = fs.GetWavSignalMono();

            Algo.Fft.FftParallel(signal, WindowSize, SamplingRate, out var fftResults);

            fftResults.TestFftResults(out var usefulFftResults, out var relativeErrors, out var allWithinMargin);

            var chart = GetFftResultsChart(usefulFftResults);
            SaveFftResults(inputFile, "parallel", chart);

            Assert.True(allWithinMargin);
        }

        private static GenericChart.GenericChart GetFftResultsChart(IEnumerable<FftResult> usefulFftResults)
        {
            var firstResult = usefulFftResults.First();
            var chart = Chart2D.Chart.Line<double, double, string>(x: firstResult.SpectralComponents.Select(s => s.Frequency), y: firstResult.SpectralComponents.Select(s => s.Magnitude));
            return chart;
        }

        private void SaveFftResults(string inputFile, string variant, GenericChart.GenericChart chart)
        {
            chart
                .WithTraceName($"Spectral components from {inputFile} [{variant}]", true)
                            .WithXAxisStyle(title: Title.init("Frequency"), ShowGrid: true, ShowLine: true)
                            .WithYAxisStyle(title: Title.init("Magnitude"), ShowGrid: true, ShowLine: true)
                            .Show();

            SaveChart(inputFile, $"{variant}_results", chart);
        }

        private void SaveChart(string inputFile, string prefix, GenericChart.GenericChart chart)
        {
            var outFileName = $"{prefix}_{Path.GetFileNameWithoutExtension(inputFile)}";

            output.WriteLine($"Saving chart to {outFileName}.png");
            var savePngFunc = SavePNG(outFileName, Width: 1440, Height: 2560);
            savePngFunc.Invoke(chart);
        }
    }
}
