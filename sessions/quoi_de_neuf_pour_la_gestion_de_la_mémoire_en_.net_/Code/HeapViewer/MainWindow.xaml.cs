using Microsoft.Diagnostics.Runtime;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HeapViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClrMDSession _session;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnDumpFilenameDoubleClick(object sender, MouseButtonEventArgs e)
        {
            tbDumpFilename.Clear();
            OnOpenDumpFile(sender, null);
        }

        private async void OnOpenDumpFile(object sender, RoutedEventArgs e)
        {
            if (_session != null)
            {
                _session.Dispose();
                _session = null;
            }

            string filename = tbDumpFilename.Text;
            if (string.IsNullOrEmpty(filename))
            {
                filename = SelectDumpFile();
                if (!string.IsNullOrEmpty(filename))
                {
                    tbDumpFilename.Text = filename;
                }
                else
                {
                    tbDumpFilename.Focus();
                    return;
                }
            }

            if (!File.Exists(filename))
            {
                tbDumpFilename.Focus();
                return;
            }

            string file = tbDumpFilename.Text;

            // it is mandatory to open the DebuggingSession in the same thread as the one that is used to create it or simply an STA thread?
            //await RunAsync(() => OpenDumpFile(file));

            OpenDumpFile(file);
        }

        private void OpenDumpFile(string filename)
        {
            if (_session == null)
            {
                _session = new ClrMDSession();
            }

            string errorMessage = null;
            try
            {
                if (!_session.Open(filename))
                {
                    MessageBox.Show(this, $"Impossible to load {filename}");
                    _session.Dispose();
                    _session = null;

                    return;
                }
            }
            catch (FileNotFoundException x)
            {
                errorMessage = "Impossible to load " + x.Message + "\r\nCopy it (with sos.dll) from the machine where the dump was taken\r\ninto the same folder as the .dmp file";
            }
            catch (Exception x)
            {
                errorMessage = "Impossible to load dump file: " + x.Message;
            }

            if (errorMessage != null)
            {
                _session = null;
                MessageBox.Show(this, errorMessage);

                return;
            }

            if (!ComputeHeapSegments())
            {
                MessageBox.Show(this, "Impossible to get segments");
                return;
            }

            UpdateForKind();
            UpdateForHeap();
            UpdateForLayout();
        }

        private void UpdateForKind()
        {
            icPoh.ItemsSource = _segments.Where(seg => seg.Kind == GCSegmentKind.Pinned).ToList();
            icLoh.ItemsSource = _segments.Where(seg => seg.Kind == GCSegmentKind.Large).ToList();
            icGen2.ItemsSource = _segments.Where(seg => seg.Kind == GCSegmentKind.Generation2).ToList();
            icGen1.ItemsSource = _segments.Where(seg => seg.Kind == GCSegmentKind.Generation1).ToList();
            icGen0.ItemsSource = _segments.Where(seg => seg.Kind == GCSegmentKind.Generation0).ToList();

            xpGen0.Header = $"Gen 0  ({_sizeByKind[0].ToString("N0")})";
            xpGen1.Header = $"Gen 1  ({_sizeByKind[1].ToString("N0")})";
            xpGen2.Header = $"Gen 2  ({_sizeByKind[2].ToString("N0")})";
            xpLoh.Header = $"LOH    ({_sizeByKind[3].ToString("N0")})";
            xpPoh.Header = $"POH    ({_sizeByKind[4].ToString("N0")})";
        }

        private void UpdateForHeap()
        {
            icHeaps.ItemsSource = _heaps;
        }

        private void UpdateForLayout()
        {
            lbSegments.ItemsSource = _segments.OrderBy(seg => seg.Start).ToList();
        }

        private bool ComputeHeapSegments()
        {
            if (_session == null) return false;

            for (int kind = 0; kind < MaxKind; kind++)
            {
                _sizeByKind[kind] = 0;
            }

            ulong maxCommittedBytes = 0;
            List<SegmentModel> segments = new List<SegmentModel>();
            List<HeapModel> heaps = new List<HeapModel>();
            foreach (ClrSubHeap subHeap in _session.ManagedHeap.SubHeaps)
            {
                var heapModel = new HeapModel()
                {
                    Index = subHeap.Index,
                    Segments = new List<SegmentModel>()
                };

                //Debug.WriteLine($"Heap #{subHeap.Index}");
                foreach (var segment in subHeap.Segments.OrderBy(s => s.Start))
                {
                    //Debug.WriteLine($"   {segment.Kind}");

                    // skip NonGC heap segments
                    if (segment.Kind == GCSegmentKind.Frozen) continue;

                    SegmentModel segmentModel = new SegmentModel();
                    segmentModel.HeapIndex = subHeap.Index;

                    segmentModel.Start = segment.CommittedMemory.Start;
                    segmentModel.End = segment.CommittedMemory.End;
                    segmentModel.Kind = segment.Kind;
                    segmentModel.FreeBlocks = ComputeFreeBlocks(segment);

                    _sizeByKind[(int)segment.Kind] += segment.CommittedMemory.Length;
                    if (maxCommittedBytes < segment.CommittedMemory.Length)
                    {
                        maxCommittedBytes = segment.CommittedMemory.Length;
                    }

                    segments.Add(segmentModel);
                    heapModel.Segments.Add(segmentModel);
                }

                heaps.Add(heapModel);
            }

            // assign pinned objects in segments from GCHandle
            foreach (var handle in _session.Clr
                .EnumerateHandles()
                .Where(h =>
                    (h.HandleKind == ClrHandleKind.Pinned) ||
                    (h.HandleKind == ClrHandleKind.AsyncPinned)
                    )
                )
            {
                var instance = handle.Object;
                if (instance.Address == 0)
                {
                    // no more there
                    continue;
                }

                var segment = GetSegment(segments, instance.Address);
                if (segment != null)
                {
                    segment.AddPinnedBlock(instance.Address, instance.Size);
                }
            }

            _segments = segments;
            foreach (var segment in _segments)
            {
                segment.ControlWidth = new GridLength(100D * segment.CommittedBytes / maxCommittedBytes, GridUnitType.Star);
                segment.EmptyColumnWidth = new GridLength(100D - (100D * segment.CommittedBytes / maxCommittedBytes), GridUnitType.Star);
            }

            _heaps = heaps;

            return true;
        }

        private SegmentModel GetSegment(List<SegmentModel> segments, ulong address)
        {
            foreach (var segment in segments)
            {
                if ((address <= segment.End) && (address >= segment.Start))
                    return segment;
            }

            return null;
        }

        private IReadOnlyList<FreeBlock> ComputeFreeBlocks(ClrSegment segment)
        {
            List<FreeBlock> freeBlocks = new List<FreeBlock>(1024);
            foreach (var instance in segment.EnumerateObjects())
            {
                if (!instance.IsFree) continue;

                freeBlocks.Add(new FreeBlock(instance.Address, instance.Size));
            }


            return freeBlocks;
        }

        private string SelectDumpFile()
        {
            // select the dump file to open
            OpenFileDialog ofd = new OpenFileDialog()
            {
                DefaultExt = ".dmp",
                Filter = "Dump files (.dmp)|*.dmp",
            };

            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                if (string.IsNullOrEmpty(ofd.FileName))
                    return null;
            }
            else
            {
                return null;
            }

            return ofd.FileName;
        }

        // gen0, 1, 2, LOH, POH
        private const int MaxKind = 5;

        private ulong[] _sizeByKind = new ulong[MaxKind];
        List<SegmentModel> _segments = new List<SegmentModel>();
        List<HeapModel> _heaps = new List<HeapModel>();

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
        }
    }
}