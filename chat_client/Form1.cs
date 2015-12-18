﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
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
                textFriendIP.Text = GetRemoteIPByName(oHostName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                textFriendIP.Text = "127.0.0.1";
            }
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
                    return ip.ToString();
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
                    receivedData = (byte[]) aResult.AsyncState;

                    ASCIIEncoding eEncoding = new ASCIIEncoding();
                    string receivedMessage = eEncoding.GetString(receivedData);


                    if (AnalyzeReceived(receivedMessage.Trim('\0')) == 1) //1 will be acutal message
                    {
                        string message = oHostChat + ": " + receivedMessage;
                        AppendText(message, Color.Red, true);
                        AppendText("", Color.Azure, true);
                        ReceiveSound();
                        FlashWindow.Flash(this);       
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
                epLocal = new IPEndPoint(IPAddress.Parse(textLocalIP.Text), Convert.ToInt32(textLocalPort.Text));
                sck.Bind(epLocal);

                epRemote = new IPEndPoint(IPAddress.Parse(textFriendIP.Text), Convert.ToInt32(textFriendPort.Text));
                sck.Connect(epRemote);

                byte[] buffer = new byte[1500];
                sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote,
                    new AsyncCallback(MessageCallBack), buffer);

                button1.Text = "Connected";
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
                    SendSound();    
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
                case SessionSwitchReason.ConsoleDisconnect:
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

            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;

            richTextBox1.SelectionColor = color;
            richTextBox1.AppendText(text);
            richTextBox1.SelectionColor = richTextBox1.ForeColor;
            richTextBox1.ScrollToCaret();
        }

        public int AnalyzeReceived(string transmission)
        {
            switch (transmission)
            {    case "***###LOCK###***": 
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
                case "***###IMG_GREEN###***":
                    Image img = Properties.Resources.greenlight;
                     ReceiveEmoticon(img);
                    return 0;
                    break;
               default:
                    return 1;
            }
            
          
        }

        private void ReceiveEmoticon(Image image)
        {
            AppendText(oHostChat + ": ", Color.Blue, true);
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.InsertImage(image);
            richTextBox1.ScrollToCaret();
            AppendText("", Color.Blue, true);
            
        }

        private void SendEmoticon(Image image, string sendString)
        {
            AppendText("Me :", Color.Blue, true);

            Clipboard.SetImage(image);
            richTextBox1.Paste();
            Clipboard.Clear();

            AppendText("", Color.Blue, true);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes(sendString);
            sck.Send(msg);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Image img = (Image)sender;
            //string name = Properties.Resources.greenlight.ToString();

            Image img = Properties.Resources.EmbarassedSmile;
            //img.Text = "This is a " + sender.GetType().ToString();
            string sendString = "***###" + Properties.Resources.greenlight.ToString() + "###***";
            SendEmoticon(img, sendString);
            
        }
    }
}