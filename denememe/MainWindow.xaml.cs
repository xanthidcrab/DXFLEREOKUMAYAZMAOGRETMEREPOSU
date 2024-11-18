using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using netDxf;
using netDxf.Entities;
using Point = System.Windows.Point;
namespace denememe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Path = AppDomain.CurrentDomain.BaseDirectory + "DXF\\";
        private TranslateTransform translateTransform;
        private Color _lineColor = Color.FromArgb(0xFF, 0x66, 0x66, 0x66);
        private Color _backgroundColor = Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
        private List<System.Windows.Shapes.Line> _gridLines = new List<System.Windows.Shapes.Line>();
        public string FilePath { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            //LoadFiles();
            translateTransform = new TranslateTransform();
            this.RenderTransform = new TransformGroup();
         

           
        }
        private System.Windows.Point _initialMousePosition;
        private readonly MatrixTransform _transform = new MatrixTransform();
        private UIElement _selectedElement;
        private Vector _draggingDelta;
        private bool _dragging;
        public float Zoomfactor { get; set; } = 1.1f;
        private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float scaleFactor = Zoomfactor;
            if (e.Delta < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }

            System.Windows.Point mousePostion = e.GetPosition(this);

            Matrix scaleMatrix = _transform.Matrix;
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
            _transform.Matrix = scaleMatrix;

            foreach (UIElement child in Panner.Children)
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);

                child.RenderTransform = _transform;
            }
        }
        private void PanAndZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _initialMousePosition = _transform.Inverse.Transform(e.GetPosition(this));
            }

            if (e.ChangedButton == MouseButton.Middle)
            {
                if (Panner.Children.Contains((UIElement)e.Source))
                {
                    _selectedElement = (UIElement)e.Source;
                    System.Windows.Point mousePosition = Mouse.GetPosition(this);
                    double x = Canvas.GetLeft(_selectedElement);
                    double y = Canvas.GetTop(_selectedElement);
                    System.Windows.Point elementPosition = new System.Windows.Point(x, y);
                    _draggingDelta = elementPosition - mousePosition;
                }
                _dragging = true;
            }
        }
        private void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                System.Windows.Point mousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Vector delta = System.Windows.Point.Subtract(mousePosition, _initialMousePosition);
                var translate = new TranslateTransform(delta.X, delta.Y);
                _transform.Matrix = translate.Value * _transform.Matrix;

                foreach (UIElement child in Panner.Children)
                {
                    child.RenderTransform = _transform;
                }
            }

            if (_dragging && e.MiddleButton == MouseButtonState.Pressed)
            {
                double x = Mouse.GetPosition(Panner).X;
                double y = Mouse.GetPosition(Panner).Y;

                if (_selectedElement != null)
                {
                    Canvas.SetLeft(_selectedElement, x + _draggingDelta.X);
                    Canvas.SetTop(_selectedElement, y + _draggingDelta.Y);
                }
            }
            var point = e.GetPosition(WorkingArea);
            Point pos = e.GetPosition(Panner);

            Xeksen.Text = "X:" + pos.X.ToString();
            Yeksen.Text = "Y:" + pos.Y.ToString();
        }
        private void LoadFiles()
        {
            if (Panner.Children.Count != 0)
            {
                Panner.Children.Clear();
            }
            DxfDocument dxfDocument = DxfDocument.Load(FilePath);
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Stroke = new SolidColorBrush(Colors.Black);
            
            path.StrokeThickness = 0.2;
           
            PathGeometry pathGeometry = new PathGeometry();
            foreach (var child in dxfDocument.Entities.All) 
            {
                switch (child.Type) 
                { 
                    case EntityType.Line:
                        netDxf.Entities.Line line = (netDxf.Entities.Line)child;
                        PathFigure pathFigure = new PathFigure();
                        pathFigure.StartPoint = new Point(line.StartPoint.X, line.StartPoint.Y);
                        LineSegment lineSegment = new LineSegment() { Point = new Point(line.EndPoint.X, line.EndPoint.Y) };
                        pathFigure.Segments.Add(lineSegment);
                        pathGeometry.Figures.Add(pathFigure);
                        break;
                    case EntityType.Arc:
                        Arc arc = (Arc)child;
                        PathFigure arcSegment = CreateArc(arc.Center.X, arc.Center.Y, arc.Radius, arc.StartAngle, arc.EndAngle);
                        pathGeometry.Figures.Add(arcSegment);
                        break;
                }
                if (child.Type != EntityType.Arc && child.Type != EntityType.Line) 
                {
                    Debug.WriteLine(child.Type);
                }
            
            }//
            //foreach (netDxf.Entities.Line item in dxfDocument.Entities.Lines)
            //{
            //    PathFigure pathFigure = new PathFigure();
            //    pathFigure.StartPoint = new Point(item.StartPoint.X, item.StartPoint.Y);
            //    LineSegment lineSegment = new LineSegment() { Point = new Point(item.EndPoint.X, item.EndPoint.Y) };
            //    pathFigure.Segments.Add(lineSegment);
            //    pathGeometry.Figures.Add(pathFigure);
            //}
            //if (dxfDocument.Entities.Arcs.Count() != 0)
            //{
            //    foreach (netDxf.Entities.Arc item in dxfDocument.Entities.Arcs)
            //    {
            //       PathFigure arcSegment = CreateArc(item.Center.X,item.Center.Y, item.Radius, item.StartAngle,item.EndAngle);
            //    pathGeometry.Figures.Add(arcSegment);

            //    }
            //}
            path.Data = pathGeometry;
            Canvas.SetLeft(path, 0);    
            Canvas.SetTop(path, 0);    
            Panner.Children.Add(path);
          
        }
        public PathFigure CreateArc(double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            // Başlangıç ve bitiş açılarını radyana çevir.
            double startAngleRad = startAngle * Math.PI / 180;
            double endAngleRad = endAngle * Math.PI / 180;

            // Yayın başlangıç ve bitiş noktalarını hesapla.
            System.Windows.Point startPoint = new System.Windows.Point(
                centerX + radius * Math.Cos(startAngleRad),
                centerY + radius * Math.Sin(startAngleRad)
            );

            System.Windows.Point endPoint = new System.Windows.Point(
                centerX + radius * Math.Cos(endAngleRad),
                centerY + radius * Math.Sin(endAngleRad)
            );

            // ArcSegment oluştur.
            ArcSegment arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius), // Elipsin yarıçapları
                SweepDirection = SweepDirection.Clockwise, // Saat yönünde mi, ters mi?
                IsLargeArc = false // Büyük bir yay mı?
            };

            // PathFigure ile başlat.
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = startPoint,
                Segments = new PathSegmentCollection { arcSegment }
            };
            return pathFigure;  
        }

            private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DXF Files (*.dxf)|*.dxf";
            openFileDialog.Title = "DXF SEÇ";
            openFileDialog.ShowDialog();
            FilePath = openFileDialog.FileName;
            LoadFiles();
        }

        private void Path_MouseMove(object sender, MouseEventArgs e)
        {
          
        }
    }
}
