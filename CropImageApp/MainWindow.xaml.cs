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
using System.Windows.Shapes;
using CropImage;

namespace CropImageApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void crop_Click(object sender, RoutedEventArgs e)
        {
            cropImage.Crop();
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            //dialog.DefaultExt = ".txt";
            //dialog.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            var result = dialog.ShowDialog();
 
            if (result == true)
            {
                cropImage.SetImage(dialog.FileName);
            }
        }

        private void cropImage_SelectedAreaChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var crop = sender as CropImage.CropImage;
            var selectedArea = crop.SelectedArea as BitmapSource;

            if (selectedArea == null)
            {
                return;
            }

            preview.Source = selectedArea;

            top.Text = crop.Top.ToString();
            left.Text = crop.Left.ToString();
            right.Text = crop.Right.ToString();
            bottom.Text = crop.Bottom.ToString();
        }
    }
}
