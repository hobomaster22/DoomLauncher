﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class SearchControl : UserControl
    {
        private string[] m_items = new string[] { };

        public event EventHandler SearchTextChanged;
        public event PreviewKeyDownEventHandler SearchTextKeyPreviewDown;

        public SearchControl()
        {
            InitializeComponent();

            txtSearch.TextChanged += txtSearch_TextChanged;
            txtSearch.PreviewKeyDown += TxtSearch_PreviewKeyDown;
        }

        private void TxtSearch_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (SearchTextKeyPreviewDown != null)
                SearchTextKeyPreviewDown(this, e);
        }

        void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (SearchTextChanged != null)
                SearchTextChanged(this, e);
        }

        public void SetSearchFilter(string item, bool check)
        {
            cmbFilter.CheckBoxItems[item].Checked = check;
        }

        public bool GetSearchFilter(string item)
        {
            return cmbFilter.CheckBoxItems[item].Checked;
        }

        public string[] GetSearchFilters()
        {
            return m_items.ToArray();
        }

        public string[] GetSelectedSearchFilters()
        {
            return m_items.Where(x => cmbFilter.CheckBoxItems[x].Checked).ToArray();
        }

        public void SetSearchFilters(IEnumerable<string> items)
        {
            cmbFilter.Items.Clear();
            m_items = items.ToArray();

            foreach (string item in items)
            {
                cmbFilter.Items.Add(item);
            }
        }

        public string SearchText
        {
            get { return txtSearch.Text; }
            set { txtSearch.Text = value; }
        }
    }
}
