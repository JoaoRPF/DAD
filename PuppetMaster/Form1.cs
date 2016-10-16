using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DADSTORM;

namespace DADSTORM
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void execFileClick(object sender, EventArgs e)
        {
            string result = PuppetMaster.services.printHello();
            logText.Text = result;
        }
    }
}
