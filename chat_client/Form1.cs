using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;


namespace chat_client
{                                                     
    public partial class Form1 : Form
    {
        private Socket sck;
        private EndPoint epLocal, epRemote;
        private string lHostName, oHostName, lHostChat, oHostChat;
        public string theConversation;
        public bool active;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;

        public Form1()
        {                           
            InitializeComponent();
           SystemEvents.SessionSwitch +=
                         new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            textLocalIP.Text = GetLocalIP();
            pictureBox1.Image = Properties.Resources.whitelight;
            label5.Text = "Offline";

            lHostName = Dns.GetHostName();
            
            if (lHostName == "JSANCHEZ-LT")
            {
                groupBox1.Text = "Jesse";
                groupBox2.Text = "Missy";
                oHostName = "HBOWLES-LT";
                oHostChat = "Missy";
                lHostChat = "Jesse";
            }
            else if (lHostName == "HBOWLES-LT")
            {
                groupBox1.Text = "Missy";
                groupBox2.Text = "Jesse";
                oHostName = "JSANCHEZ-LT";
                oHostChat = "Jesse";
                lHostChat = "Missy";               
            }

            try
            {
                comboBox1.Items.Add(GetRemoteIPByName(oHostName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                comboBox1.Items.Add("127.0.0.1");
            }
            comboBox1.SelectedIndex = 0;
            textLocalPort.Text = "80";
            textFriendPort.Text = "80";
            button1.Focus();
            button2.Enabled = false;
            //listView1.Visible = false;

        }

        private string GetLocalIP()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());

            string ipAddress = "127.0.0.1";
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return ipAddress;
        }

        private string GetRemoteIPByName(string rHost)
        {
            IPHostEntry hostMissy = Dns.GetHostByName(rHost);
            string ipAddress = "127.0.0.1";
            foreach (IPAddress ip in hostMissy.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    pictureBox1.Image = Properties.Resources.greenlight2;
                    label5.Text = "Online";
                    comboBox1.Items.Add(ip.ToString());
                    ipAddress = ip.ToString();
                }
            }
            return ipAddress;

        }
        private void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                
                int size = sck.EndReceiveFrom(aResult, ref epRemote);
                if (size > 0)
                {
                    byte[] receivedData = new byte[1464];
                    receivedData = (byte[])aResult.AsyncState;

                    ASCIIEncoding eEncoding = new ASCIIEncoding();
                    string receivedMessage = eEncoding.GetString(receivedData);
                                        
                    if (AnalyzeReceived(receivedMessage.Trim('\0')) == 1) //1 will be acutal message
                    {
                        string message = oHostChat + ": " + receivedMessage.Trim('\0');
                        AppendText(message, Color.Red, true);
                        AppendText("", Color.Azure, true);
                        theConversation += message + "\n";
                        ScrollToBottom(richTextBox1);
                        ReceiveSound();
                        this.Invoke((Action)delegate
                        {
                            FlashWindow.Flash(this);
                        });
                    }
                    else
                    {
                        theConversation += oHostChat+": "+ receivedMessage.Trim('\0')+"\n";               //the ghost conversation so you can delete
                    }
                }
                byte[] buffer = new byte[1500];
                sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote,
                    new AsyncCallback(MessageCallBack), buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                //throw;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (sck.Connected == true)
                {
                    //sck.Disconnect(true);
                    sck.Close();
                }
                sck = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                epLocal = new IPEndPoint(IPAddress.Parse(textLocalIP.Text), Convert.ToInt32(textLocalPort.Text));
                sck.Bind(epLocal);

                epRemote = new IPEndPoint(IPAddress.Parse(comboBox1.Text), Convert.ToInt32(textFriendPort.Text));
                sck.Connect(epRemote);

