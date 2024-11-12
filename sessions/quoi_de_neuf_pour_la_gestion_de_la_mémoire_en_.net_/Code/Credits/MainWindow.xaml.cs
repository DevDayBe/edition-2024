using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Credits
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _lines;
        private int _index;

        private DispatcherTimer _timer;

        public MainWindow()
        {
            _lines = File.ReadAllLines("gc.cpp");

            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Tick += _timer_Tick;
            _timer_Tick(null, default);
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            var line = _lines[_index++]
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");

            var parserContext = new ParserContext();
            parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");

            var str = @"<ModelVisual3D>
                <ModelVisual3D.Content>
                    <GeometryModel3D>
                        <GeometryModel3D.Geometry>
                            <MeshGeometry3D x:Name=""meshMain""
                                Positions=""0.2 -5 0   0.8 -5 0   0.2 1 0   0.8 1 0""
                                TriangleIndices=""0 1 3  0 3 2""
                                TextureCoordinates=""0 1  1 1  0 0  1 0"">
                            </MeshGeometry3D>
                        </GeometryModel3D.Geometry>
                        <GeometryModel3D.Material>
                            <DiffuseMaterial x:Name=""matDiffuseMain"" >
                                <DiffuseMaterial.Brush>
                                    <VisualBrush >
                                        <VisualBrush.Visual>
                                            <Grid Width=""200"" Height=""1000"" Background=""Black"">
                                                <Border BorderBrush=""Black"">
                                                    <TextBlock x:Name=""TextCredits""  Background=""Black""
                                                             TextWrapping=""Wrap""
                                                             Foreground=""#FFFFDA00"" 
                                                             FontFamily=""Franklin Gothic"" 
                                                             FontWeight=""Bold""
                                                             FontSize=""16""
                                                             TextAlignment=""Justify""
                                                             LineHeight=""17""
                                                             LineStackingStrategy=""BlockLineHeight""
                                                             >
                                                        {text}
                                                    </TextBlock>
                                                </Border>
                                            </Grid>
                                        </VisualBrush.Visual>
                                    </VisualBrush>
                                </DiffuseMaterial.Brush>
                            </DiffuseMaterial>
                        </GeometryModel3D.Material>
                    </GeometryModel3D>
                </ModelVisual3D.Content>
                <ModelVisual3D.Transform>
                    <TranslateTransform3D x:Name=""TextPos"" OffsetY=""-1.5"" OffsetZ=""{offsetZ}""/>
                </ModelVisual3D.Transform>
            </ModelVisual3D>
            ";

            var obj = XamlReader.Parse(str.Replace("{text}", line).Replace("{offsetZ}", (_index / 100000.0).ToString()), parserContext);

            var model = (ModelVisual3D)obj;

            viewport3D1.Children.Add(model);

            var storyboard = new Storyboard();

            var animation = new DoubleAnimation();
            animation.From = -1.5;
            animation.To = 5;
            animation.Duration = new Duration(new TimeSpan(0, 1, 30));

            Storyboard.SetTarget(animation, model);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Transform.OffsetY"));

            storyboard.Completed += (_, _) =>
            {
                viewport3D1.Children.Remove(model);
            };

            storyboard.Children.Add(animation);

            storyboard.Begin();

            _timer.Stop();

            var size = MeasureString(line);

            _timer.Interval = TimeSpan.FromMilliseconds(size.Height * 85);
            _timer.Start();

        }

        private Size MeasureString(string candidate)
        {
            var textBlock = new TextBlock
            {
                Text = candidate,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Franklin Gothic"),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                TextAlignment = TextAlignment.Justify,
                LineHeight = 17,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };

            textBlock.Measure(new Size(200, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(textBlock.DesiredSize));

            return new Size(textBlock.ActualWidth, textBlock.ActualHeight);
        }
    }
}