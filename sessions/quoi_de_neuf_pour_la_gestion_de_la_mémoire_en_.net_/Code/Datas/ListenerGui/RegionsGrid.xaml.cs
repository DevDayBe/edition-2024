using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ListenerGui.Models;
using Microsoft.Diagnostics.Runtime;

namespace ListenerGui
{
    public partial class RegionsGrid
    {
        private IReadOnlyList<Segment>? _segments;
        private double _regionSize = 20;

        public RegionsGrid()
        {
            InitializeComponent();
        }

        public void SetRegions(IReadOnlyList<Segment> segments)
        {
            _segments = segments;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_segments == null)
            {
                return;
            }

            ulong lastRegionEnd = 0;

            var line = 0;
            var column = 0;
            var rectanglesPerLine = Math.Floor(ActualWidth / _regionSize);

            void DrawRectangle(Color color)
            {
                // Can we fit another rectangle on this line?
                if (column >= rectanglesPerLine)
                {
                    line++;
                    column = 0;
                }

                drawingContext.DrawRectangle(
                    new SolidColorBrush(color),
                    // new Pen(new SolidColorBrush(color), 0.0),
                    new Pen(new SolidColorBrush(Colors.White), 1.0),
                    new Rect(new Point(column * _regionSize, line * _regionSize), new Size(_regionSize, _regionSize)));

                column++;
            }

            foreach (var segment in _segments.OrderBy(s => s.Start))
            {
                if (segment.Flags.HasFlag((ClrSegmentFlags)32))
                {
                    Debug.WriteLine($"Skipping decommitted region - {segment.Address:x2}");
                    //continue;
                }

                var generation = segment.Generation;

                if (generation == Generation.Frozen)
                {
                    continue;
                }

                var start = segment.Start;
                var end = segment.ReservedMemory.End;

                if (lastRegionEnd != 0)
                {
                    var diff = start - lastRegionEnd;
                    var diffInMB = ToUnits(diff);

                    for (int i = 0; i < diffInMB; i++)
                    {
                        DrawRectangle(Colors.LightGray);
                    }
                }

                var size = end - start;
                var sizeInMB = ToUnits(size);

                if (segment.Kind == GCSegmentKind.Ephemeral)
                {
                    var gen1Size = ToUnits(segment.Generation1.Length);
                    var gen2Size = ToUnits(segment.Generation2.Length);
                    var gen0Size = ToUnits(segment.ReservedMemory.End - segment.Start) - gen1Size - gen2Size;

                    for (int i = 0; i < gen2Size; i++)
                    {
                        DrawRectangle(Region.GetColor(Generation.Generation2));
                    }

                    for (int i = 0; i < gen1Size; i++)
                    {
                        DrawRectangle(Region.GetColor(Generation.Generation1));
                    }

                    for (int i = 0; i < gen0Size; i++)
                    {
                        DrawRectangle(Region.GetColor(Generation.Generation0));
                    }
                }
                else
                {
                    var color = Region.GetColor(generation);

                    if (segment.Flags.HasFlag((ClrSegmentFlags)32))
                    {
                        color = Colors.Red;
                    }

                    for (int i = 0; i < sizeInMB; i++)
                    {
                        DrawRectangle(color);
                    }
                }

                lastRegionEnd = end;
            }
        }

        private static double ToUnits(ulong length)
        {
            return Math.Floor(length / (1024.0 * 1024));
        }
    }
}
