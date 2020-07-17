using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Windows.Forms;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using GMap.NET.CacheProviders;
using System.IO;
using GMap.NET.MapProviders;

namespace turksatdeneme_6
{
    public partial class Form1 : Form
    {
        private static List<Telemetri> dataset;
        private static string _data;
        private static string _oldData;
        private FilterInfoCollection webcam; //webcam isminde tanımladığımız değişken bilgisayara kaç kamera bağlıysa onları tutan bir dizi.
        private VideoCaptureDevice cam; //cam ise bizim kullanacağımız aygıt.
        public bool IsClosed { get; private set; }

        public Form1()
        {
            InitializeComponent();

            //com3 usb bağlıntısını kontrol ediyoruz ve bağlantının açılıp açılmadığını denetliyoruz
            while (true)
                try
                {
                    if (serialPort1.IsOpen == false)
                    {
                        serialPort1.Open();
                        break;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

        }

        void Cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = (Bitmap)eventArgs.Frame.Clone(); //kısaca bu eventta kameradan alınan görüntüyü picturebox a atıyoruz.
            pcbVideo.Image = bmp;
        }
        private void Cek_Click(object sender, EventArgs e)
        {
            SaveFileDialog swf = new SaveFileDialog();
            swf.Filter = "(*.jpg)|*.jpg|Bitma*p(*.bmp)|*.bmp";
            DialogResult dialog = swf.ShowDialog();  //resmi çekiyoruz ve aşağıda da kaydediyoruz.

            if (dialog == DialogResult.OK)
            {
                pcbVideo.Image.Save(swf.FileName);
            }

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            var ports = SerialPort.GetPortNames();
            cmbPort.DataSource = ports;

            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        string data = serialPort1.ReadLine();
                        _data = data;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Port okuma işlemi sonlandırıldı.");
                    }

                }

            }).Start();
            webcam = new
            FilterInfoCollection(FilterCategory.VideoInputDevice); //webcam dizisine mevcut kameraları dolduruyoruz.
            foreach (FilterInfo item in webcam)
            {
                comboBox1.Items.Add(item.Name); //kameraları combobox a dolduruyoruz.
            }
            comboBox1.SelectedIndex = 0;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;


        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            cam = new
            VideoCaptureDevice(webcam[comboBox1.SelectedIndex].MonikerString); //başlaya basıldığıdnda yukarda tanımladığımız cam değişkenine comboboxta seçilmş olan kamerayı atıyoruz.
            cam.NewFrame += new NewFrameEventHandler(Cam_NewFrame);
            cam.Start(); //kamerayı başlatıyoruz.
        }

        private void btnStop_Click(object sender, EventArgs e)
        {

            if (cam.IsRunning)
            {
                cam.Stop(); // kamerayı durduruyoruz.
            }
        }

        

        private void tmrRefresh_Tick(object sender, EventArgs e)// timer ile gelen verileri saniyede bir yenilemeyi sağlayan fonksiyonumuz.
        {
            if (_data != _oldData)
            {
                _oldData = _data;
                string[] pots = _data.Split(',');

                var tele = new Telemetri
                {
                    Statu = "Beklemede",
                    Basinc = float.Parse(pots[7]) / 100,
                    Donus_Sayisi = 315,
                    Roll = 365,
                    GPS_Long = 37,
                    Gonderme_Zamani = DateTime.Now,
                    Takim_No = 55502,
                    GPS_Lot = 41 ,
                    Inis_Hizi = 3657,
                    Paket_No = 5372,
                    Pil_Gerilimi = 457,
                    Pitch = float.Parse(pots[1]) / 100,
                    RPM = 34,
                    Yaw = float.Parse(pots[5]) / 100,
                    Yukseklik = float.Parse(pots[8]) / 100,
                    Sicaklik = float.Parse(pots[6]) / 100
                };


                Telemetri.Add(tele);
                dataGridView1.DataSource = dataset = Telemetri.GetAll();

                this.chtBsn.Series["Basınç"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Basinc);
                this.chtDns.Series["Dönüş Sayısı"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Donus_Sayisi);
                this.chtGPSLg.Series["GPS Long"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.GPS_Long);
                this.chtGPSLt.Series["GPS Lot"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.GPS_Lot);
                this.chtHiz.Series["İniş Hızı"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Inis_Hizi);
                this.chtPil.Series["Pil Gerilimi"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Pil_Gerilimi);
                this.chtPtc.Series["Pitch"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Pitch);
                this.chtRoll.Series["Roll"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Roll);
                this.chtRPM.Series["RPM"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.RPM);
                this.chtSck.Series["Sıcaklık"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Sicaklik);
                this.chtYaw.Series["Yaw"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Yaw);
                this.chtYks.Series["Yükseklik"].Points.AddXY(tele.Gonderme_Zamani.ToString(), tele.Yukseklik);


                map.DragButton = MouseButtons.Right;
                map.MapProvider = GMapProviders.GoogleMap;
                map.Position = new GMap.NET.PointLatLng(tele.GPS_Long, tele.GPS_Lot);
                map.MaxZoom = 1000;
                map.MinZoom = 1;
                map.Zoom = 10;

            }


           
        }

       

        private void dataGridView1_DataSourceChanged(object sender, EventArgs e)
        {
            txtStatu.Text = dataset[0].Statu;
            txtBsn.Text = dataset[0].Basinc.ToString();
            txtDns.Text = dataset[0].Donus_Sayisi.ToString();
            txtGnd.Text = dataset[0].Gonderme_Zamani.ToString();
            txtGPSlg.Text = dataset[0].GPS_Long.ToString();
            txtGPSlt.Text = dataset[0].GPS_Lot.ToString();
            txtPil.Text = dataset[0].Pil_Gerilimi.ToString();
            txtPitch.Text = dataset[0].Pitch.ToString();
            txtPkt.Text = dataset[0].Paket_No.ToString();
            txtRoll.Text = dataset[0].Roll.ToString();
            txtRPM.Text = dataset[0].RPM.ToString();
            txtSck.Text = dataset[0].Sicaklik.ToString();
            txtTkm.Text = dataset[0].Takim_No.ToString();
            txtYaw.Text = dataset[0].Yaw.ToString();
            txtHiz.Text = dataset[0].Inis_Hizi.ToString();
            txtYks.Text = dataset[0].Yukseklik.ToString();
            
        }

        private void btnVdGnd_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.Write(rhctxtGirdi.Text);

                txtVdGndDnt.Text = ("Gönderme başarılı.");

            }
            catch (Exception)
            {
                txtVdGndDnt.Text = ("Gönderme başarısız.");

            }

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
