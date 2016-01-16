using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_client
{
    public partial class Debug : Form
    {
        public Debug(string _text)
        {
            InitializeComponent();
            DisplayRTF(_text);
        }

        private void DisplayRTF(string _text)
        {
            richTextBox2.Text = _text;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
