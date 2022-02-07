using AR.P2.Algo;
using AR.P2.Manager.Services;
using AR.P2.Manager.Utility;
using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Algo
{
    public class FftTask : CustomTask
    {
        public const int WindowSize = 4096;
        public const int SamplingRate = 44100;
        public const int BitDepth = 16;
        public const int ChannelCount = 1;
        public const double Frequency = 800;
        public const double MaxRelativeError = 0.05;

        private ImmutableList<CustomResourceFile> inputFiles;

        public FftTask(
            CustomTaskSettings customTaskSettings = null,
            ImmutableList<CustomResourceFile> customResources = null,
            CustomScheduler scheduler = null)
            : base(
                  null,
                  DefaultState,
                  customTaskSettings,
                  ImmutableList.Create(
                      customResources.Concat(customResources.Select(r => new CustomResourceFile(GetOutputFilePath(r))))
                        .Cast<CustomResource>().ToArray()),
                  scheduler)
        {
            ;
            Run = RunAction();

            if (inputFiles?.Count == 0)
                throw new ArgumentException(null, nameof(customResources));

            inputFiles = customResources;

            FftCompoundTaskState fftCompoundTaskState = State as FftCompoundTaskState;
            foreach (var item in inputFiles)
            {
                fftCompoundTaskState.FftTaskSubStates.Add(
                    new FftTaskState
                    {
                        InputFilePath = item.Uri,
                        OutputFilePath = GetOutputFilePath(item),
                    });
            }
        }

        private static string GetOutputFilePath(CustomResourceFile item)
        {
            return item.Uri.Replace(".wav", $"_output.csv");
        }

        public static FftCompoundTaskState DefaultState => new();

        // TODO use canc token
        // TODO progress updates
        private Action<ICustomTaskState, CustomCancellationToken> RunAction()
        {
            return (iState, cToken) =>
            {
                var state = iState as FftCompoundTaskState;

                using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cToken.CancellationToken, cToken.PauseToken);
                var localToken = linkedCancellationToken.Token;

                localToken.ThrowIfCancellationRequested();

                PreProcessResources(state, linkedCancellationToken.Token);

                localToken.ThrowIfCancellationRequested();

                ParallelizedResources(linkedCancellationToken.Token)
                    .ForAll(i =>
                    {
                        var fftTaskState = state.FftTaskSubStates[i];
                        if (fftTaskState.Results is null)
                        {
                            fftTaskState.Results = FftProcessResource(i, state, Settings, linkedCancellationToken.Token);
                        }
                    });

                localToken.ThrowIfCancellationRequested();

                ParallelizedResources(linkedCancellationToken.Token)
                    .ForAll(i =>
                    {
                        var fftTaskState = state.FftTaskSubStates[i];
                        if (!fftTaskState.WrittenToFile)
                        {
                            bool noException = true;
                            try
                            {
                                LockResourceAndAct(fftTaskState.OutputFilePath, () =>
                                {
                                    using var fs = File.OpenWrite(fftTaskState.OutputFilePath);
                                    using var writer = new StreamWriter(fs);

                                    foreach (var res in fftTaskState.Results)
                                    {
                                        foreach (var specComp in res.SpectralComponents)
                                        {
                                            writer.WriteLine($"{specComp.Frequency},{specComp.Magnitude}");
                                        }
                                    }
                                });
                            }
                            catch (Exception)
                            {
                                noException = false;
                                throw;
                            }
                            finally
                            {
                                if (noException)
                                    fftTaskState.WrittenToFile = true;
                            }
                        }
                    });

                Progress = 100;

                localToken.ThrowIfCancellationRequested();
            };
        }

        // TODO use canc token
        // TODO use derived type for task settings - will include FFT params instead of constants
        // TODO progress updates
        // TODO save state
        private static unsafe List<FftResult> FftProcessResource(
            int resourceIndex,
            FftCompoundTaskState state,
            CustomTaskSettings settings,
            CancellationToken cToken)
        {
            var subState = state.FftTaskSubStates[resourceIndex];
            var signal = subState.Signal;
            var windowSize = WindowSize;
            var samplingRate = SamplingRate;

            var signalSpan = signal.AsSpan();
            var signalCount = signal.Length;

            var cpuCount = settings.MaxCores;

            var parallelSignalCount = signalCount / cpuCount / windowSize * cpuCount * windowSize;
            if (parallelSignalCount == 0)
                throw new ArgumentException($"Signal is too short for fully efficient parallelization. Use an input signal of at least {cpuCount * windowSize} samples.", nameof(signal));

            var parallelTaskCount = parallelSignalCount / cpuCount;
            var remainingSignalCount = signalCount - parallelSignalCount;
            var seqSignalCount = remainingSignalCount / windowSize * windowSize;

            var processingTasks = new Task<List<KeyValuePair<SubTaskInfo, FftResult>>>[cpuCount];
            // TODO save in in state
            for (int i = 0; i < cpuCount; i++)
            {
                int taskIndex = i;
                processingTasks[i] = Task.Factory.StartNew(() =>
                {
                    fixed (double* signalPtr = signal)
                    {
                        // TODO save signalSpan in state
                        var signalSpan = new Span<double>(signalPtr, signal.Length);
                        var slicedSpan = signalSpan.Slice(taskIndex * windowSize, parallelTaskCount);

                        return Fft.ParallelInner(taskIndex, signal: slicedSpan, windowSize, parallelTaskCount, samplingRate);
                    }
                }, cToken);
            }

            Task.WaitAll(processingTasks, cToken);

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
            return result;
        }

        // TODO progress updates
        private void PreProcessResources(FftCompoundTaskState state, CancellationToken cToken)
        {
            if (inputFiles.Count == 1)
            {
                GetFileWithTaskState(state, 0, out var file, out var subState);
                if (subState.Signal is null)
                    LockResourceAndAct(file, () =>
                    {
                        using var fs = File.OpenRead(file.Uri);

                        subState.Signal = fs.GetWavSignalMono();
                    });
            }
            else
            {
                LockResourcesAndAct(
                    ImmutableList.Create(inputFiles.Cast<CustomResource>().ToArray()),
                    () =>
                {
                    ParallelizedResources(cToken)
                        .ForAll(i =>
                        {
                            GetFileWithTaskState(state, i, out var file, out var subState);
                            if (subState.Signal is null)
                            {
                                using var fs = File.OpenRead(file.Uri);

                                subState.Signal = fs.GetWavSignalMono();
                            }
                        });
                });
            }
        }

        private void GetFileWithTaskState(FftCompoundTaskState state, int i, out CustomResource file, out FftTaskState subState)
        {
            file = inputFiles[i];
            subState = state.FftTaskSubStates[i];
        }

        private ParallelQuery<int> ParallelizedResources(CancellationToken cToken)
        {
            return Enumerable.Range(0, inputFiles.Count).AsParallel()
                                .WithDegreeOfParallelism(Settings.MaxCores)
                                .WithCancellation(cToken);
        }

        public override FftTask Deserialize(string json)
        {
            JsonSerializer.Deserialize<FftCompoundTaskState>(json);
            throw new NotImplementedException();
        }
    }

    public class FftCompoundTaskState : ICustomTaskState
    {
        public List<FftTaskState> FftTaskSubStates { get; set; } = new();
    }

    public class FftTaskState : ICustomTaskState
    {
        public string InputFilePath { get; init; }
        public string OutputFilePath { get; init; }
        internal double[] Signal { get; set; }
        internal List<FftResult> Results { get; set; }
        public bool WrittenToFile { get; set; }
    }
}
