using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ImageResizer;
using System.Diagnostics;
using System.IO;

namespace CropImage
{
    /// <summary>
    /// Interaction logic for CropImage.xaml
    /// </summary>
    public partial class CropImage : UserControl
    {
        #region PROPERTIES/FIELDS
        private bool isResizing;
        private bool isDragging;
        private bool isAreaSelected;

        private double oldWidth;
        private double oldHeight;

        private ResizeDirection direction;

        private Point dragPoint;
        private Point startPoint;

        public double Top { get; set; }

        public double Left { get; set; }

        public double Right { get; set; }

        public double Bottom { get; set; }

        //public Image Image { get; set; }

        /// <summary>Identifies the SelectedArea dependency property.</summary>
        public static DependencyProperty SelectedAreaProperty;

        /// <summary>The area selected to be cropped. This is a dependency property.</summary>
        public BitmapSource SelectedArea
        {
            get { return GetValue(SelectedAreaProperty) as BitmapSource; }
            set { SetValue(SelectedAreaProperty, value); }
        }
        #endregion

        #region EVENTS
        /// <summary>Identifies the SelectedAreaChanged routed event.</summary>
        public static readonly RoutedEvent SelectedAreaChangedEvent;

        /// <summary>Occurs when the selected area is modified.</summary>
        public event RoutedPropertyChangedEventHandler<object> SelectedAreaChanged
        {
            add { AddHandler(SelectedAreaChangedEvent, value); }
            remove { RemoveHandler(SelectedAreaChangedEvent, value); }
        }

        /// <summary>Handles the SelectedAreaChanged logic.</summary>
        protected static void OnSelectedAreaChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RoutedPropertyChangedEventArgs<object> args = new RoutedPropertyChangedEventArgs<object>(e.OldValue, e.NewValue);
            args.RoutedEvent = CropImage.SelectedAreaChangedEvent;
            var cropImage = (CropImage)sender;
            cropImage.RaiseEvent(args);
            //cropImage.SetDisplayText();
        }
        #endregion

        #region CONSTRUCTORS
        /// <summary>Initializes a new instance of the CropImage class.</summary>
        public CropImage()
        {
            InitializeComponent();

            isResizing = false;
            isDragging = false;
            isAreaSelected = false;
        }

        /// <summary>Registers routed events and dependency properties.</summary>
        static CropImage()
        {
            SelectedAreaChangedEvent = EventManager.RegisterRoutedEvent(
                "SelectedAreaChanged", RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<object>), typeof(CropImage));

