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
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Windows.Documents;
//using System.Windows.Controls;

//using Telerik.WinControls.RichTextBox.Model;


namespace chat_client
{                                                     
    public partial class Form1 : Form
    {
        private Socket sck;
        private EndPoint epLocal, epRemote;
        private string lHostName, oHostName, lHostChat, oHostChat;
        private bool isLastMessageDelivered = false;
        //public string[] theConversation;
        //public Dictionary<string, int> theConversation = new Dictionary<string, int>();
        //public List<Tuple<string, int>> theConversation = new List<Tuple<string, int>>();
        //public bool active = false, connectedToSocket = false, firstMessageSent = false;
        public List<TMessage> conversationList = new List<TMessage>();
        //public List<string> receivedMessages = new List<string>();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;
        //int typeOfReload = 0;

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
            //Logic to determine who am I
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
            else
            {
                groupBox1.Text = "Me";
                groupBox2.Text = "Friend";
                lHostName = "Me";
                oHostName = "Friend";
                oHostChat = "Friend";
                lHostChat = "Me";
            }

            try
            {
                comboBox1.Items.Add(GetRemoteIPByName(oHostName));       //get other client's IP Address, if not put 127.0.0.1
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                comboBox1.Items.Add(textLocalIP.Text);    //when client's IP is not found, it throws an error, and this just puts my IP there
            }
            comboBox1.SelectedIndex = 0;
            textLocalPort.Text = "80";
            textFriendPort.Text = "80";
            button1.Focus();
            button2.Enabled = false;
            label6.Visible = false;
            listBox2.Visible = false;
        }

        //Get my own IP adress
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

         //Get Other client's IP Addresses(plural)
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

