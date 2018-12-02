using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace BlobViz
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private abstract class DataType {
            public String Name;
            public uint NumBytes;

            public abstract byte read(int idx);
            public abstract void preProcessData(byte[] data, int offset, int stride);

            public abstract DataType CreateInstance();
        }

        private class DataFormatReader
        {
            private List<DataType> m_dataTypes;

            public DataFormatReader()
            {
                m_dataTypes = new List<DataType>();
            }

            public void addType(DataType type)
            {
                m_dataTypes.Add(type);

                if (m_dataTypes.Count > 3)
                    throw new Exception("Only 3 Channels supported right now");
            }

            public uint getSize()
            {
                uint size = 0;
                m_dataTypes.ForEach((DataType t) => { size += t.NumBytes; });

                return size;
            }

            public void preProcessData(byte[] bytes)
            {
                int offset = 0;
                int stride = (int)this.getSize();

                foreach(DataType t in m_dataTypes)
                {
                    t.preProcessData(bytes, offset, stride);
                    offset += (int)t.NumBytes;
                }
            }

            public uint read(int idx)
            {
                uint[] colors = new uint[3] { 0x00, 0x00, 0x00 };
                int colorIdx = 0;

                if (idx == 512)
                {
                }
                foreach(DataType t in m_dataTypes)
                {
                    colors[colorIdx++] = t.read(idx);
                }

                uint color = 0xff000000;
                color |= colors[2] << 0; //B
                color |= colors[1] << 8; //G
                color |= colors[0] << 16; //R

                //Console.WriteLine("Hex: {0:X}", color);

                return color;
            }
        }

        private class DataTypeFloat : DataType
        {
            private float min;
            private float max;

            private float[] data;

            public DataTypeFloat(uint numBytes)
            {
                this.Name = "float" + numBytes;
                this.NumBytes = numBytes;
            }

            public override DataType CreateInstance()
            {
                return new DataTypeFloat(this.NumBytes);
            }

            public override void preProcessData(byte[] data, int offset, int stride)
            {
                int numValues = data.Length / stride;
                this.data = new float[numValues];

                byte[] intermediateData = new byte[4];

                this.min = float.MaxValue;
                this.max = -float.MaxValue;

                for (int i=0; i < numValues; ++i)
                {
                    for (int j = 0; j < this.NumBytes; ++j)
                        intermediateData[j] = 0;

                    for (int j = 0; j < this.NumBytes; ++j)
                        intermediateData[j] = data[i * stride + offset + j];

                    float v = System.BitConverter.ToSingle(intermediateData, 0);

                    if (v < this.min)
                        this.min = v;

                    if (v > this.max)
                        this.max = v;

                    this.data[i] = v;
                }
            }

            public override byte read(int idx)
            {
                float value = (this.data[idx] - this.min) / (this.max - this.min);
                return (byte)(value * 255.0);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private DataFormatReader GetDataFormatReader()
        {
            string args = (this.FindName("BufferFormatString") as TextBox).Text;

            string[] descs = args.Trim().Split(';');

            for (int i = 0; i < descs.Length; ++i)
            {
                descs[i] = descs[i].Trim();
            }

            DataFormatReader reader = new DataFormatReader();

            DataType[] type = new DataType[] {
                new DataTypeFloat(1),
                new DataTypeFloat(2),
                new DataTypeFloat(3),
                new DataTypeFloat(4)
            };

            foreach (String desc in descs)
            {
                if (desc.Trim().Equals(""))
                    continue;

                string[] splitted = desc.Trim().Split(':');
                string name = splitted[0];
                int numBytes = int.Parse(splitted[1]);

                string typeName = name + numBytes.ToString();

                foreach (DataType t in type)
                {
                    if (typeName.Equals(t.Name))
                    {
                        reader.addType(t.CreateInstance());
                        break;
                    }
                }
            }

            return reader;
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);

                System.Windows.Controls.TextBox textBoxWidth = this.FindName("ImageWidth") as System.Windows.Controls.TextBox;
                System.Windows.Controls.TextBox textBoxHeight = this.FindName("ImageHeight") as System.Windows.Controls.TextBox;

                uint width = 0;
                uint height = 0;

                uint.TryParse(textBoxWidth.Text, out width);
                uint.TryParse(textBoxHeight.Text, out height);

                if (width == 0 || height == 0)
                {
                    System.Windows.MessageBox.Show("Width or Height invalid");
                    return;
                }

                System.Windows.Controls.Image imageContainer = this.FindName("ImageContainer") as System.Windows.Controls.Image;
                imageContainer.Width = width;
                imageContainer.Height = height;

                WriteableBitmap bitmap = new WriteableBitmap((int)width, (int)height, 96, 96, PixelFormats.Bgra32, null);

                DataFormatReader reader = this.GetDataFormatReader();
                reader.preProcessData(bytes);

                uint[] data = new uint[width * height];


                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        int idx = y * (int)width + x;
                        data[idx] = reader.read(idx);
                    }
                }

                bitmap.WritePixels(new Int32Rect(0, 0, (int)width, (int)height), data, (int)width * 4, 0);

                imageContainer.Source = bitmap;
            }
        }
    }
}
