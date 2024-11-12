using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ListenerGui.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ListenerGui
{
    public partial class MainWindow : Window
    {
        private EventPipeSession _session;
        private int _pid;
        private readonly ManualResetEventSlim _mutex = new();
        private bool _realSize = true;
        private bool _showReservedMemory = true;
        private bool _showEmptyMemory = false;
        private bool _tiledView = false;
        private List<Frame> _frames = new();
        private Frame? _activeFrame;
        private bool _playing = true;
        private DispatcherTimer _playTimer;

        public MainWindow()
        {
            InitializeComponent();

            _playTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _playTimer.Tick += PlayTimer_Tick;

            DataContext = this;

            foreach (var value in Enum.GetValues<Generation>())
            {
                var color = Region.GetColor(value);

                PanelLegend.Children.Add(new Rectangle
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(color),
                    Margin = new Thickness(15, 5, 5, 5)
                });

                PanelLegend.Children.Add(new TextBlock
                {
                    Text = value.ToString(),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            PanelLegend.Children.Add(new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.LightGray),
                Margin = new Thickness(15, 5, 5, 5)
            });

            PanelLegend.Children.Add(new TextBlock
            {
                Text = "Empty",
                VerticalAlignment = VerticalAlignment.Center
            });

            PanelLegend.Children.Add(new TextBlock
            {
                Text = "#",
                VerticalAlignment = VerticalAlignment.Center,
                Height = 20,
                Margin = new Thickness(15, 5, 5, 5),
                FontWeight = FontWeights.Bold,
                FontSize = 16
            });

            PanelLegend.Children.Add(new TextBlock
            {
                Text = "Heap index",
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        public ObservableCollection<Gc> GCs { get; } = new();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out var pid))
                {
                    _ = Task.Factory.StartNew(() => Listen(pid), TaskCreationOptions.LongRunning);
                }
                else
                {
                    Load(args[1]);
                }
            }
            else
            {
                _ = Task.Factory.StartNew(() => Listen(null), TaskCreationOptions.LongRunning);
            }

            _playTimer.Start();
            _ = Task.Factory.StartNew(InspectProcess, TaskCreationOptions.LongRunning);
        }

        private void Listen(int? pid)
        {
            pid ??= GetProcessId();

            _pid = pid.Value;

            Dispatcher.BeginInvoke(() =>
            {
                Title += $" - Attached to {pid}";
            });

            _mutex.Set();

            _session = CreateSession(pid.Value);

            var source = new EventPipeEventSource(_session.EventStream);

            source.NeedLoadedDotNetRuntimes();
            source.AddCallbackOnProcessStart(process =>
            {
                process.AddCallbackOnDotNetRuntimeLoad(runtime =>
                {
                    runtime.GCEnd += GCEnd;
                });
            });

            source.Clr.All += Clr_All;
            source.AllEvents += Source_AllEvents;
            source.Process();
        }

        private unsafe void Source_AllEvents(TraceEvent obj)
        {
            if (obj.ID == (TraceEventID)39)
            {
                var ptr = (byte*)obj.DataStart;

                var eventName = new string((char*)ptr);

                if (eventName != "HeapCountTuning")
                {
                    return;
                }

                ptr += eventName.Length * 2 + 2;

                ptr += 6; // I don't know why

                var nHeaps = *(short*)ptr;
                var gcIndex = *(long*)(ptr + 2);
                var medianThroughputCostPercent = *(float*)(ptr + 10);
                var smoothedMedianThroughputCostPercent = *(float*)(ptr + 14);
                var tcpReductionPerStepUp = *(float*)(ptr + 18);
                var tcpIncreasePerStepDown = *(float*)(ptr + 22);
                var scpIncreasePerStepUp = *(float*)(ptr + 26);
                var scpDecreasePerStepDown = *(float*)(ptr + 30);

                // Display all those values
                Debug.WriteLine($"nHeaps: {nHeaps}, gcIndex: {gcIndex}, medianThroughputCostPercent: {medianThroughputCostPercent}, smoothedMedianThroughputCostPercent: {smoothedMedianThroughputCostPercent}, tcpReductionPerStepUp: {tcpReductionPerStepUp}, tcpIncreasePerStepDown: {tcpIncreasePerStepDown}, scpIncreasePerStepUp: {scpIncreasePerStepUp}, scpDecreasePerStepDown: {scpDecreasePerStepDown}");

                Dispatcher.BeginInvoke(() =>
                {
                    TextThroughputCost.Text = $"{medianThroughputCostPercent:0.00}%";
                });

                if (medianThroughputCostPercent > 10.0f)
                {
                    // Increase aggressively
                }
                else if (smoothedMedianThroughputCostPercent > 5.0f)
                {
                    // Increase
                }
                else if ((tcpReductionPerStepUp - scpIncreasePerStepUp) >= 1.0f)
                {
                    // Increase
                }
                else if (smoothedMedianThroughputCostPercent < 1.0f && (scpDecreasePerStepDown - tcpIncreasePerStepDown) >= 1.0f)
                {
                    // Reduction
                }
            }
        }

        private void Clr_All(TraceEvent obj)
        {
        }

        private void GCEnd(TraceProcess process, TraceGC gc)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (_pid != 0)
                {
                    GCs.Insert(0, new(gc));
                }
            });

            _mutex.Set();
        }

        private static EventPipeSession CreateSession(int pid)
        {
            var provider = new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.GC);

            var client = new DiagnosticsClient(pid);
            var session = client.StartEventPipeSession(provider);

            return session;
        }

        private void InspectProcess()
        {
            while (true)
            {
                _mutex.Wait();
                _mutex.Reset();

                if (_pid == 0)
                {
                    continue;
                }

                using var dataTarget = DataTarget.AttachToProcess(_pid, true);

                var runtime = dataTarget.ClrVersions[0].CreateRuntime();

                if (!runtime.Heap.CanWalkHeap)
                {
                    continue;
                }

                using var process = Process.GetProcessById(_pid);
                var privateMemoryMb = process.PrivateMemorySize64 / (1024.0 * 1024);
                var subHeaps = runtime.Heap.SubHeaps.Select(s => new SubHeap(s)).ToList();

                var frame = new Frame
                {
                    PrivateMemoryMb = privateMemoryMb,
                    SubHeaps = subHeaps,
                    GcNumber = GCs.Count > 0 ? GCs[0].Number : -1
                };

                Dispatcher.BeginInvoke(() => AddFrame(frame));
            }
        }

        private void AddFrame(Frame frame)
        {
            _frames.Add(frame);

            if (ScrollFrames.Value == ScrollFrames.Maximum && _playing)
            {
                ScrollFrames.Maximum++;
                ScrollFrames.Value++;
            }
            else
            {
                ScrollFrames.Maximum++;
            }

            ScrollFrames.Minimum = 1;

            TextStep.Text = $"{(int)ScrollFrames.Value} / {_frames.Count}";
        }

        private void RefreshView(Frame? frame = null)
        {
            frame ??= _activeFrame;

            if (frame == null)
            {
                return;
            }

            _activeFrame = frame;

            TextStep.Text = $"{(int)ScrollFrames.Value} / {_frames.Count}";

            if (_activeFrame.GcNumber != -1)
            {
                ListGc.SelectedItem = GCs.FirstOrDefault(gc => gc.Number == _activeFrame.GcNumber);

                if (ListGc.SelectedItem != null)
                {
                    ListGc.ScrollIntoView(ListGc.SelectedItem);
                }
            }
            else
            {
                ListGc.SelectedItem = null;
            }

            var subHeaps = frame.SubHeaps;
            var privateMemoryMb = frame.PrivateMemoryMb;

            TextPid.Text = _pid.ToString();
            TextNbHeaps.Text = subHeaps.Count.ToString();

            TextPrivateBytes.Text = $"{(long)privateMemoryMb} MB";

            if (_tiledView)
            {
                PanelRegions.Children.Clear();
                PanelRegions.Visibility = Visibility.Collapsed;
                RegionsGrid.Visibility = Visibility.Visible;
                RegionsGrid.SetRegions(subHeaps.SelectMany(h => h.Segments).ToList());
            }
            else
            {
                PanelRegions.Visibility = Visibility.Visible;
                RegionsGrid.Visibility = Visibility.Collapsed;

                var regions = PanelRegions.Children.OfType<Region>().ToList();
                var newRegions = new List<Region>();

                foreach (var heap in subHeaps)
                {
                    foreach (var segment in heap.Segments.OrderBy(s => s.Start))
                    {
                        if (segment.Flags.HasFlag((ClrSegmentFlags)32))
                        {
                            Debug.WriteLine($"Skipping decommitted region - {segment.Address:x2}");
                            //Debugger.Launch();
                            //continue;
                        }

                        if (segment.Generation == Generation.Frozen)
                        {
                            continue;
                        }

                        var region = regions.FirstOrDefault(r => r.Address == segment.Start);

                        if (region == null)
                        {
                            region = new Region(segment, heap.Index, _realSize, _showReservedMemory);
                        }
                        else
                        {
                            region.Update(segment, heap.Index, _realSize, _showReservedMemory);
                        }

                        newRegions.Add(region);
                    }
                }

                foreach (var region in regions)
                {
                    if (!newRegions.Any(r => r.Address == region.Address) && !region.IsDeleted)
                    {
                        if (!_showEmptyMemory)
                        {
                            region.Delete();
                            newRegions.Add(region);
                        }
                    }
                }

                PanelRegions.Children.Clear();

                ulong lastRegionEnd = 0;

                foreach (var region in newRegions.OrderBy(r => r.Segment.Start))
                {
                    var start = region.Segment.Start;
                    var end = region.Segment.ReservedMemory.End;

                    if (_realSize && _showEmptyMemory && lastRegionEnd != 0)
                    {
                        if (start - lastRegionEnd >= 64)
                        {
                            var diff = start - lastRegionEnd;
                            var diffInMB = ToMB(diff);

                            var placeholder = new Grid { Width = diffInMB * 10, Height = 40 };

                            placeholder.Children.Add(new Rectangle { Fill = new SolidColorBrush(Colors.LightGray), });

                            placeholder.Children.Add(new TextBlock
                            {
                                HorizontalAlignment = diffInMB < 10 ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Center,
                                Text = $"{diffInMB} MB",
                                FontSize = 14,
                                FontWeight = FontWeights.Bold,
                                Margin = diffInMB < 10 ? new Thickness(0) : new Thickness(5)
                            });

                            PanelRegions.Children.Add(placeholder);
                        }
                    }

                    lastRegionEnd = end;

                    PanelRegions.Children.Add(region);
                }
            }
        }

        private static double ToMB(ulong length)
        {
            return Math.Round(length / (1024.0 * 1024), 2);
        }

        private static int GetProcessId()
        {
            while (true)
            {
                var processes = Process.GetProcessesByName("DatasTest");

                if (processes.Length > 0)
                {
                    return processes[0].Id;
                }

                Thread.Sleep(500);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void RadioReal_Checked(object sender, RoutedEventArgs e)
        {
            _realSize = true;
            _showReservedMemory = true;
            RefreshView();
        }

        private void RadioLogical_Checked(object sender, RoutedEventArgs e)
        {
            _realSize = false;
            _showReservedMemory = true;
            RefreshView();
        }

        private void RadioCommitted_Checked(object sender, RoutedEventArgs e)
        {
            _realSize = true;
            _showReservedMemory = false;
            RefreshView();
        }

        private void ToggleEmpty_Click(object sender, RoutedEventArgs e)
        {
            _showEmptyMemory = ToggleEmpty.IsChecked == true;
            RefreshView();
        }

        private void ToggleTiles_Click(object sender, RoutedEventArgs e)
        {
            _tiledView = ToggleTiles.IsChecked == true;
            RefreshView();
        }

        private void ScrollFrames_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_frames.Count == 0)
            {
                return;
            }

            RefreshView(_frames[(int)e.NewValue - 1]);
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Right)
            {
                if (ScrollFrames.Value < ScrollFrames.Maximum)
                {
                    ScrollFrames.Value++;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                if (ScrollFrames.Value > ScrollFrames.Minimum)
                {
                    ScrollFrames.Value--;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Space)
            {
                TogglePlaying();
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            Load(openFileDialog.FileName);
        }

        private void Load(string fileName)
        {
            _pid = 0;
            _session?.Stop();

            var json = File.ReadAllText(fileName);

            var session = JsonConvert.DeserializeObject<Session>(json)!;

            _frames = session.Frames.ToList();

            GCs.Clear();

            foreach (var gc in session.GCs)
            {
                GCs.Add(gc);
            }

            if (_frames.Count == 0)
            {
                ScrollFrames.Minimum = 0;
                ScrollFrames.Value = 0;
                ScrollFrames.Maximum = 0;
                return;
            }

            ScrollFrames.Minimum = 1;
            ScrollFrames.Maximum = _frames.Count;

            if (ScrollFrames.Value == 1)
            {
                RefreshView(_frames[0]);
            }
            else
            {
                ScrollFrames.Value = 1;
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json"
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            var session = new Session { Frames = _frames, GCs = GCs };

            var json = JsonConvert.SerializeObject(session);

            File.WriteAllText(saveFileDialog.FileName, json);
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            TogglePlaying();
        }

        private void TogglePlaying(bool? newValue = null)
        {
            if (newValue == null)
            {
                newValue = !_playing;
            }

            _playing = newValue.Value;

            if (_playing)
            {
                ButtonPlay.Content = "⏸️";
                ButtonPlay.Foreground = new SolidColorBrush(Colors.DarkBlue);
                _playTimer.Start();
            }
            else
            {
                ButtonPlay.Content = "▶️";
                ButtonPlay.Foreground = new SolidColorBrush(Colors.DarkGreen);
                _playTimer.Stop();
            }
        }

        private void PlayTimer_Tick(object? sender, EventArgs e)
        {
            if (ScrollFrames.Value < ScrollFrames.Maximum)
            {
                ScrollFrames.Value++;
            }
        }
    }
}