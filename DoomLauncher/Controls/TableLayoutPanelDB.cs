﻿using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class TableLayoutPanelDB : TableLayoutPanel
    {
        public TableLayoutPanelDB()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }
    }
}
