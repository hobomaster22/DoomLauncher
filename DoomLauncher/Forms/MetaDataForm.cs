﻿using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class MetaDataForm : Form
    {
        public MetaDataForm()
        {
            InitializeComponent();
            gameFileEdit1.SetShowCheckBoxes(true);
        }

        public GameFileEdit GameFileEdit
        {
            get { return gameFileEdit1; }
        }
    }
}