                byte[] buffer = new byte[1500];
                sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote,
                    new AsyncCallback(MessageCallBack), buffer);

               // button1.Text = "Connected";
                //button1.Enabled = false;
                button2.Enabled = true;
                textMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
               System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
               byte[] msg = new byte[1500];
                msg = enc.GetBytes(textMessage.Text);

                sck.Send(msg);

                int size = msg.Length;
                
                if (size > 0)
                {
                    string message = "Me: " + textMessage.Text;
                    AppendText(message, Color.Blue, true);
                    theConversation += message + "\n";
                    ScrollToBottom(richTextBox1);
                    SendSound();
                    //if (Form1.ActiveForm.Activated)
                    //{
                    //    deleteLast.Enabled = true;
                    //}
                }
               
                textMessage.Clear();
           }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2.PerformClick();
                // these last two lines will stop the beep sound
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void ReceiveSound()
        {
            SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.boing_spring);
            simpleSound.Play();
        }

        private void SendSound()
        {
            SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.bloop_x);
            simpleSound.Play();
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            switch (e.Reason)
            {
                // ...
                case SessionSwitchReason.SessionLock:
                    // Do whatever you need to do for a lock
                    msg = enc.GetBytes("***###LOCK###***");
                    sck.Send(msg);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    // Do whatever you need to do for an unlock
                    msg = enc.GetBytes("***###UNLOCK###***");
                    sck.Send(msg);
                    break;
                case SessionSwitchReason.SessionLogoff:
                    msg = enc.GetBytes("***###OFF###***");
                    sck.Send(msg);
                    break;
                    // ...
            }
        }


        public void AppendText(string text, Color color, bool AddNewLine = false)
        {
            if (AddNewLine)
            {
                text += Environment.NewLine;
            }

            richTextBox1.Invoke((Action)delegate
            {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;

                richTextBox1.SelectionColor = color;
                richTextBox1.AppendText(text);
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
                ScrollToBottom(richTextBox1);
            });
        }

        public int AnalyzeReceived(string transmission)
        {
            Image img = null;
            switch (transmission)
            {
                case "***###ACTIVATED###***":
                    active = true;
                    MessageBox.Show("Activated");
                    return 0;
                    break;
                case "***###DEACTIVATED###***":
                    active = false;
                    MessageBox.Show("Deactivated");
                    return 0;
                    break;
                case "***###LOCK###***": 
                    pictureBox1.Image = Properties.Resources.yellowlight;
                    label5.Text = "Away";
                    return 0;
                    break;
                case "***###UNLOCK###***":
                    pictureBox1.Image = Properties.Resources.greenlight2;
                    label5.Text = "Online";
                    return 0;
                    break;
                case "***###OFF###***":
                    pictureBox1.Image = Properties.Resources.whitelight;
                    label5.Text = "Offline";
                    return 0;
                    break;
                case "***###EmbarassedSmile###***":
                    img = Properties.Resources.EmbarassedSmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###AngelSmile###***":
                     img = Properties.Resources.AngelSmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###AngrySmile###***":
                     img = Properties.Resources.AngrySmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###Beer###***":
                    img = Properties.Resources.Beer;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###BrokenHeart###***":
                    img = Properties.Resources.BrokenHeart;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###ConfusedSmile###***":
                    img = Properties.Resources.ConfusedSmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###CrySmile###***":
                    img = Properties.Resources.CrySmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###DevilSmile###***":
                    img = Properties.Resources.DevilSmile;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###ThumbsUp###***":
                    img = Properties.Resources.ThumbsUp;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###black_eye###***":
                    img = Properties.Resources.black_eye;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                case "***###slapping###***":
                    img = Properties.Resources.slapping;
                    ReceiveEmoticon(img);
                    return 0;
                    break; 
                case "***###shit_emoticon###***":
                    img = Properties.Resources.shit_emoticon;
                    ReceiveEmoticon(img);
                    return 0;
                    break;
                default:
                    return 1;
            }
            
          
        }

        public static void ScrollToBottom(RichTextBox MyRichTextBox)
        {
            MyRichTextBox.Invoke((Action)delegate
           {
               SendMessage(MyRichTextBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
           });
        }

        private void ReceiveEmoticon(Image image)
        {
            AppendText(oHostChat + ": ", Color.Red);
            richTextBox1.Invoke((Action)delegate
            {
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.InsertImage(image);
            });
            AppendText("", Color.Red, true);
            ScrollToBottom(richTextBox1);
        }

        private void SendEmoticon(Image image, string sendString)
        {
            AppendText("Me :", Color.Blue, false);
            Clipboard.SetImage(image);
            richTextBox1.Paste();
            Clipboard.Clear();
            AppendText("", Color.Blue, true);
            ScrollToBottom(richTextBox1);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes(sendString);
            theConversation += "Me: "+sendString + "\n";
            sck.Send(msg);
            textMessage.Focus();
        }

        private void button3_Click(object sender, EventArgs e)                                                                         
        {
            Image img = Properties.Resources.EmbarassedSmile;
            string name = button3.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.AngelSmile;
            string name = button4.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.AngrySmile;
            string name = button5.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.Beer;
            string name = button6.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.BrokenHeart;
            string name = button7.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.ConfusedSmile;
            string name = button8.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.CrySmile;
            string name = button9.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.shit_emoticon;
            string name = button10.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.DevilSmile;
            string name = button11.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.ThumbsUp;
            string name = button12.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.black_eye;
            string name = button13.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Image img = Properties.Resources.slapping;
            string name = button14.Tag.ToString();
            string sendString = "***###" + name + "###***";
            SendEmoticon(img, sendString);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           // sck.Disconnect(false);
            sck.Close();
            sck.Dispose();
        }

        private void deleteLast_Click(object sender, EventArgs e)
        {
            string theText = theConversation;
            int where = theText.LastIndexOf(':');
            string deletedText = theText.Remove(where - 2);
            MessageBox.Show(deletedText);
        
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes("***###ACTIVATED###***");
            sck.Send(msg);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes("***###DEACTIVATED###***");
            sck.Send(msg);
        }
    }
}