            SelectedAreaProperty = DependencyProperty.Register(
                "SelectedArea", typeof(BitmapSource), typeof(CropImage),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSelectedAreaChanged)));
        }
        #endregion

        #region METHODS
        public void Crop()
        {
            var path = GetPath(image);

            if (path == null)
            {
                throw new ApplicationException("The application could not find the path of the image.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The image could not be found.");
            }

            //var left = Canvas.GetLeft(selection) + selection.BorderThickness.Left;
            //var top = Canvas.GetTop(selection) + selection.BorderThickness.Top;
            //var right = left + (selection.ActualWidth - BorderThickness.Right );
            //var bottom = top + (selection.ActualHeight - BorderThickness.Bottom);

            Debug.WriteLine(string.Format("crop={0},{1},{2},{3}", Left, Top, Right, Bottom));

            var settings = new ResizeSettings(string.Format(
                //"crop=0,0,100,100"))
                "crop={0},{1},{2},{3}", 
                Left, 
                Top,
                Right,
                Bottom))
            {
                //CropTopLeft = new System.Drawing.PointF((float)Canvas.GetLeft(selection), (float)Canvas.GetTop(selection)),
                //CropBottomRight = new System.Drawing.PointF(-(float)Canvas.GetRight(selection), -(float)Canvas.GetBottom(selection))
                //CropXUnits = croppedArea.ActualWidth,
                //CropYUnits = croppedArea.ActualHeight
            };

            var dir = Path.GetDirectoryName(path);
            var newPath = Path.Combine(dir, string.Concat("cropped_", DateTime.Now.Ticks.ToString(), Path.GetExtension(path)));

            ImageBuilder.Current.Build(path, newPath, settings);
            System.Diagnostics.Process.Start(newPath);
        }

        public void SetImage(string path)
        {
            var source = new BitmapImage();

            source.BeginInit();
            source.UriSource = new Uri(path, UriKind.Absolute);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();

            image.Source = source;
            overlay.Source = source;
        }

        protected string GetPath(Image image)
        {
            var source = image.Source as BitmapImage;

            if (source == null)
            {
                return null;
            }

            return source.UriSource.AbsolutePath;
        }

        protected void Update(double x, double y, double width, double height)
        {
            Top = y;
            Left = x;
            Right = x + width;
            Bottom = y + height;

            DisplayOverlay(x, y, width, height);
        }

        protected void DisplayOverlay(double x, double y, double width, double height)
        {
            image.Opacity = 0.6;

            var rectangle = overlay.Clip as RectangleGeometry;
            var rect = rectangle.Rect;

            rect.X = x;
            rect.Y = y;
            rect.Width = width;
            rect.Height = height;

            rectangle.Rect = rect;
            overlay.Visibility = System.Windows.Visibility.Visible;

            var render = new RenderTargetBitmap(
                rect.Width == 0 ? 1 : (int)(rect.Width),
                rect.Height == 0 ? 1 : (int)(rect.Height),
                96,
                96,
                PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();

            using (var context = drawingVisual.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(overlay);
                context.DrawRectangle(brush, null, new Rect(new Point(), rect.Size));
            }

            render.Render(drawingVisual);

            SelectedArea = render;
        }

        protected void SelectArea(Point position)
        {
            var x = Math.Min(position.X, startPoint.X);
            var y = Math.Min(position.Y, startPoint.Y);

            var w = Math.Max(position.X, startPoint.X) - x;
            var h = Math.Max(position.Y, startPoint.Y) - y;

            selection.Width = w;
            selection.Height = h;

            Canvas.SetLeft(selection, x);
            Canvas.SetTop(selection, y);

            Update(x, y, w, h);
        }

        protected void Drag(Point position)
        {
            Debug.WriteLine("{0} {1}", position, startPoint);
            isDragging = true;

            var x = startPoint.X - (dragPoint.X - position.X);
            var y = startPoint.Y - (dragPoint.Y - position.Y);
            
            Canvas.SetLeft(selection, x);
            Canvas.SetTop(selection, y);

            Update(x, y, selection.Width, selection.Height);
        }

        protected void Resize(Point position)
        {
            isResizing = true;

            var x = Math.Min(position.X, startPoint.X);
            var y = Math.Min(position.Y, startPoint.Y);

            var w = Math.Max(position.X, startPoint.X) - x;
            var h = Math.Max(position.Y, startPoint.Y) - y;

            if (oldWidth == 0 && oldHeight == 0)
            {
                oldWidth = selection.Width;
                oldHeight = selection.Height;
            }

            switch (direction)
            {
                case ResizeDirection.Top:
                    Canvas.SetTop(selection, y);
                    selection.Height = h + oldHeight;
                    break;

                case ResizeDirection.Left:
                    //Debug.WriteLine("{0} {1} {2} {3}", selection.Width, position.X, startPoint.X, selection.Width + (startPoint.X - position.X));
                    Canvas.SetLeft(selection, x);
                    selection.Width = w + oldWidth;
                    break;

                case ResizeDirection.Right:
                    selection.Width = selection.Width + (position.X - selection.Width - startPoint.X);
                    break;

                case ResizeDirection.Bottom:
                    selection.Height = selection.Height + (position.Y - selection.Height - startPoint.Y);
                    break;

                case ResizeDirection.TopLeft:
                    Canvas.SetTop(selection, y);
                    Canvas.SetLeft(selection, x);
                    selection.Height = h + oldHeight;
                    selection.Width = w + oldWidth;
                    break;

                case ResizeDirection.TopRight:
                    Canvas.SetTop(selection, y);
                    selection.Height = h + oldHeight;
                    selection.Width = selection.Width + (position.X - selection.Width - startPoint.X);
                    break;

                case ResizeDirection.BottomLeft:
                    Canvas.SetLeft(selection, x);
                    selection.Width = w + oldWidth;
                    selection.Height = selection.Height + (position.Y - selection.Height - startPoint.Y);
                    break;

                case ResizeDirection.BottomRight:
                    selection.Width = selection.Width + (position.X - selection.Width - startPoint.X);
                    selection.Height = selection.Height + (position.Y - selection.Height - startPoint.Y);
                    break;
            }

            Update(x, y, selection.Width, selection.Height);
        }

        protected void PrepareForResizing(Point position)
        {
            var width = selection.ActualWidth;
            var height = selection.ActualHeight;
            var thickness = selection.BorderThickness.Left + 1;

            //top left corner
            if (position.X >= 0 && position.X <= thickness && position.Y >= 0 && position.Y <= thickness)
            {
                selection.Cursor = Cursors.SizeNWSE;
                direction = ResizeDirection.TopLeft;
            }
            //top right corner
            else if (position.X >= width - thickness && position.Y >= 0 && position.Y <= thickness)
            {
                selection.Cursor = Cursors.SizeNESW;
                direction = ResizeDirection.TopRight;
            }
            //bottom left corner
            else if (position.X >= 0 && position.X <= thickness && position.Y >= height - thickness)
            {
                selection.Cursor = Cursors.SizeNESW;
                direction = ResizeDirection.BottomLeft;
            }
            //bottom right corner
            else if (position.X >= width - thickness && position.Y >= height - thickness)
            {
                selection.Cursor = Cursors.SizeNWSE;
                direction = ResizeDirection.BottomRight;
            }
            //top 
            else if (position.Y <= thickness)
            {
                selection.Cursor = Cursors.SizeNS;
                direction = ResizeDirection.Top;
            }
            //bottom 
            else if (position.Y >= height - thickness)
            {
                selection.Cursor = Cursors.SizeNS;
                direction = ResizeDirection.Bottom;
            }
            //left 
            else if (position.X <= thickness)
            {
                selection.Cursor = Cursors.SizeWE;
                direction = ResizeDirection.Left;
            }
            //right 
            else if (position.X >= width - thickness)
            {
                selection.Cursor = Cursors.SizeWE;
                direction = ResizeDirection.Right;
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            //if (isResizing)
            //{
            //    return;
            //}

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //if the mouse button is pressed and there is an area selected, it means we're resizing
                if (isAreaSelected)
                {
                    var position = e.GetPosition(canvas);

                    Resize(position);
                }
                //else we're selecting an area/drawing the rectangle
                else
                {
                    var position = e.GetPosition(canvas);

                    SelectArea(position);
                }
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isResizing)
            {
                return;
            }

            startPoint = e.GetPosition(canvas);

            selection.Width = 0;
            selection.Height = 0;

            oldWidth = 0;
            oldHeight = 0;

            Canvas.SetLeft(selection, startPoint.X);
            Canvas.SetTop(selection, startPoint.Y);
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selection.Width == 0 && selection.Height == 0)
            {
                isAreaSelected = false;
                overlay.Visibility = System.Windows.Visibility.Hidden;
                image.Opacity = 1;
            }
            else
            {
                isAreaSelected = true;
            }

            if (isDragging)
            {
                startPoint = new Point(Canvas.GetLeft(selection), Canvas.GetTop(selection));
                isDragging = false;
            }
        }

        private void croppedArea_MouseEnter(object sender, MouseEventArgs e)
        {
            var canvas = sender as Canvas;
            canvas.Cursor = Cursors.SizeAll;

            //isMouseOverSelectedArea = true;
        }

        private void croppedArea_MouseLeave(object sender, MouseEventArgs e)
        {
            //If the selected area is being dragged, this event will be fired as a false positive, so we return.
            if (isDragging)
            {
                return;
            }

            var canvas = sender as Canvas;
            canvas.Background = Brushes.Transparent;
        }

        private void croppedArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Drag(e.GetPosition(canvas));
            }
            e.Handled = true;
        }

        private void croppedArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragPoint = e.GetPosition(canvas);
            e.Handled = true;
        }

        private void selection_MouseEnter(object sender, MouseEventArgs e)
        {
            PrepareForResizing(e.GetPosition(selection));
        }

        private void selection_MouseMove(object sender, MouseEventArgs e)
        {
            //e.Handled = true;
            var position = e.GetPosition(selection);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Resize(position);
            }
            else
            {
                PrepareForResizing(position);
            }
        }

        private void selection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Resize(e.GetPosition(selection));
            //isResizing = true;
            e.Handled = true;
        }

        private void selection_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
        }

        private void selection_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                isResizing = false;
            }
        }

        private void canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //selection.ismou
        }

        private void selection_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (selection.IsMouseDirectlyOver == false)
            {
                isResizing = false;
            }
        }
        #endregion
    }
}
