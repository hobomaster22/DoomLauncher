﻿using DoomLauncher.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class FileDetailsEditForm : Form
    {
        public FileDetailsEditForm()
        {
            InitializeComponent();
        }

        public void Initialize(IDataSourceAdapter adapter)
        {
            Initialize(adapter, null);
        }

        public void Initialize(IDataSourceAdapter adapter, IFileData file)
        {
            DataSourceAdapter = adapter;

            cmbSourcePort.DisplayMember = "Name";
            cmbSourcePort.ValueMember = "SourcePortID";
            cmbSourcePort.DataSource = adapter.GetSourcePorts();

            if (file != null)
            {
                cmbSourcePort.SelectedValue = file.SourcePortID;
                txtDescription.Text = file.Description;
            }
        }

        public void ShowDescription(bool set)
        {
            if (set)
            {
                txtDescription.Visible = true;
                Height = 300;
                MaximumSize = new Size(400, Height);
            }
            else
            {
                txtDescription.Visible = false;
                Height = 104;
                MaximumSize = new Size(400, Height);
            }
        }

        public ISourcePortData SourcePort
        {
            get { return cmbSourcePort.SelectedItem as ISourcePortData; }
            set { cmbSourcePort.SelectedValue = value.SourcePortID; }
        }

        public string Description
        {
            get { return txtDescription.Text; }
            set { txtDescription.Text = value; }
        }

        public IDataSourceAdapter DataSourceAdapter { get; set; }
    }
}
