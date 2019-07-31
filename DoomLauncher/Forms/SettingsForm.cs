﻿using DoomLauncher.DataSources;
using DoomLauncher.Forms;
using DoomLauncher.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class SettingsForm : Form
    {
        private List<Tuple<IConfigurationData, object>> m_configValues = new List<Tuple<IConfigurationData, object>>();
        private TextBox m_gameFileDirectory, m_screenshotDirectories;
        private Label m_lblScreenshotWidth;

        private readonly IDataSourceAdapter m_adapter;
        private readonly AppConfiguration m_appConfig;

        public SettingsForm(IDataSourceAdapter adapter, AppConfiguration appConfig)
        {
            InitializeComponent();

            lblLaunchSettings.Text = string.Concat("These are the default settings for a game file", Environment.NewLine, 
                " that does not have a specific configuration saved.");

            m_adapter = adapter;
            m_appConfig = appConfig;

            PopulateDefaultSettings(m_adapter);
            PopulateConfiguration();
        }

        public void SetToLaunchSettingsTab()
        {
            tabControl.SelectedIndex = 1;
        }

        public void SetCancelAllowed(bool set)
        {
            btnCancel.Visible = set;
            ControlBox = set;
        }

        private void PopulateConfiguration()
        {
            IEnumerable<IConfigurationData> configItems = m_adapter.GetConfiguration().Where(x => x.UserCanModify);

            TableLayoutPanel tblMain = new TableLayoutPanel();
            tblMain.Dock = DockStyle.Top;
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            int height = 8;

            foreach (IConfigurationData config in configItems)
            {
                GrowLabel lbl = new GrowLabel();
                lbl.Anchor = AnchorStyles.Left;
                lbl.Text = AddSpaceBetweenWords(config.Name);

                tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, lbl.Height < 32 ? 32 : lbl.Height));
                tblMain.Controls.Add(lbl, 0, tblMain.RowStyles.Count - 1);

                if (!string.IsNullOrEmpty(config.AvailableValues))
                    HandleComboBox(tblMain, config);
                else if (config.Name == "ScreenshotPreviewSize") //special case for TrackBar
                    HandleScreenshotPreviewSize(tblMain, config);
                else
                    HandleTextBox(tblMain, config);

                height += 32;
            }

            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tblMain.Height = height;

            tabControl.TabPages[0].Controls.Add(tblMain);
            Height = height + 110;
        }

        private void HandleTextBox(TableLayoutPanel tblMain, IConfigurationData config)
        {
            TextBox txt = new TextBox();
            txt.Dock = DockStyle.Fill;
            txt.Text = config.Value;
            m_configValues.Add(new Tuple<IConfigurationData, object>(config, txt));

            if (config.Name == "GameFileDirectory")
                HandleGameFileDirectory(tblMain, txt);
            else if (config.Name == "ScreenshotCaptureDirectories")
                HandleScreenshotCaptureDirectories(tblMain, txt);
            else
                tblMain.Controls.Add(txt, 1, tblMain.RowStyles.Count - 1);
        }

        private void HandleComboBox(TableLayoutPanel tblMain, IConfigurationData config)
        {
            ComboBox cmb = new ComboBox();
            cmb.Dock = DockStyle.Fill;

            string[] items = config.AvailableValues.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<Tuple<string, string>> cmbDataSource = new List<Tuple<string, string>>();

            for (int i = 0; i < items.Length - 1; i += 2)
                cmbDataSource.Add(new Tuple<string, string>(items[i], items[i + 1]));

            cmb.ValueMember = "Item1";
            cmb.DataSource = cmbDataSource;
            cmb.BindingContext = new BindingContext();
            cmb.SelectedItem = cmbDataSource.FirstOrDefault(x => x.Item2 == config.Value);

            tblMain.Controls.Add(cmb, 1, tblMain.RowStyles.Count - 1);
            m_configValues.Add(new Tuple<IConfigurationData, object>(config, cmb));
        }

        private void HandleScreenshotPreviewSize(TableLayoutPanel tblMain, IConfigurationData config)
        {
            m_lblScreenshotWidth = new Label();
            m_lblScreenshotWidth.Width = 68;
            m_lblScreenshotWidth.Height = 16;
            m_lblScreenshotWidth.Margin = new Padding(0, 8, 0, 0);

            TrackBar trk = new TrackBar();
            trk.Minimum = -8;
            trk.Maximum = 8;
            trk.ValueChanged += Trk_ValueChanged;
            trk.Value = Convert.ToInt32(config.Value);
            trk.Width = 200;

            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Fill;
            flp.Controls.Add(trk);
            flp.Controls.Add(m_lblScreenshotWidth);

            tblMain.Controls.Add(flp, 1, tblMain.RowStyles.Count - 1);
            m_configValues.Add(new Tuple<IConfigurationData, object>(config, trk));
        }

        private void Trk_ValueChanged(object sender, EventArgs e)
        {
            m_lblScreenshotWidth.Text = string.Concat("Width: ", Util.GetPreviewScreenshotWidth(((TrackBar)sender).Value));
        }

        private void HandleScreenshotCaptureDirectories(TableLayoutPanel tblMain, TextBox txt)
        {
            m_screenshotDirectories = txt;
            m_screenshotDirectories.Width = 190;
            m_screenshotDirectories.Enabled = false;
            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Fill;
            flp.Controls.Add(txt);
            Button changeButton = new Button();
            changeButton.Text = "Change...";
            changeButton.Click += ChangeButton_Click;
            flp.Controls.Add(changeButton);
            tblMain.Controls.Add(flp, 1, tblMain.RowStyles.Count - 1);
        }

        private void HandleGameFileDirectory(TableLayoutPanel tblMain, TextBox txt)
        {
            m_gameFileDirectory = txt;
            m_gameFileDirectory.Width = 190;
            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Fill;
            flp.Controls.Add(m_gameFileDirectory);
            Button browseButton = new Button();
            browseButton.Text = "Browse...";
            browseButton.Click += browseButton_Click;
            flp.Controls.Add(browseButton);
            tblMain.Controls.Add(flp, 1, tblMain.RowStyles.Count - 1);
        }

        void browseButton_Click(object sender, EventArgs e)
        {
            LauncherPath path = new LauncherPath(m_gameFileDirectory.Text);
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = path.GetFullPath();
            
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                path = new LauncherPath(dialog.SelectedPath);
                m_gameFileDirectory.Text = path.GetPossiblyRelativePath();
            }
        }

        private void ChangeButton_Click(object sender, EventArgs e)
        {
            DirectoriesForm form = new DirectoriesForm();
            form.SetDirectories(m_screenshotDirectories.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            form.StartPosition = FormStartPosition.CenterParent;

            if (form.ShowDialog(this) == DialogResult.OK)
                m_screenshotDirectories.Text = string.Join(";", form.GetDirectories());
        }

        private void PopulateDefaultSettings(IDataSourceAdapter adapter)
        {
            cmbSourcePorts.DataSource = adapter.GetSourcePorts();
            cmbIwad.DataSource = Util.GetIWadsDataSource(adapter);
            cmbSkill.DataSource = Util.GetSkills();

            cmbSourcePorts.SelectedValue = m_appConfig.GetTypedConfigValue(ConfigType.DefaultSourcePort, typeof(int));
            cmbIwad.SelectedValue = m_appConfig.GetTypedConfigValue(ConfigType.DefaultIWad, typeof(int));
            cmbSkill.SelectedItem = m_appConfig.GetTypedConfigValue(ConfigType.DefaultSkill, typeof(string));
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            foreach (Tuple<IConfigurationData, object> config in m_configValues)
            {
                config.Item1.Value = GetValue(config.Item1, config.Item2);
                m_adapter.UpdateConfiguration(config.Item1);
            }

            HandleLaunchSettings();
            Close();
        }

        private void HandleLaunchSettings()
        { 
            string[] configNames = new[]
            {  
                ConfigType.DefaultSourcePort.ToString("g"), 
                ConfigType.DefaultIWad.ToString("g"), 
                ConfigType.DefaultSkill.ToString("g") 
            };
            string[] configValues = new[]
            { 
                cmbSourcePorts.SelectedItem == null ? null : ((ISourcePortData)cmbSourcePorts.SelectedItem).SourcePortID.ToString(),
                cmbIwad.SelectedItem == null ? null : ((IIWadData)cmbIwad.SelectedItem).IWadID.ToString(),
                cmbSkill.SelectedItem == null ? null : cmbSkill.SelectedItem.ToString()
            };

            IEnumerable<IConfigurationData> configuration = m_adapter.GetConfiguration().Where(x => configNames.Contains(x.Name));

            for (int i = 0; i < configNames.Length; i++)
            {
                string configName = configNames[i];
                string configValue = configValues[i];
                IConfigurationData config = configuration.FirstOrDefault(x => x.Name == configName);

                if (configValue != null)
                {
                    if (config == null)
                    {
                        config = CreateConfig(configName, configValue);
                        m_adapter.InsertConfiguration(config);
                    }
                    else
                    {
                        config.Value = configValue;
                        m_adapter.UpdateConfiguration(config);
                    }
                }
            }
        }

        private static IConfigurationData CreateConfig(string configName, string configValue)
        {
            ConfigurationData config = new ConfigurationData();
            config.Name = configName;
            config.Value = configValue;
            config.UserCanModify = false;
            config.AvailableValues = string.Empty;
            return config;
        }

        private string GetValue(IConfigurationData config, object value)
        {
            if (value is ComboBox && !string.IsNullOrEmpty(config.AvailableValues))
            {
                 ComboBox cmb = value as ComboBox;

                if (cmb != null && cmb.SelectedItem != null)
                {
                    Tuple<string, string> item = cmb.SelectedItem as Tuple<string, string>;
                    return item.Item2;
                }
            }
            else if (value is TrackBar)
            {
                TrackBar trk = value as TrackBar;
                return trk.Value.ToString();
            }
            else if (value is TextBox)
            {
                TextBox txt = value as TextBox;
                return txt.Text;
            }

            return string.Empty;
        }

        private static string AddSpaceBetweenWords(string item)
        {
            for (int i = 0; i < item.Length; i++)
            {
                if (char.IsUpper(item[i]) && i != 0)
                {
                    item = item.Insert(i, " ");
                    i++;
                }
            }

            return item;
        }
    }
}
