using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = System.Drawing.Size;

namespace Sizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _newHeightValue = 100;

        private int _newWidthValue = 100;

        public MainWindow()
        {
            InitializeComponent();

            ResolutionComboBox.ItemsSource = new List<StandardSize>
            {
                 new StandardSize("Giant Banner", 300, 600),
                 new StandardSize("Top Website Banner", 728, 90),
                 new StandardSize("Central Banner", 555, 90),
                 new StandardSize("Side Banner", 300, 300),
                 new StandardSize("Top Newsletter Banner", 468, 120)
            };

            var modes = new List<ImageProcessor.Imaging.ResizeMode>();
            foreach (ImageProcessor.Imaging.ResizeMode value in Enum.GetValues(typeof(ImageProcessor.Imaging.ResizeMode)))
            {
                modes.Add(value);
            }
            ScaleComboBox.ItemsSource = modes;
            ScaleComboBox.SelectedItem = ImageProcessor.Imaging.ResizeMode.Stretch;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ConvertSample
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                {
                    return null;
                }

                if (!Dragging)
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Temp"));
                    var temp = Path.Combine(Environment.CurrentDirectory, "Temp", DateTime.Now.Ticks + ".png");
                    Resize(temp, 60);
                    return new BitmapImage(new Uri(temp));
                }
                return null;
            }
        }

        public bool Dragging { get; set; }
        public string FileName { get; set; }

        public string HeightRatio
        {
            get
            {
                return Math.Round((NewHeightValue / (double)OriginalHeight) * 100.0, 2) + "%";
            }
        }

        public int NewHeightValue
        {
            get
            {
                return _newHeightValue;
            }
            set
            {
                _newHeightValue = value;
                Changed(nameof(NewHeightValue));
                Changed(nameof(HeightRatio));

                if (ForceAspectCheckbox.IsChecked == true)
                {
                    _newWidthValue = OriginalWidth * _newHeightValue / OriginalHeight;
                    Changed(nameof(NewWidthValue));
                    Changed(nameof(WidthRatio));
                }

                Changed(nameof(ConvertSample));
            }
        }

        public int NewWidthValue
        {
            get
            {
                return _newWidthValue;
            }
            set
            {
                _newWidthValue = value;
                Changed(nameof(NewWidthValue));
                Changed(nameof(WidthRatio));

                if (ForceAspectCheckbox.IsChecked == true)
                {
                    _newHeightValue = OriginalHeight * _newWidthValue / OriginalWidth;
                    Changed(nameof(NewHeightValue));
                    Changed(nameof(HeightRatio));
                }

                Changed(nameof(ConvertSample));
            }
        }

        private ImageProcessor.Imaging.ResizeMode _resizeMode = ImageProcessor.Imaging.ResizeMode.Stretch;

        public ImageProcessor.Imaging.ResizeMode ResizeMode
        {
            get
            {
                return _resizeMode;
            }
            set
            {
                _resizeMode = value;
                Changed(nameof(ConvertSample));
            }
        }

        public int OriginalHeight { get; set; } = 100;
        public int OriginalWidth { get; set; } = 100;

        public string WidthRatio
        {
            get
            {
                return Math.Round((NewWidthValue / (double)OriginalWidth) * 100.0, 2) + "%";
            }
        }

        public void Changed(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Dragging = false;
            Changed(nameof(ConvertSample));
        }

        private void DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            Dragging = true;
        }

        private void ForceAspectCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            NewHeightValue = _newHeightValue;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog();

            if (d.ShowDialog() == true)
            {
                FileName = d.FileName;
                var image = new BitmapImage(new Uri(FileName));
                OriginalImage.Source = image;

                OriginalHeight = image.PixelHeight;
                OriginalWidth = image.PixelWidth;

                WidthSlider.Maximum = OriginalWidth * 2;
                HeightSlider.Maximum = OriginalHeight * 2;

                NewWidthValue = OriginalWidth;
                NewHeightValue = OriginalHeight;

                ControlPanel.IsEnabled = true;
            }
        }

        private void Resize(string outFile, int quality = 100)
        {
            var photoBytes = File.ReadAllBytes(FileName);
            var format = new PngFormat { Quality = quality };
            var size = new Size(NewWidthValue, NewHeightValue);
            using (var inStream = new MemoryStream(photoBytes))
            {
                using (var outStream = new FileStream(outFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                    using (var imageFactory = new ImageFactory(preserveExifData: true))
                    {
                        // Load, resize, set the format and quality and save an image.
                        imageFactory.Load(inStream)
                                    .Resize(new ResizeLayer(size, ResizeMode))
                                    .Format(format)
                                    .Save(outStream);
                    }
                }
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            var outFile = Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileNameWithoutExtension(FileName) + ".scaled.png");
            Resize(outFile);

            Process.Start("explorer.exe", $"/select,\"{outFile}\"");
        }

        private void ResolutionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var size = (StandardSize)ResolutionComboBox.SelectedItem;
            ForceAspectCheckbox.IsChecked = false;
            NewWidthValue = size.Width;
            NewHeightValue = size.Height;
        }

        private void ScaleComboBox_ScaleModeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ResizeMode = (ImageProcessor.Imaging.ResizeMode)ScaleComboBox.SelectedItem;
        }
    }

    public class StandardSize
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public StandardSize(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return $"{Name} ({Width}x{Height})";
        }
    }
}