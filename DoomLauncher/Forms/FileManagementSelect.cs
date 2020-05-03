﻿using System.Windows.Forms;

namespace DoomLauncher.Forms
{
    public partial class FileManagementSelect : Form
    {
        public FileManagementSelect()
        {
            InitializeComponent();
            cmbFileManagement.DataSource = new string[] { "Managed", "Static Path" };
        }

        public FileManagement GetSelectedFileManagement()
        {
            if (cmbFileManagement.SelectedIndex == 0)
                return FileManagement.Managed;

            return FileManagement.StaticPath;
        }

        private void BtnOK_Click(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}