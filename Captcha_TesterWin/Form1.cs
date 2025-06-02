using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Captcha_TesterWin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = new Captcha().Generate();
            pictureBox1.Image = Image.FromStream(new MemoryStream(result.Content));
        }
    }
}
