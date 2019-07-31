﻿using DoomLauncher.DataSources;
using DoomLauncher.Interfaces;
using Equin.ApplicationFramework;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class FileSelectForm : Form
    {
        private TabHandler m_tabHandler;
        private bool m_bOverrideInit, m_multiSelect;

        public FileSelectForm()
        {
            InitializeComponent();
            SetupSearchFilters();
            m_multiSelect = m_bOverrideInit = false;
            ctrlSearch.SearchTextKeyPreviewDown += CtrlSearch_SearchTextKeyPreviewDown;
        }

        private void CtrlSearch_SearchTextKeyPreviewDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                GameFileViewControl ctrl = CurrentGameFileControl;
                if (ctrl != null)
                    HandleSearch(ctrl);
            }
        }

        public void Initialize(IDataSourceAdapter adapter, ITabView tabView, IEnumerable<IGameFile> gameFilesBind)
        {
            Initialize(adapter, new[] { tabView });
            m_tabHandler.TabViews[0].SetGameFilesData(gameFilesBind);
            m_bOverrideInit = true;
        }

        public void Initialize(IDataSourceAdapter adapter, IEnumerable<ITabView> views)
        {
            DataSourceAdapter = adapter;
            m_tabHandler = new TabHandler(tabControl);

            foreach(ITabView view in views)
            {
                ITabView viewAdd = (ITabView)view.Clone();
                viewAdd.GameFileViewControl.MultiSelect = MultiSelect;
                viewAdd.GameFileViewControl.RowDoubleClicked += GameFileViewControl_RowDoubleClicked;
                m_tabHandler.AddTab(viewAdd);
            }
        }

        private void GameFileViewControl_RowDoubleClicked(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        public bool MultiSelect
        {
            get { return m_multiSelect;  }
            set
            {
                m_multiSelect = value;
                if (m_tabHandler != null)
                {
                    foreach(ITabView view in m_tabHandler.TabViews)
                    {
                        view.GameFileViewControl.MultiSelect = MultiSelect;
                    }
                }
            }
        }

        public ITabView[] TabViews
        {
            get { return m_tabHandler.TabViews; }
        }

        public void SetShowCancel(bool set)
        {
            btnCancel.Visible = set;
        }

        public void SetDisplayText(string text)
        {
            lblText.Text = text;
        }

        public void ShowSearchControl(bool set)
        {
            ctrlSearch.Visible = set;
            btnSearch.Visible = set;
        }

        protected override void OnShown(EventArgs e)
        {
            foreach (ITabView view in m_tabHandler.TabViews)
            {
                HandleSearch(view.GameFileViewControl);
            }
        }

        private void SetupSearchFilters()
        {
            Util.SetDefaultSearchFields(ctrlSearch);
        }

        public IDataSourceAdapter DataSourceAdapter { get; set; }

        public IGameFile[] SelectedFiles
        {
            get
            {
                object[] items = CurrentGameFileControl.SelectedItems;

                List<IGameFile> ret = new List<IGameFile>(items.Length);

                foreach (object obj in items)
                {
                    ret.Add(((ObjectView<GameFile>)obj).Object as IGameFile);
                }

                return ret.ToArray();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            GameFileViewControl ctrl = CurrentGameFileControl;

            if (ctrl != null)
            {
                HandleSearch(ctrl);
            }
        }

        private void HandleSearch(GameFileViewControl ctrl)
        {
            if (!m_bOverrideInit)
            {
                ITabView view = m_tabHandler.TabViewForControl(ctrl);

                if (view != null)
                {
                    if (!string.IsNullOrEmpty(ctrlSearch.SearchText))
                        view.SetGameFiles(Util.SearchFieldsFromSearchCtrl(ctrlSearch));
                    else
                        view.SetGameFiles();
                }
            }
        }

        private GameFileViewControl CurrentGameFileControl
        {
            get
            {
                ITabView view = tabControl.SelectedTab.Controls[0] as ITabView;

                if (view != null)
                    return view.GameFileViewControl;

                return null;
            }
        }
    }
}
