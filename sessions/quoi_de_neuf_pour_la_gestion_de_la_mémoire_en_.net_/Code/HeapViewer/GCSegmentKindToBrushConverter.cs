using Microsoft.Diagnostics.Runtime;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HeapViewer
{
    [ValueConversion(typeof(GCSegmentKind), typeof(Brush))]
    public class GCSegmentKindToBrushConverter : IValueConverter
    {
        public GCSegmentKindToBrushConverter()
        {
            Gen0Brush = _whiteBrush;
            Gen1Brush = _whiteBrush;
            Gen2Brush = _whiteBrush;
            POHBrush = _whiteBrush;
            LOHBrush = _whiteBrush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GCSegmentKind kind = (GCSegmentKind)value;

            switch (kind)
            {
                case GCSegmentKind.Pinned:
                    return POHBrush;

                case GCSegmentKind.Large:
                    return LOHBrush;

                case GCSegmentKind.Generation0:
                    return Gen0Brush;

                case GCSegmentKind.Generation1:
                    return Gen1Brush;

                case GCSegmentKind.Generation2:
                    return Gen2Brush;

            }

            return _whiteBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


        public Brush POHBrush { get; set; }

        public Brush LOHBrush { get; set; }

        public Brush Gen0Brush { get; set; }

        public Brush Gen1Brush { get; set; }

        public Brush Gen2Brush { get; set; }

        private static Brush _whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    }
}