         //Receiving Messages
        private void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                int size = 0;
                try
                {
                    size = sck.EndReceiveFrom(aResult, ref epRemote);
                }
                  catch(SocketException se)
                {
                      //do nothing. this is for when it is sending messages back and forth about being active and 
                      //the other chat client isn't up yet, it would throw an exception.
                }
                if (size > 0)
                {
                    byte[] receivedData = new byte[1464];
                    receivedData = (byte[])aResult.AsyncState;

                    ASCIIEncoding eEncoding = new ASCIIEncoding();
                    string receivedMessage = eEncoding.GetString(receivedData).Trim('\0');

                    AnalyzeMessage(receivedMessage);
                    Display_Dictionary();
                }
                byte[] buffer = new byte[1500];
                sck.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote,
                    new AsyncCallback(MessageCallBack), buffer);
            }
            catch (SocketException SocEx)
            {
               // MessageBox.Show(SocEx.ToString());
                //throw;
            }
        }

         //Not sure if I'll need this later, but keeping it here for now... I figured another way to do this for now, just catching exceptions
        bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
    
        public bool IsApplicationActive()
        {
            return this.ContainsFocus; 
        }

        //analyze whole message to decide what type of message it is and what to do with it
        private void AnalyzeMessage(string message)
        {
            string theHeader = message.Substring(0, 4);
            int typeOfMessage = AnalyzeHeader(theHeader);
            if (typeOfMessage == 1)
            { ReceiveTextMessage(message); }
            if (typeOfMessage == 3)
            { ReceiveEmoticonMessage(message); }
            if (typeOfMessage == 4)
            { ReceiveDeleteMessage(message); }
            if(typeOfMessage == 5)
            { ReceiveActionMessage(message); }
            if (typeOfMessage == 6)
            { ReceiveDeliveredMessage(message); }

        }

        private void ReceiveEmoticonMessage(string message)           //function gets run when incoming message is an emoticon
        {
            string tID = message.Substring(4, 36);

            TMessage receiveEmoticon = new TMessage(tID);               //Create the Message Object
            receiveEmoticon.header = message.Substring(0, 4);
            receiveEmoticon.msg = message.Remove(0, 40);
            receiveEmoticon.sender = oHostChat;
            receiveEmoticon.read = true;

            System.Drawing.Image img = Images.GetImage(receiveEmoticon.msg);

            TMessage deliveredE = new TMessage(tID);                //Create the delivered message
            deliveredE.header = "0006";
            deliveredE.msg = "DELIVERED";
            deliveredE.sender = "Me";
            deliveredE.read = false;
            deliveredE.isActive = IsApplicationActive();

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes(deliveredE.header + deliveredE.msgID + deliveredE.isActive + deliveredE.msg);
            sck.Send(msg);                                     //send back the message of delivered and read or not read
                                                              
            RichTextBox myERTFEmoticon = new RichTextBox();

            flowLayoutPanel1.Invoke((Action)delegate
            {
                try
                {
                    flowLayoutPanel1.Controls.Add(myERTFEmoticon);
                }
                catch (Exception aex)
                {
                    MessageBox.Show(aex.ToString());
                }
            });
            AppendText(myERTFEmoticon, receiveEmoticon.sender + ": ", Color.Red, false);       //append the sender: 
            RichTextBoxStuff.EmbedImage(img, myERTFEmoticon);
           // myERTFEmoticon.InsertImage(img);
            myERTFEmoticon.Invoke((Action)delegate
            {
                myERTFEmoticon.Dock = DockStyle.Top;
                myERTFEmoticon.BorderStyle = BorderStyle.None;
                myERTFEmoticon.Anchor = AnchorStyles.Top;

                myERTFEmoticon.Size = new System.Drawing.Size(flowLayoutPanel1.Width - 24, img.Height + 5);
                flowLayoutPanel1.ScrollControlIntoView(myERTFEmoticon);

            });

            conversationList.Add(receiveEmoticon);                      //add it to the List keeping track of the conversation
        }

        //Function which receives a message saying that your message was delivered and then sets whether the other chat client is active or not
        private void ReceiveDeliveredMessage(string message)
        {
            string wasItActive = message.Substring(40, 4);
            isLastMessageDelivered = true;
            label6.Invoke((Action)delegate
                {
                if (wasItActive == "true")
                    {
                        label6.Text = "Activated";
                    }
                    else
                        label6.Text = "Deactivated";
                        deleteLast.Visible = true;
                });

        }

        //Function that receives message telling it to delete the last message.
        private void ReceiveDeleteMessage(string message)
        {

            string tID = message.Substring(4, 36);                                      //Get GUID of received message
            if (conversationList[conversationList.Count - 1].msgID == tID)              //if it's the same on your conversation list, then delete
            {
                conversationList.RemoveAll(t=>t.msgID == tID);                          //delete from converationList
                Control c = flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1];
                c.Dispose();                                                            //delete from UI
            }
        }

        private void ReceiveTextMessage(string message)
        {
            string tID = message.Substring(4, 36);                      //Get GUID of received message

            TMessage receivedMessage = new TMessage(tID);               //Create the received message object
            receivedMessage.header = message.Substring(0, 4);
            receivedMessage.msg = message.Remove(0, 40);
            receivedMessage.sender = oHostChat;
            receivedMessage.read = true;

            TMessage delivered = new TMessage(tID);               //Create the delivered message object
            delivered.header = "0006";
            delivered.msg = "DELIVERED";
            delivered.sender = "Me";
            delivered.read = false;
            delivered.isActive = IsApplicationActive();

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes(delivered.header + delivered.msgID + delivered.isActive + delivered.msg);
            sck.Send(msg);                                    //send back the message of delivered and read or not read

            if (receivedMessage.msg.Count() > 0)
            {
                RichTextBox myERTF1 = new RichTextBox();

                flowLayoutPanel1.Invoke((Action)delegate
                {
                    try
                    {
                        flowLayoutPanel1.Controls.Add(myERTF1);
                    }
                    catch (Exception aex)
                    {
                        MessageBox.Show(aex.ToString());
                    }
                });

                AppendText(myERTF1, receivedMessage.sender + ": " + receivedMessage.msg, Color.Red, true);       //Put the message into the RichTextBox inside layout
                int CountOfCharacters = 0;
                myERTF1.Invoke((Action)delegate
                {
                    myERTF1.Dock = DockStyle.Top;
                    myERTF1.BorderStyle = BorderStyle.None;
                    myERTF1.Anchor = AnchorStyles.Top;
                    foreach (char c in myERTF1.Text)
                    {
                        CountOfCharacters++;
                    }
                    int lineCount = 2 + (CountOfCharacters + 4) / 107;
                    myERTF1.Size = new System.Drawing.Size(flowLayoutPanel1.Width - 24, lineCount * (Font.Height));
                    flowLayoutPanel1.ScrollControlIntoView(myERTF1);

                });

                conversationList.Add(receivedMessage);
            }   //end if recievedMessage.msg.count > 0
        }

        private int AnalyzeHeader(string header)                //analyzes message header and returns an int
        {
            int intHeader;
            Int32.TryParse(header, out intHeader);
           
            switch(intHeader)
            {
                case (int)headerCodes.messageID:
                    return 0;
                case (int)headerCodes.message:
                    return 1;
                case (int)headerCodes.sender:
                    return 2;
                case (int)headerCodes.emoticon:
                    return 3;
                case (int)headerCodes.deleteID:
                    return 4;
                case (int)headerCodes.action:
                    return 5;
                case (int)headerCodes.delivered:
                    return 6;
                default:
                    return 1;
            }
        }
        private void Connect_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (sck.Connected == true)
                {
                    //sck.Disconnect(true);
                    //sck.Dispose();
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

                button1.Text = "Connected";
                button1.Enabled = false;
                button2.Enabled = true;
                textMessage.Focus();
               // connectedToSocket = true;
                //active = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Send_Button_Click(object sender, EventArgs e)
        {
            try
            {                             
               
               TMessage sentMessage = new TMessage();
               sentMessage.header = "0001";
               sentMessage.msg = textMessage.Text;
               sentMessage.read = false;
               sentMessage.sender = "Me";

               System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
               byte[] msg = new byte[1500];
               msg = enc.GetBytes(sentMessage.header + sentMessage.msgID + sentMessage.msg);

                sck.Send(msg);
                isLastMessageDelivered = false;
                int size = msg.Length;
                
                if (size > 40)  //cause the message includes the header and the msgID which are 40 characters long, so anything else is actual message
                {
                   
                    string message = sentMessage.sender + ": " + sentMessage.msg;
                   
                    ExRichTextBox myERTF = new ExRichTextBox();
                    myERTF.Dock = DockStyle.Top;
                    myERTF.BorderStyle = BorderStyle.None;
                    myERTF.Anchor = AnchorStyles.Top;
                    //myERTF.AutoSize = true;
                    flowLayoutPanel1.Controls.Add(myERTF);
                                                                  
                    AppendText(myERTF, message, Color.Blue, false);
                    
                    int CountOfCharacters = 0;
                    foreach(char c in myERTF.Text)
                    {
                        CountOfCharacters++;
                    }
                    int lineCount = 2 + (CountOfCharacters + 4)/ 107;
                    myERTF.Size = new System.Drawing.Size(flowLayoutPanel1.Width - 24, lineCount*(Font.Height));
                    
                    flowLayoutPanel1.ScrollControlIntoView(myERTF);
                    
                    conversationList.Add(sentMessage);      //adding the object to the list of messages in conversation
                    SendSound();
                     
                    if(label6.Text == "Activated")
                    {
                        Change_All_To_Read();
                    }

                    if (isLastMessageDelivered == true)
                    {
                        label6.Invoke((Action)delegate
                        {
                            if (label6.Text == "Deactivated")
                                deleteLast.Visible = true;
                            else if (label6.Text == "Activated")
                                deleteLast.Visible = false;
                        });
                        isLastMessageDelivered = false;
                    }
                    Display_Dictionary();               //For Listbox2 which is for debugging, now it is hidden
                }
                textMessage.Clear();
           }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //Function so you can press enter to send the message you are sending
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

        //Function that sends the Event whenever the other computer is locked or unlocked
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            
            TMessage actionObject = new TMessage();   //create an object to send the action with only the message pending
            actionObject.header = "0005";
            actionObject.read = false;
            actionObject.sender = "Me";

            switch (e.Reason)
            {
                   //Set the Object's .msg property with the switch           
                case SessionSwitchReason.SessionLock:
                    actionObject.msg = "LOCK";       
                    break;

                case SessionSwitchReason.SessionUnlock:
                    actionObject.msg = "UNLOCK";
                    break;

                case SessionSwitchReason.SessionLogoff:
                    actionObject.msg = "LOGOFF";
                    break;
                    
            }
            //send the object to the other host to get decrypted
            msg = enc.GetBytes(actionObject.header + actionObject.msgID + actionObject.msg);
            sck.Send(msg);
        }

         //Function that adds text to the RichTextBox you tell it to.
        public void AppendText(RichTextBox rtb, string text, Color color, bool AddNewLine = false)
        {
            if (AddNewLine)
            {
                text += Environment.NewLine;
            }

            rtb.Invoke((Action)delegate
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;

                rtb.SelectionColor = color;
                rtb.AppendText(text);
                rtb.SelectionColor = rtb.ForeColor;
                ScrollToBottom(rtb);
            });
        }

        //Function that recieves message with action such as away or logged off
        public void ReceiveActionMessage(string transmission)
        {
            string tID = transmission.Substring(4, 36);                      //Get GUID of received message

            TMessage receivedAction = new TMessage(tID);               //Create the Message Object
            receivedAction.header = transmission.Substring(0, 4);
            receivedAction.msg = transmission.Remove(0, 40);
            receivedAction.sender = oHostChat;
            receivedAction.read = false;

            Image img = null;
            switch (receivedAction.msg)
            {
                case "ACTIVATED":
                    //active = true;
                    label6.Invoke((Action)delegate
                    {
                        label6.Text = "Activated";
                    });

                    if (deleteLast.Visible == true)
                    { deleteLast.Visible = false; }
                    break;
                case "DEACTIVATED":
                   // active = false;
                    label6.Invoke((Action)delegate
                    {
                        label6.Text = "Deactivated";
                    });
                    break;
                case "LOCK": 
                    pictureBox1.Image = Properties.Resources.redlightc;
                    label5.Text = "Away";
                    break;
                case "UNLOCK":
                    pictureBox1.Image = Properties.Resources.greenlight2;
                    label5.Text = "Online";
                    break;
                case "OFF":
                    pictureBox1.Image = Properties.Resources.whitelight;
                    label5.Text = "Offline";
                    break;
                
                default:
                   pictureBox1.Image = Properties.Resources.redlightc;
                    label5.Text = "Offline";
                    break; 
            }
            
          
        }
        //Functio to Scroll To Bottom of the Rich Text Box   || I don't think I need this anymore, since i am scrolling to bottom of flowLayoutPanel now
        public static void ScrollToBottom(RichTextBox MyRichTextBox)
        {
            MyRichTextBox.Invoke((Action)delegate
           {
               SendMessage(MyRichTextBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
           });
        }
      
        //Function that sends Emoticon when any emoticon button is pressed
        private void SendEmoticon(TMessage emoticon)
        {
            ExRichTextBox sendEmoTextBox = new ExRichTextBox();
            
            AppendText(sendEmoTextBox, emoticon.sender + ": ", Color.Blue, false);
            Image image = Images.GetImage(emoticon.msg);
            sendEmoTextBox.InsertImage(image);
            
            flowLayoutPanel1.Controls.Add(sendEmoTextBox);
           
            sendEmoTextBox.Invoke((Action)delegate
            {
                sendEmoTextBox.Dock = DockStyle.Top;
                sendEmoTextBox.BorderStyle = BorderStyle.None;
                sendEmoTextBox.Anchor = AnchorStyles.Top;
                sendEmoTextBox.Size = new System.Drawing.Size(flowLayoutPanel1.Width - 24, image.Height+5);
                flowLayoutPanel1.ScrollControlIntoView(sendEmoTextBox);
            });

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];
            msg = enc.GetBytes(emoticon.header + emoticon.msgID + emoticon.msg);
            conversationList.Add(emoticon);
            sck.Send(msg);
            isLastMessageDelivered = false;
            
            SendSound();
            textMessage.Focus();


            if (label6.Text == "Activated")
            {
                Change_All_To_Read();
            }

            if (isLastMessageDelivered == true)
            {
                label6.Invoke((Action)delegate
                {
                    if (label6.Text == "Deactivated")
                        deleteLast.Visible = true;
                    else if (label6.Text == "Activated")
                        deleteLast.Visible = false;
                });
                isLastMessageDelivered = false;
            }
            Display_Dictionary();                          //For listbox2 which is for debugging, now it is hidden
        }

        private void button3_Click(object sender, EventArgs e)                                                                         
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button3.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);    
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button4.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button5.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button6.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button7.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button8.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button9.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button10.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button11.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button12.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button13.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            TMessage emoObject = new TMessage();
            emoObject.header = "0003";
            emoObject.msg = button14.Tag.ToString();
            emoObject.sender = "Me";
            emoObject.read = false;
            SendEmoticon(emoObject);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           // sck.Disconnect(false);
           // sck.Close();
           // sck.Dispose();
        }

        //Function for what happens when you click on the deleteLast button
        private void deleteLast_Click(object sender, EventArgs e)
        {
           // New_Reload_Own_Text();

            TMessage lastMessage = new TMessage();
            foreach(TMessage message in conversationList)
            {
                lastMessage = message;
                lastMessage.msgID = message.msgID;
            }

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] msg = new byte[1500];

            lastMessage.header = "0004";

            msg = enc.GetBytes(lastMessage.header + lastMessage.msgID + lastMessage.msg);
            
            sck.Send(msg);

            conversationList.RemoveAt(conversationList.Count - 1);
            Control c = flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1];
            c.Dispose();

            //Reload_Own_Text();
            if (conversationList[conversationList.Count - 1].read == true)
            {
                deleteLast.Visible = false;
            }
            Display_Dictionary();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            try
            {
                TMessage activatedObject = new TMessage();
                activatedObject.header = "0005";
                activatedObject.msg = "ACTIVATED";
                activatedObject.read = false;
                activatedObject.sender = "Me";

                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] msg = new byte[1500];

                msg = enc.GetBytes(activatedObject.header + activatedObject.msgID + activatedObject.msg);
                sck.Send(msg);

            }
            catch (SocketException ex)
            {
                //sck.Close();
                //sck.Connect(epRemote);
                // MessageBox.Show(ex.ToString());
            }            
            
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            try
            {
                TMessage activatedObject = new TMessage();
                activatedObject.header = "0005";
                activatedObject.msg = "DEACTIVATED";
                activatedObject.read = false;
                activatedObject.sender = "Me";

                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] msg = new byte[1500];

                msg = enc.GetBytes(activatedObject.header + activatedObject.msgID + activatedObject.msg);
                sck.Send(msg);

            }
            catch (SocketException ex)
            {
                //Do Nothing
                //sck.Close();
                //sck.Connect(epRemote);
                // MessageBox.Show(ex.ToString());
            } 
        }
        
        //Function that when the other client becomes active, it goes through the your conversation list and says all the messages have been read
        private void Change_All_To_Read()
        {
            foreach(var tMessage in conversationList)
            {
                tMessage.read = true;
            }
            
        }
        
        //display the conversationList for debugging purposes
        private void Display_Dictionary()
        {
            listBox2.Invoke((Action)delegate
           {
               listBox2.Items.Clear();
               foreach(var tMessage in conversationList)
               {
                   listBox2.Items.Add(tMessage.header + " " + tMessage.msgID + " " + tMessage.msg + " " + tMessage.read);
               }
           });
        }
         
        //What to do if the label6 changes, this is what changes the picture and the delete Last button
        private void label6_TextChanged(object sender, EventArgs e)
        {
            if(label6.Text == "Activated")
            {
                pictureBox1.Image = Properties.Resources.greenlight2;
                label5.Text = "Active";
                deleteLast.Visible = false;
                Change_All_To_Read();
                Display_Dictionary();
            }
            else if(label6.Text == "Deactivated")
            {
                pictureBox1.Image = Properties.Resources.yellowlight;
                label5.Text = "Not Looking";
            }

        }
        
        //Don't need this
       
        
       //dont need this either
        private void richTextBox1_Enter(object sender, EventArgs e)
        {
            textMessage.Focus();
            //System.Windows.Forms.SendKeys.Send("{tab}");
        }

        private void flowLayoutPanel1_Enter(object sender, EventArgs e)
        {
            textMessage.Focus();
        }

       
    }

    
}
