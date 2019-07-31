﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class ProgressBarForm : Form
    {
        public event EventHandler Cancelled;

        public ProgressBarForm()
        {
            InitializeComponent();

            this.Load += ProgressBarForm_Load;
        }

        void ProgressBarForm_Load(object sender, EventArgs e)
        {
            if (Owner != null)
            {
                Location = new Point(Owner.Location.X + Owner.Width / 2 - Width / 2,
                    Owner.Location.Y + Owner.Height / 2 - Height / 2);
            }
        }

        public void SetCancelAllowed(bool set)
        {
            btnCancel.Visible = set;
        }

        public ProgressBarStyle ProgressBarStyle
        {
            get { return progressBar1.Style; }
            set { progressBar1.Style = value; }
        }

        public int Value
        {
            get { return progressBar1.Value; }
            set { progressBar1.Value = value; }
        }

        public int Minimum
        {
            get { return progressBar1.Minimum; }
            set { progressBar1.Minimum = value; }
        }

        public int Maximum
        {
            get { return progressBar1.Maximum; }
            set { progressBar1.Maximum = value; }
        }

        public string DisplayText
        {
            get { return lblProcess.Text; }
            set { lblProcess.Text = value; }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (Cancelled != null)
            {
                Cancelled(this, new EventArgs());
            }
        }
    }
}
