﻿using System.Diagnostics;
using System.Windows.Forms;

namespace DoomLauncher.Forms
{
    public partial class Welcome : Form
    {
        public Welcome()
        {
            InitializeComponent();
        }

        private void lnkHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("Help.pdf");
            }
            catch
            {
                MessageBox.Show(this, "Could not open the help file. Did you copy all the files to your Doom Launcher folder correctly?", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
