using AR.P2.Algo;
using AR.P2.Manager.Services;
using AR.P2.Manager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Algo
{
    public class Fft
    {
        public static void FftSequential(
            double[] signal,
            int windowSize,
            double samplingRate,
            out List<FftResult> fftResults)
        {
            fftResults = new List<FftResult>(signal.Length);

            unsafe
            {
                fixed (double* signalPtr = signal)
                {
                    FftService.SequentialInner(signalPtr, windowSize, samplingRate, signal.Length, fftResults);
                }
            }
        }

        public static List<KeyValuePair<SubTaskInfo, FftResult>> ParallelInner(
            int taskIndex,
            Span<double> signal,
            int windowSize,
            int signalPartCount,
            double samplingRate)
        {
            var kvps = new List<KeyValuePair<SubTaskInfo, FftResult>>();

            unsafe
            {
                fixed (double* signalPtr = signal)
                {
                    int i = 0;
                    for (int k = 0; k + windowSize <= signalPartCount; k += windowSize)
                    {
                        FftResult fftResult;
                        List<Complex> complexSpecCompsList = null;
                        complexSpecCompsList = Operations.FftRecurse(signalPtr + k, windowSize);

                        fftResult = Operations.GetFftResult(complexSpecCompsList, samplingRate, windowSize);

                        kvps.Add(new KeyValuePair<SubTaskInfo, FftResult>(new SubTaskInfo { TaskIndex = taskIndex, WindowIndex = i }, fftResult));
                        i++;
                    }
                }
            }

            return kvps;
        }

        // TODO provide SIMD implementation as well

        // TODO read from input stream as each task is initialized to get processing earlier
        public unsafe static void FftParallel(double[] signal, int windowSize, double samplingRate, out List<FftResult> fftResults)
        {
            var signalSpan = signal.AsSpan();
            var signalCount = signal.Length;

            var cpuCount = Environment.ProcessorCount;

            var parallelSignalCount = signalCount / cpuCount / windowSize * cpuCount * windowSize;
            if (parallelSignalCount == 0)
                throw new ArgumentException($"Signal is too short for fully efficient parallelization. Use an input signal of at least {cpuCount * windowSize} samples.", nameof(signal));

            var parallelTaskCount = parallelSignalCount / cpuCount;
            var remainingSignalCount = signalCount - parallelSignalCount;
            var seqSignalCount = remainingSignalCount / windowSize * windowSize;

            var processingTasks = new Task<List<KeyValuePair<SubTaskInfo, FftResult>>>[cpuCount];
            for (int i = 0; i < cpuCount; i++)
            {
                int taskIndex = i;
                processingTasks[i] = Task.Factory.StartNew(() =>
                {
                    fixed (double* signalPtr = signal)
                    {
                        var signalSpan = new Span<double>(signalPtr, signal.Length);

                        return ParallelInner(taskIndex, signal: signalSpan.Slice(i * windowSize, parallelTaskCount), windowSize, parallelTaskCount, samplingRate);
                    }
                });
            }

            Task.WaitAll(processingTasks);

            var dict = new SortedDictionary<SubTaskInfo, FftResult>(new SubTaskInfoComparer());
            var seqResults = new List<FftResult>(seqSignalCount);

            if (seqSignalCount > 0)
            {
                unsafe
                {
                    fixed (double* signalPtr = signal.AsSpan().Slice(parallelSignalCount, remainingSignalCount))
                    {
                        FftService.SequentialInner(signalPtr, windowSize, samplingRate, seqSignalCount, seqResults);
                    }
                }
            }

            foreach (var t in processingTasks)
            {
                foreach (var res in t.Result)
                {
                    dict.Add(res.Key, res.Value);
                }
            }

            var result = dict.Values.Concat(seqResults).ToList();

            fftResults = result;
        }

    }
}
