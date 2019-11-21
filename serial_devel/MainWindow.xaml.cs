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
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Threading;

namespace serial_devel
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            selPort();
            AddBaudrate(cmb_baud);
            SetupTimer();
            cmb.SelectedIndex = 0;
            cmb_baud.SelectedIndex = 2;
        }

        System.IO.Ports.SerialPort serialPort = null;
        public class ComPort
        {
            public string DeviceID { get; set; }
            public string Description { get; set; }
        }

       /*----------------------------
        * シリアルポート列挙
        *---------------------------*/
        private void selPort()
        {
            // シリアルポートの列挙
            string[] PortList = SerialPort.GetPortNames();
            var MyList = new ObservableCollection<ComPort>();
            foreach (string p in PortList)
            {
                System.Console.WriteLine(p);
                MyList.Add(new ComPort { DeviceID = p, Description = p });
            }
            cmb.ItemsSource = MyList;
            cmb.SelectedValuePath = "DeviceID";
            cmb.DisplayMemberPath = "Description";
        }

        /*----------------------------
         * 接続ボタンを押した時の処理
         *---------------------------*/
        private void connect_Click(object sender, RoutedEventArgs e)
        {
            // ポートが選択されている場合
            if (cmb.SelectedValue != null)
            {
                // ポート名を取得
                var port = cmb.SelectedValue.ToString();
                // まだポートに繋がっていない場合
                if (serialPort == null)
                {
                    // serialPortの設定
                    serialPort = new SerialPort();
                    serialPort.PortName = port;
                    serialPort.BaudRate = Int32.Parse(cmb_baud.Text);
                    serialPort.DataBits = 8;
                    serialPort.Parity = Parity.None;
                    serialPort.StopBits = StopBits.One;
                    serialPort.Encoding = Encoding.UTF8;
                    serialPort.WriteTimeout = 100000;
                    serialPort.DtrEnable = true;
                    serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);

                    // シリアルポートに接続
                    try
                    {
                        // ポートオープン
                        serialPort.Open();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        /*----------------------------
         * 切断ボタンを押した時
         *---------------------------*/
        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort = null;
            }
        }

        /*----------------------------
         * 送信ボタンを押した時
         *---------------------------*/
        private void send_Click(object sender, RoutedEventArgs e)
        {
            // 繋がっていない場合は処理しない。
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;

            // テキストボックスから、送信するテキストを取り出す。
            String data = sendText.Text + "\r\n";

            try
            {
                // シリアルポートからテキストを送信する.
                serialPort.Write(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        delegate void Delegate_RcvDataToTextBox(string data);

        //データ受信
        private void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // 繋がっていない場合は処理しない。
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;

            try
            {
                //! 受信データを読み込む.
                string data = serialPort.ReadExisting();

                //! 受信したデータをテキストボックスに書き込む.
                Dispatcher.Invoke(new Delegate_RcvDataToTextBox(RcvDataToTextBox), new Object[] { data });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RcvDataToTextBox(string data)
        {
            Receive_txtBox.AppendText(data);
            Receive_txtBox.ScrollToEnd();
        }

        /// <summary>
        /// CombBoxにボーレートを加える
        /// </summary>
        /// <param name="comboBox"></param>
        private void AddBaudrate(System.Windows.Controls.ComboBox comboBox)
        {
            comboBox.Items.Add(9600);
            comboBox.Items.Add(38400);
            comboBox.Items.Add(115200);
        }

        private void SetupTimer()
        {
            // タイマのインスタンスを生成
            var timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                // インターバルを設定
                Interval = TimeSpan.FromSeconds(0.2),
            };
            // タイマメソッドを設定（ラムダ式で記述）
            timer.Tick += (s, e) =>
            {
                status.Content = "STATUS : ";
                if (serialPort != null && serialPort.IsOpen == true)
                    status.Content += "CONNECTED";
                else
                    status.Content += "DISCONNECT";

            };
            // タイマを開始
            timer.Start();

            // 画面が閉じられるときに、タイマを停止（ラムダ式で記述）
            this.Closing += (s, e) => timer.Stop();
        }



    }
}
