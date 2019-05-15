using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoChat
{
    public partial class login : Form
    {
        public login()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Data.Value = textBox1.Text;
                Form1 fr = new Form1(this);
                this.Hide();
                fr.Show();
            }
            else
                MessageBox.Show("Pleace enter login", "Login problemm");            
        }
    }
}
