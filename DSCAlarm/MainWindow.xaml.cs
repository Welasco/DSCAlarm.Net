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
using System.IO.Ports;
using System.IO;

namespace DSCAlarm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// https://msdn.microsoft.com/en-us/library/system.io.ports.serialport.datareceived(v=vs.110).aspx
    /// https://code.msdn.microsoft.com/windowsdesktop/SerialPort-brief-Example-ac0d5004
    /// https://code.msdn.microsoft.com/windowsdesktop/SerialPort-brief-Example-ac0d5004
    /// https://msdn.microsoft.com/en-us/library/system.io.ports(v=vs.110).aspx
    ///

    public partial class MainWindow : Window
    {

        private SerialPort _sPort = new SerialPort();
        private delegate void AccessFormMarshalDelegate(string action);
        private delegate void EllipseDelegate(string action);




        public MainWindow()
        {
            Log("Application DSCAlarm Started at: " + DateTime.Now);
            InitializeComponent();
        }

        // Loading comboBox
        // Method to read all available COM ports on PC and add to the combobox object
        private void comboBox_DropDownOpened(object sender, EventArgs e)
        {
            string[] availablePorts = SerialPort.GetPortNames();
            this.comboBox.Items.Clear();
            // Removing duplicated entries on Array
            availablePorts = availablePorts.Distinct().ToArray();
            if (availablePorts != null)
            {
                foreach (string Port in availablePorts)
                {
                    this.comboBox.Items.Add(Port);
                }
            }
        }

        // Connect / Disconnect
        // Method to connect or disconnect to a selected COM port
        private void button_Click(object sender, RoutedEventArgs e)
        {

            if (this.comboBox.Text == "")
            {
                MessageBox.Show("Please select a COM port!");
                return;
            }

            if (button.Content.ToString() == "Connect")
            {

                
                this._sPort.PortName = this.comboBox.Text;
                this._sPort.DataBits = 8;
                //this._sPort.BaudRate = 9600;
                this._sPort.BaudRate = int.Parse(this.comboBox2.Text);
                this._sPort.Parity = Parity.None;
                this._sPort.StopBits = StopBits.One;
                this._sPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                try
                {
                    this._sPort.Open();
                    this.button.Content = "Disconnect";
                    Log("Connected to: " + this.comboBox.Text);
                    Log("BaudRate  to: " + this.comboBox2.Text);

                }
                catch (Exception exc)
                {
                    Log("Failed to connect \r\n " + exc.Message);
                    MessageBox.Show("Failed to connect \r\n " + exc.Message);
                }
                
            }
            else
            {
                try
                {
                    this._sPort.Close();
                    this.button.Content = "Connect";
                    Log("Disconnected");
                }
                catch (Exception exc)
                {
                    Log("Failed to connect \r\n " + exc.Message);
                    MessageBox.Show("Failed to connect \r\n " + exc.Message);
                }
            }
        }

        // Watching received serial Data
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

            AccessFormMarshal("\r\n###############################\r");

            string indata = this._sPort.ReadExisting();
            //string msg = "Data Received in Str: " + indata;

            // Update Zone Status
            updateZoneStatus(indata);

            char[] values = indata.ToCharArray();
            string hexOutput = "";
            string DecOutput = "";
            foreach (char item in values)
            {
                int decValue = Convert.ToInt32(item);
                hexOutput = hexOutput + String.Format("{0:X}", decValue) + "-";
                DecOutput = DecOutput + decValue.ToString() + "-";

            }
            try
            {
                AccessFormMarshal("Data Received in Dec: " + DecOutput.Substring(0, DecOutput.Length - 1) + "\r");
                AccessFormMarshal("Data Received in Hex: " + hexOutput.Substring(0, hexOutput.Length - 1) + "\r");
                AccessFormMarshal("Data Received in Str: " + indata);
            }
            catch (Exception exc)
            {
                //AccessFormMarshal("Data Received in Dec: " + "Cannot trim received data" + "\r");
                //AccessFormMarshal("Data Received in Hex: " + "Cannot trim received data" + "\r");
                AccessFormMarshal("Weird Data Received in Str: " + indata);
            }
            //AccessFormMarshal("Data Received in Dec: " + DecOutput.Substring(0, DecOutput.Length - 1) + "\r");
            //AccessFormMarshal("Data Received in Hex: " + hexOutput.Substring(0, hexOutput.Length - 1) + "\r");
            //AccessFormMarshal("Data Received in Str: " + indata);
            AccessFormMarshal("###############################\r\n");
            //(this.Parent as form)
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);
        }

        // Writing to a richtextBox
        private void AccessFormAppendTextBox(string data)
        {
            this.richTextBox.AppendText(data);
            this.richTextBox.ScrollToEnd();
            Log(data);
        }

        // Dispatching Delegate
        private void AccessFormMarshal(string action)
        {
            MainWindow.AccessFormMarshalDelegate AccessFormMarshalDelegate = new MainWindow.AccessFormMarshalDelegate(this.AccessFormAppendTextBox);
            
            object[] args = new object[]
            {
                action
            };

            base.Dispatcher.Invoke(AccessFormMarshalDelegate, args);
        }

        // Clear richtextBox
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.richTextBox.Document.Blocks.Clear();
            //this.Ellipse.Fill = new SolidColorBrush(Colors.Green);
        }

        //ArmAway
        private void button2_Click(object sender, RoutedEventArgs e)
        {

            if (button.Content.ToString() == "Disconnect")
            {
                try
                {
                    //# Alarm Arm Away Funcionando
                    //#[Byte[]] $request = 48,51,48,49,67,52,13,10
                    this._sPort.DiscardOutBuffer();
                    string armawayCMD = "0301";
                    Byte[] armAway = PrepareCommand(armawayCMD);

                    this._sPort.Write(armAway, 0, armAway.Length);
                    var unsignedBytes = new byte[armAway.Length];
                    Buffer.BlockCopy(armAway, 0,unsignedBytes, 0,armAway.Length);

                    string armAwayDecStr = "";

                    foreach (var item in armAway)
                    {
                        armAwayDecStr = armAwayDecStr + item.ToString() + "-";
                    }
                    AccessFormAppendTextBox("\r\n%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r");
                    AccessFormAppendTextBox("Data sent in Dec: " + armAwayDecStr.Substring(0,armAwayDecStr.Length -1) + "\r");
                    AccessFormAppendTextBox("Data sent in Hex: " + BitConverter.ToString(armAway)+"\r");
                    AccessFormAppendTextBox("Data sent in Str: " + System.Text.Encoding.ASCII.GetString(armAway));
                    AccessFormAppendTextBox("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r\n");
                    //AccessFormAppendTextBox(BitConverter.ToString(armAway) + "\r");
                }
                catch (Exception exc)
                {
                    Log("Faild to send Serial Data: " + exc.Message);
                    MessageBox.Show("Faild to send Serial Data: " + exc.Message);
                }
            }
            else
            {
                MessageBox.Show("You are not connected to a serial port.");
            }
        }

        // ArmStay  
        private void button3_Click(object sender, RoutedEventArgs e)
        {

            if (button.Content.ToString() == "Disconnect")
            {
                try
                {
                    //# Alarm Arm Stay
                    //[Byte[]] $request = 48,51,49,49,67,53,13,10
                    // 0311C5 CR LF
                    this._sPort.DiscardOutBuffer();

                    string armstayCMD = "0311";
                    Byte[] armStay = PrepareCommand(armstayCMD);

                    this._sPort.Write(armStay, 0, armStay.Length);
                    var unsignedBytes = new byte[armStay.Length];
                    Buffer.BlockCopy(armStay, 0, unsignedBytes, 0, armStay.Length);

                    string armStayDecStr = "";

                    foreach (var item in armStay)
                    {
                        armStayDecStr = armStayDecStr + item.ToString() + "-";
                    }

                    AccessFormAppendTextBox("\r\n%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r");
                    AccessFormAppendTextBox("Data sent in Dec: " + armStayDecStr.Substring(0, armStayDecStr.Length - 1) + "\r");
                    AccessFormAppendTextBox("Data sent in Hex: " + BitConverter.ToString(armStay) + "\r");
                    AccessFormAppendTextBox("Data sent in Str: " + System.Text.Encoding.ASCII.GetString(armStay));
                    AccessFormAppendTextBox("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r\n");
                    //AccessFormAppendTextBox(BitConverter.ToString(armStay) + "\r");
                }
                catch (Exception exc)
                {
                    Log("Faild to send Serial Data: " + exc.Message);
                    MessageBox.Show("Faild to send Serial Data: " + exc.Message);
                }
            }
            else
            {
                MessageBox.Show("You are not connected to a serial port.");
            }


        }

        // Disarm
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (button.Content.ToString() == "Disconnect")
            {
                if (passwordBox.Password.Length == 4)
                {
                    try
                    {
                        //# Alarm Arm Stay
                        //[Byte[]] $request = 48,51,49,49,67,53,13,10
                        // 0401180600F4
                        this._sPort.DiscardOutBuffer();

                        //string disarmCMD = "0401180600";
                        string disarmCMD = "0401" + passwordBox.Password.ToString() + "00";
                        Byte[] disarmAlarm = PrepareCommand(disarmCMD);

                        this._sPort.Write(disarmAlarm, 0, disarmAlarm.Length);
                        var unsignedBytes = new byte[disarmAlarm.Length];
                        Buffer.BlockCopy(disarmAlarm, 0, unsignedBytes, 0, disarmAlarm.Length);

                        string disarmAlarmDecStr = "";

                        foreach (var item in disarmAlarm)
                        {
                            disarmAlarmDecStr = disarmAlarmDecStr + item.ToString() + "-";
                        }
                        AccessFormAppendTextBox("\r\n%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r");
                        AccessFormAppendTextBox("Data sent in Dec: " + disarmAlarmDecStr.Substring(0, disarmAlarmDecStr.Length - 1) + "\r");
                        AccessFormAppendTextBox("Data sent in Hex: " + BitConverter.ToString(disarmAlarm) + "\r");
                        AccessFormAppendTextBox("Data sent in Str: " + System.Text.Encoding.ASCII.GetString(disarmAlarm));
                        AccessFormAppendTextBox("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r\n");
                        //AccessFormAppendTextBox(BitConverter.ToString(disarmAlarm) + "\r");
                    }
                    catch (Exception exc)
                    {
                        Log("Faild to send Serial Data: " + exc.Message);
                        MessageBox.Show("Faild to send Serial Data: " + exc.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Please type your Password before Disarm the Alarm.");
                }

            }
            else
            {
                MessageBox.Show("You are not connected to a serial port.");
            }
        }

        // Disarm backup before checksum
        //private void button4_Click(object sender, RoutedEventArgs e)
        //{
        //    if (button.Content.ToString() == "Disconnect")
        //    {
        //        try
        //        {
        //            //# Alarm Arm Stay
        //            //[Byte[]] $request = 48,51,49,49,67,53,13,10
        //            // 0401180600F4
        //            this._sPort.DiscardOutBuffer();
        //            Byte[] disarmAlarm = new byte[14];
        //            disarmAlarm[0] = 48;
        //            disarmAlarm[1] = 52;
        //            disarmAlarm[2] = 48;
        //            disarmAlarm[3] = 49;
        //            disarmAlarm[4] = 49;
        //            disarmAlarm[5] = 56;
        //            disarmAlarm[6] = 48;
        //            disarmAlarm[7] = 54;
        //            disarmAlarm[8] = 48;
        //            disarmAlarm[9] = 48;
        //            disarmAlarm[10] = 70;
        //            disarmAlarm[11] = 52;
        //            disarmAlarm[12] = 13;
        //            disarmAlarm[13] = 10;

        //            this._sPort.Write(disarmAlarm, 0, disarmAlarm.Length);
        //            var unsignedBytes = new byte[disarmAlarm.Length];
        //            Buffer.BlockCopy(disarmAlarm, 0, unsignedBytes, 0, disarmAlarm.Length);

        //            string disarmAlarmDecStr = "";

        //            foreach (var item in disarmAlarm)
        //            {
        //                disarmAlarmDecStr = disarmAlarmDecStr + item.ToString() + "-";
        //            }
        //            AccessFormAppendTextBox("\r\n%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r");
        //            AccessFormAppendTextBox("Data sent in Dec: " + disarmAlarmDecStr.Substring(0, disarmAlarmDecStr.Length - 1) + "\r");
        //            AccessFormAppendTextBox("Data sent in Hex: " + BitConverter.ToString(disarmAlarm) + "\r");
        //            AccessFormAppendTextBox("Data sent in Str: " + System.Text.Encoding.ASCII.GetString(disarmAlarm));
        //            AccessFormAppendTextBox("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\r\n");
        //            //AccessFormAppendTextBox(BitConverter.ToString(disarmAlarm) + "\r");
        //        }
        //        catch (Exception exc)
        //        {
        //            Log("Faild to send Serial Data: " + exc.Message);
        //            MessageBox.Show("Faild to send Serial Data: " + exc.Message);
        //        }

        //    }
        //}

        // Update Zone Status
        private void updateZoneStatus(string indata)
        {
            MainWindow.EllipseDelegate AccessEllipseDelegate = new MainWindow.EllipseDelegate(this.AccessEllipse);

            object[] args = new object[]
            {
                indata
            };

            base.Dispatcher.Invoke(AccessEllipseDelegate, args);

        }

        private void AccessEllipse(string indata)
        {

            if (indata.Length >= 3)
            {
                if (indata.Substring(0, 3) == "609")
                {
                    switch (indata.Substring(3, 3))
                    {
                        case "001":
                            this.Ellipse.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case "002":
                            this.Ellipse1.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case "003":
                            this.Ellipse2.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case "004":
                            this.Ellipse3.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case "005":
                            this.Ellipse4.Fill = new SolidColorBrush(Colors.Green);
                            break;
                        case "006":
                            this.Ellipse5.Fill = new SolidColorBrush(Colors.Green);
                            break;
                    }
                }

                if (indata.Substring(0, 3) == "610")
                {
                    switch (indata.Substring(3, 3))
                    {
                        case "001":
                            this.Ellipse.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                        case "002":
                            this.Ellipse1.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                        case "003":
                            this.Ellipse2.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                        case "004":
                            this.Ellipse3.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                        case "005":
                            this.Ellipse4.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                        case "006":
                            this.Ellipse5.Fill = new SolidColorBrush(Colors.Orange);
                            break;
                    }
                }
            }
        }

        // Write a log file https://msdn.microsoft.com/en-us/library/3zc0w663(v=vs.110).aspx
        public static void Log(string logMessage)
        {
            using (StreamWriter w = File.AppendText("DSCAlarmLog.txt"))
            {
                //w.Write("\r\nLog Entry : ");
                //w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                //w.WriteLine("  :");
                //w.WriteLine("  :{0}", logMessage);
                //w.WriteLine("-------------------------------");

                w.WriteLine(logMessage);
            }
        }

        // Method to calculate the checksum and prepare the command to be sent to the alarm board
        public Byte[] PrepareCommand(string cmd)
        {
            // The CheckSum must be calculated like this:
            // Command to ArmAway is: 030 but you need to choose the partition like 1
            // So the full command is 0301 in decimal
            // To calculate the check sum you need to convert the command do Hex
            // the command would be 30 33 30 31
            // We need to sum it: 30+33+30+31=C4 so C4 is the check sum
            // C4 in Decimal is 67 52
            // We will work with decimal representantion on C#

            // 48 = 30 = 0;
            // 52 = 34 = 4;
            // 48 = 30 = 0;
            // 49 = 31 = 1;
            // 49 = 31 = 1;
            // 56 = 38 = 8;
            // 48 = 30 = 0;
            // 54 = 36 = 6;
            // 48 = 30 = 0;
            // 48 = 30 = 0;
            // 70 = 46 = F;
            // 52 = 34 = 4;
            // 13 = D  = CR;
            // 10 = A  = LF;

            decimal decValue = 0;
            Byte[] arrChar = new byte[cmd.Length + 4];
            int i = 0;
            foreach (char chr in cmd)
            {
                decValue = chr;
                arrChar[i] = (byte)decValue;
                i++;
            }


            int result = 0;
            foreach (int intValue in arrChar)
            {
                result = result + intValue;
            }

            string checkSum;
            checkSum = BitConverter.ToString(BitConverter.GetBytes(result)).Substring(0, 2);

            arrChar[cmd.Length] = (byte)((decimal)checkSum[0]);
            arrChar[cmd.Length + 1] = (byte)((decimal)checkSum[1]);
            arrChar[cmd.Length + 2] = 13;
            arrChar[cmd.Length + 3] = 10;

            return arrChar;

        }
    }
}
