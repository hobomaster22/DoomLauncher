﻿using DoomLauncher.Forms;
using DoomLauncher.Interfaces;
using DoomLauncher.SourcePort;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class MainForm
    {
        private void HandlePlay()
        {
            if (GetCurrentViewControl() != null)
                HandlePlay(SelectedItems(GetCurrentViewControl()));
        }

        private bool AssertFile(string file)
        {
            FileInfo fi = new FileInfo(file);

            if (!fi.Exists)
                MessageBox.Show(this, string.Format("The file {0} does not exist.", file), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return fi.Exists;
        }

        private void HandlePlay(IEnumerable<IGameFile> gameFiles)
        {
            HandlePlay(gameFiles, null);
        }

        private void HandlePlay(IEnumerable<IGameFile> gameFiles, ISourcePortData sourcePort)
        {
            LaunchData launchData = GetLaunchFiles(gameFiles);

            if (launchData.Success)
            {
                if (launchData.GameFile == null)
                {
                    var iwad = DataSourceAdapter.GetIWad((int)AppConfiguration.GetTypedConfigValue(ConfigType.DefaultIWad, typeof(int)));
                    if (iwad != null)
                    {
                        GameFileGetOptions options = new GameFileGetOptions(new GameFileSearchField(GameFileFieldType.GameFileID, iwad.GameFileID.Value.ToString()));
                        launchData.GameFile = DataSourceAdapter.GetGameFiles(options).FirstOrDefault();
                    }
                }

                if (launchData.GameFile != null)
                {
                    SetupPlayForm(launchData.GameFile);
                    if (sourcePort != null) m_currentPlayForm.SelectedSourcePort = sourcePort;

                    if (m_currentPlayForm.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            HandlePlaySettings(m_currentPlayForm, launchData.GameFile);
                            if (m_currentPlayForm.SelectedSourcePort != null)
                                m_playInProgress = StartPlay(launchData.GameFile, m_currentPlayForm.SelectedSourcePort, m_currentPlayForm.ScreenFilter);
                        }
                        catch (IOException)
                        {
                            MessageBox.Show(this, "The file is in use and cannot be launched. Please close any programs that may be using the file and try again.", "File In Use",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        HandleSelectionChange(GetCurrentViewControl(), true);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(launchData.ErrorTitle))
            {
                MessageBox.Show(this, launchData.ErrorTitle, launchData.ErrorDescription, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private LaunchData GetLaunchFiles(IEnumerable<IGameFile> gameFiles)
        {
            IGameFile gameFile = null;

            if (gameFiles != null)
            {
                if (gameFiles.Count() > 1)
                {
                    bool accepted;
                    gameFile = PromptUserMainFile(gameFiles, out accepted);  //ask user which file to tie all stats to
                    if (!accepted)
                        return new LaunchData(string.Empty, string.Empty);
                }
                else
                {
                    gameFile = gameFiles.FirstOrDefault();
                }
            }

            if (m_playInProgress)
                return new LaunchData("Already Playing", "There is already a game in progress. Please exit that game first.");

            if (!DataSourceAdapter.GetSourcePorts().Any())
                return new LaunchData("No Source Ports", "You must have at least one source port configured to play! Click the settings menu on the top left and select Source Ports to configure.");

            if (!DataSourceAdapter.GetIWads().Any())
                return new LaunchData("No IWADs", "You must have at least one IWAD configured to play! Click the settings menu on the top left and select IWads to configure.");

            if (gameFile != null && GetCurrentViewControl() != null)
            {
                ITabView tabView = m_tabHandler.TabViewForControl(GetCurrentViewControl());
                if (tabView != null) //there's a reason for doing this that I can't remember...
                    gameFile = DataSourceAdapter.GetGameFile(gameFile.FileName); //need to retrieve all the data for the file

                if (gameFiles.Count() > 1) //for when the user selected more than one file
                {
                    HandleMultiSelectPlay(gameFile, gameFiles.Except(new[] { gameFile })); //sets SettingsFiles with all the other game files
                    List<IGameFile> gameFilesList = new List<IGameFile>();
                    gameFilesList.Add(gameFile);
                    Array.ForEach(gameFiles.Skip(1).ToArray(), x => gameFilesList.Add(x));
                    gameFiles = gameFilesList;
                }
            }

            return new LaunchData(gameFile, gameFiles);
        }

        private IGameFile PromptUserMainFile(IEnumerable<IGameFile> gameFiles, out bool accepted)
        {
            accepted = false;

            FileSelectForm form = new FileSelectForm();
            ITabView tabView = m_tabHandler.TabViews.FirstOrDefault(x => x.Key.Equals(s_localKey));
            form.Initialize(DataSourceAdapter, tabView, gameFiles);
            form.StartPosition = FormStartPosition.CenterParent;
            form.SetDisplayText("Please select the main file that all data will be associated with. (Screenshots, demos, save games, etc.)");
            form.MultiSelect = false;
            form.ShowSearchControl(false);

            if (form.ShowDialog(this) == DialogResult.OK && form.SelectedFiles.Length > 0)
            {
                accepted = true;
                return form.SelectedFiles[0];
            }

            return gameFiles.First();
        }

        private void HandleMultiSelectPlay(IGameFile firstGameFile, IEnumerable<IGameFile> gameFiles)
        {
            //If the user selected multiple files
            //Take all the files after the first and set them as additional files to the first

            StringBuilder sbAdditionalFiles = new StringBuilder();

            foreach(IGameFile gameFile in gameFiles)
            {
                sbAdditionalFiles.Append(gameFile.FileName);
                sbAdditionalFiles.Append(';');
            }

            firstGameFile.SettingsFiles = sbAdditionalFiles.ToString();
        }

        private void HandlePlaySettings(PlayForm form, IGameFile gameFile)
        {
            if (form.RememberSettings && gameFile != null)
            {
                gameFile.SourcePortID = gameFile.IWadID = null;

                if (form.SelectedSourcePort != null) gameFile.SourcePortID = form.SelectedSourcePort.SourcePortID;
                if (form.SelectedIWad != null) gameFile.IWadID = form.SelectedIWad.IWadID;

                if (form.SelectedMap != null) gameFile.SettingsMap = form.SelectedMap;
                else gameFile.SettingsMap = string.Empty; //this setting can be turned off

                if (form.SelectedSkill != null) gameFile.SettingsSkill = form.SelectedSkill;
                if (form.ExtraParameters != null) gameFile.SettingsExtraParams = form.ExtraParameters;

                gameFile.SettingsStat = form.SaveStatistics;

                if (form.ShouldSaveAdditionalFiles())
                {
                    gameFile.SettingsFiles = string.Join(";", form.GetAdditionalFiles().Select(x => x.FileName).ToArray());
                    gameFile.SettingsFilesIWAD = string.Join(";", form.GetIWadAdditionalFiles().Select(x => x.FileName).ToArray());
                    gameFile.SettingsFilesSourcePort = string.Join(";", form.GetSourcePortAdditionalFiles().Select(x => x.FileName).ToArray());

                    if (form.SpecificFiles != null)
                        gameFile.SettingsSpecificFiles = string.Join(";", form.SpecificFiles);
                    else
                        gameFile.SettingsSpecificFiles = string.Empty; //this setting can be turned off
                }

                DataSourceAdapter.UpdateGameFile(gameFile, new[] { GameFileFieldType.SourcePortID, GameFileFieldType.IWadID, GameFileFieldType.SettingsMap, 
                    GameFileFieldType.SettingsSkill, GameFileFieldType.SettingsFiles, GameFileFieldType.SettingsExtraParams, GameFileFieldType.SettingsSpecificFiles, GameFileFieldType.SettingsStat,
                    GameFileFieldType.SettingsFilesIWAD, GameFileFieldType.SettingsFilesSourcePort });
            }
        }

        private void SetupPlayForm(IGameFile gameFile)
        {
            m_currentPlayForm = new PlayForm(AppConfiguration, DataSourceAdapter);
            m_currentPlayForm.SaveSettings += m_currentPlayForm_SaveSettings;
            m_currentPlayForm.OnPreviewLaunchParameters += m_currentPlayForm_OnPreviewLaunchParameters;
            m_currentPlayForm.StartPosition = FormStartPosition.CenterParent;

            List<ITabView> views = GetAdditionalTabViews();

            if (gameFile != null)
                gameFile = DataSourceAdapter.GetGameFile(gameFile.FileName); //this file came from the grid, which does not have all info populated to save perfomance

            m_currentPlayForm.Initialize(views, gameFile);

            SetDefaultSelections();

            if (gameFile != null)
            {
                IIWadData iwad = DataSourceAdapter.GetIWad(gameFile.GameFileID.Value);

                if (iwad != null)
                    m_currentPlayForm.SelectedIWad = gameFile;

                if (gameFile.SourcePortID.HasValue)
                    m_currentPlayForm.SelectedSourcePort = DataSourceAdapter.GetSourcePort(gameFile.SourcePortID.Value);

                if (gameFile.IWadID.HasValue)
                {
                    m_currentPlayForm.SelectedIWad = gameFile;
                    m_currentPlayForm.SelectedIWad = DataSourceAdapter.GetGameFileIWads().FirstOrDefault(x => x.IWadID == gameFile.IWadID);
                }

                if (!string.IsNullOrEmpty(gameFile.SettingsMap)) m_currentPlayForm.SelectedMap = gameFile.SettingsMap;
                if (!string.IsNullOrEmpty(gameFile.SettingsSkill)) m_currentPlayForm.SelectedSkill = gameFile.SettingsSkill;
                if (!string.IsNullOrEmpty(gameFile.SettingsExtraParams)) m_currentPlayForm.ExtraParameters = gameFile.SettingsExtraParams;
                if (!string.IsNullOrEmpty(gameFile.SettingsSpecificFiles)) m_currentPlayForm.SpecificFiles = gameFile.SettingsSpecificFiles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            m_currentPlayForm.InitializeComplete();
        }

        private void m_currentPlayForm_OnPreviewLaunchParameters(object sender, EventArgs e)
        {
            GameFilePlayAdapter playAdapter = CreatePlayAdapter(m_currentPlayForm, playAdapter_ProcessExited, AppConfiguration);
            playAdapter.ExtractFiles = false;
            string err;
            if (m_currentPlayForm.SettingsValid(out err))
                ShowLaunchParameters(playAdapter, m_currentPlayForm.GameFile, m_currentPlayForm.SelectedSourcePort);
            else
                MessageBox.Show(this, err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void m_currentPlayForm_SaveSettings(object sender, EventArgs e)
        {
            HandlePlaySettings(m_currentPlayForm, m_currentPlayForm.GameFile);
        }

        private List<ITabView> GetAdditionalTabViews()
        {
            List<ITabView> views = new List<ITabView>();
            views.AddRange(m_tabHandler.TabViews.Where(x => x.Title == s_localKey));
            views.AddRange(m_tabHandler.TabViews.Where(x => x is TagTabView));
            return views;
        }

        private void SetDefaultSelections()
        {
            int port = (int)AppConfiguration.GetTypedConfigValue(ConfigType.DefaultSourcePort, typeof(int));
            int iwad = (int)AppConfiguration.GetTypedConfigValue(ConfigType.DefaultIWad, typeof(int));
            string skill = (string)AppConfiguration.GetTypedConfigValue(ConfigType.DefaultSkill, typeof(string));

            ISourcePortData sourcePort = DataSourceAdapter.GetSourcePorts().FirstOrDefault(x => x.SourcePortID == port);
            if (sourcePort != null) 
                m_currentPlayForm.SelectedSourcePort = sourcePort;

            IIWadData iwadSource =  DataSourceAdapter.GetIWads().FirstOrDefault(x => x.IWadID == Convert.ToInt32(iwad));
            if (iwadSource != null)
            {
                GameFileGetOptions options = new GameFileGetOptions(new GameFileSearchField(GameFileFieldType.GameFileID, iwadSource.GameFileID.Value.ToString()));
                IEnumerable<IGameFile> gameFileIwad = DataSourceAdapter.GetGameFiles(options);
                if (gameFileIwad.Any())
                    m_currentPlayForm.SelectedIWad = gameFileIwad.First();
            }

            if (skill != null) 
                m_currentPlayForm.SelectedSkill = skill;
        }

        private DateTime m_dtStartPlay;
        private IGameFile m_currentPlayFile;
        private FilterForm m_filterForm;

        private bool StartPlay(IGameFile gameFile, ISourcePortData sourcePort, bool screenFilter)
        {
            GameFilePlayAdapter playAdapter = CreatePlayAdapter(m_currentPlayForm, playAdapter_ProcessExited, AppConfiguration);
            m_saveGames = new IFileData[] { };

            CopySaveGames(gameFile, sourcePort);
            CreateFileDetectors(sourcePort);

            if (m_currentPlayForm.PreviewLaunchParameters)
                ShowLaunchParameters(playAdapter, gameFile, sourcePort);

            bool isGameFileIwad = IsGameFileIwad(gameFile);

            if (m_currentPlayForm.SaveStatistics)
                SetupStatsReader(sourcePort, gameFile);

            if (playAdapter.Launch(AppConfiguration.GameFileDirectory, AppConfiguration.TempDirectory, 
                gameFile, sourcePort, isGameFileIwad))
            {
                m_currentPlayFile = gameFile;

                if (gameFile != null)
                {
                    gameFile.LastPlayed = DateTime.Now;
                    m_dtStartPlay = DateTime.Now;
                    DataSourceAdapter.UpdateGameFile(gameFile, new[] { GameFileFieldType.LastPlayed });
                    UpdateDataSourceViews(gameFile);
                }
            }
            else
            {
                MessageBox.Show(this, playAdapter.LastError, "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (screenFilter)
            {
                m_filterForm = new FilterForm(Screen.FromControl(this), m_currentPlayForm.GetFilterSettings());
                m_filterForm.Show(this);
            }

            return true;
        }

        private void CopySaveGames(IGameFile gameFile, ISourcePortData sourcePort)
        {
            if (gameFile != null) //BUG: what if it's iwad?
            {
                HandleCopySaveGames(gameFile, sourcePort);
            }
            else if (IsGameFileIwad(gameFile))
            {
                gameFile = GetGameFileForIWad(gameFile);
                HandleCopySaveGames(gameFile, sourcePort);
            }
        }

        private void HandleCopySaveGames(IGameFile gameFile, ISourcePortData sourcePort)
        {
            m_saveGames = DataSourceAdapter.GetFiles(gameFile, FileType.SaveGame).ToArray();
            SaveGameHandler saveGameHandler = new SaveGameHandler(DataSourceAdapter, AppConfiguration.SaveGameDirectory);
            saveGameHandler.CopySaveGamesToSourcePort(sourcePort, m_saveGames);
        }

        private void ShowLaunchParameters(GameFilePlayAdapter playAdapter, IGameFile gameFile, ISourcePortData sourcePort)
        {
            TextBoxForm form = new TextBoxForm();
            form.Text = "Launch Parameters";
            form.StartPosition = FormStartPosition.CenterParent;

            string launchParameters = playAdapter.GetLaunchParameters(AppConfiguration.GameFileDirectory,
                AppConfiguration.TempDirectory, gameFile, sourcePort, IsGameFileIwad(gameFile));

            if (launchParameters != null)
            {
                launchParameters = launchParameters.Replace(@" -", string.Concat(Environment.NewLine, " -"));
                launchParameters = launchParameters.Replace("\" \"", string.Concat("\"", Environment.NewLine, " \""));
                if (launchParameters.StartsWith(Environment.NewLine)) launchParameters = launchParameters.Substring(Environment.NewLine.Length);
                string individualFiles = string.Empty;

                if (m_currentPlayForm.SpecificFiles != null && m_currentPlayForm.SpecificFiles.Length > 0)
                    individualFiles = Environment.NewLine + string.Format("Selected Files: {0}", string.Join(", ", m_currentPlayForm.SpecificFiles));

                string sourcePortParams = string.Empty;
                if (!string.IsNullOrEmpty(sourcePort.ExtraParameters))
                    sourcePortParams = string.Concat(Environment.NewLine, Environment.NewLine, "Paramters from source port: ", sourcePort.ExtraParameters);

                form.DisplayText = string.Concat(launchParameters, Environment.NewLine, Environment.NewLine, 
                    string.Format("Supported Extensions: {0}", sourcePort.SupportedExtensions),
                    individualFiles,
                    sourcePortParams,
                    Environment.NewLine, Environment.NewLine,
                    "*** If files appear to be missing check the 'Select Individual Files' option and supported extensions options in the Source Port form of the selected source port.");
            }
            else
            {
                form.DisplayText = "Failed to generate launch parameters";
            }

            form.ShowDialog(this);
        }

        private void SetupStatsReader(ISourcePortData sourcePort, IGameFile gameFile)
        {
            m_statsReader = CreateStatisticsReader(sourcePort, gameFile);

            if (m_statsReader != null)
            {
                m_statsReader.NewStastics += m_statsReader_NewStastics;
                m_statsReader.Start();
            }
        }

        private void CreateFileDetectors(ISourcePortData sourcePort)
        {
            m_screenshotDetectors = CreateDefaultScreenshotDetectors();
            m_screenshotDetectors.Add(CreateScreenshotDetector(sourcePort.Directory.GetFullPath()));
            Array.ForEach(m_screenshotDetectors.ToArray(), x => x.StartDetection());

            m_saveFileDetectors = CreateDefaulSaveGameDetectors();
            m_saveFileDetectors.Add(CreateSaveGameDetector(sourcePort.Directory.GetFullPath()));
            Array.ForEach(m_saveFileDetectors.ToArray(), x => x.StartDetection());
        }

        private static GameFilePlayAdapter CreatePlayAdapter(PlayForm form, EventHandler processExited, AppConfiguration appConfig)
        {
            GameFilePlayAdapter playAdapter = new GameFilePlayAdapter();
            playAdapter.IWad = form.SelectedIWad;
            playAdapter.Map = form.SelectedMap;
            playAdapter.Skill = form.SelectedSkill;
            playAdapter.Record = form.Record;
            playAdapter.SpecificFiles = form.SpecificFiles;
            playAdapter.AdditionalFiles = form.GetAdditionalFiles().ToArray();
            playAdapter.PlayDemo = form.PlayDemo;
            playAdapter.ExtraParameters = form.ExtraParameters;
            playAdapter.SaveStatistics = form.SaveStatistics;
            playAdapter.ProcessExited += processExited;
            if (form.SelectedDemo != null) playAdapter.PlayDemoFile = Path.Combine(appConfig.DemoDirectory.GetFullPath(), form.SelectedDemo.FileName);
            return playAdapter;
        }

        private IStatisticsReader CreateStatisticsReader(ISourcePortData sourcePort, IGameFile gameFile)
        {
            List<IStatsData> existingStats = new List<IStatsData>();
            if (gameFile != null && gameFile.GameFileID.HasValue)
                existingStats = DataSourceAdapter.GetStats(gameFile.GameFileID.Value).ToList();

            return SourcePortUtil.CreateSourcePort(sourcePort).CreateStatisticsReader(gameFile, existingStats);
        }

        void m_statsReader_NewStastics(object sender, NewStatisticsEventArgs e)
        {
            if (e.Statistics != null && m_currentPlayFile != null && m_currentPlayFile.GameFileID.HasValue)
            {
                e.Statistics.MapName = e.Statistics.MapName.ToUpper();
                e.Statistics.GameFileID = m_currentPlayFile.GameFileID.Value;
                e.Statistics.SourcePortID = m_currentPlayForm.SelectedSourcePort.SourcePortID;

                if (e.Update)
                {
                    IStatsData stats = DataSourceAdapter.GetStats(e.Statistics.GameFileID).LastOrDefault(x => x.MapName == e.Statistics.MapName);
                    if (stats != null)
                        DataSourceAdapter.DeleteStats(stats.StatID);
                }

                DataSourceAdapter.InsertStats(e.Statistics);
            }
        }

        private bool IsGameFileIwad(IGameFile gameFile)
        {
            return DataSourceAdapter.GetGameFileIWads().Any(x => x.GameFileID.Value == gameFile.GameFileID.Value);
        }

        void playAdapter_ProcessExited(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object>(HandleProcessExited), new[] { sender });
            }
            else
            {
                HandleProcessExited(sender);
            }
        }

        private void HandleProcessExited(object sender)
        {
            if (m_filterForm != null)
            {
                m_filterForm.Close();
                m_filterForm = null;
            }

            GameFilePlayAdapter adapter = sender as GameFilePlayAdapter;
            DateTime dtExit = DateTime.Now;
            Directory.SetCurrentDirectory(m_workingDirectory);
            m_playInProgress = false;

            if (adapter.SourcePort != null)
            {
                IGameFile gameFile = adapter.GameFile;

                if (gameFile != null)
                    SetMinutesPlayed(dtExit, gameFile);

                if (!string.IsNullOrEmpty(adapter.RecordedFileName))
                    HandleRecordedDemo(adapter, gameFile);

                HandleDetectorFiles(adapter, gameFile);

                if (m_statsReader != null)
                {
                    m_statsReader.Stop();

                    if (m_statsReader.ReadOnClose)
                        m_statsReader.ReadNow();

                    if (m_statsReader.Errors.Length > 0)
                        HandleStatReaderErrors(m_statsReader);

                    m_statsReader = null;
                }
            }

            if (GetCurrentViewControl() != null)
                HandleSelectionChange(GetCurrentViewControl());
        }

        private IGameFile GetGameFileForIWad(IGameFile gameFile)
        {
            return DataSourceAdapter.GetGameFileIWads().FirstOrDefault(x => x.GameFileID.Value == gameFile.GameFileID.Value);
        }

        private void SetMinutesPlayed(DateTime dtExit, IGameFile gameFile)
        {
            gameFile.MinutesPlayed += Convert.ToInt32(dtExit.Subtract(m_dtStartPlay).TotalMinutes);
            DataSourceAdapter.UpdateGameFile(gameFile, new[] { GameFileFieldType.MinutesPlayed });
            UpdateDataSourceViews(gameFile);
        }

        private void HandleStatReaderErrors(IStatisticsReader m_statsReader)
        {
            TextBoxForm form = new TextBoxForm();
            form.StartPosition = FormStartPosition.CenterParent;
            form.Text = "Statistic Reader Errors";
            form.HeaderText = string.Concat("The following errors were reported by the statistics reader.", 
                Environment.NewLine, "The statistics may be incomplete or missing.");

            StringBuilder sb = new StringBuilder();
            foreach(string error in m_statsReader.Errors)
            {
                sb.Append(error);
                sb.Append(Environment.NewLine);
            }

            form.DisplayText = sb.ToString();
            form.ShowDialog(this);
        }

        private void HandleDetectorFiles(GameFilePlayAdapter adapter, IGameFile gameFile)
        {
            ScreenshotHandler screenshotHandler = new ScreenshotHandler(DataSourceAdapter, AppConfiguration.ScreenshotDirectory);
            screenshotHandler.HandleNewScreenshots(adapter.SourcePort, gameFile, GetNewScreenshots());
            SaveGameHandler savegameHandler = new SaveGameHandler(DataSourceAdapter, AppConfiguration.SaveGameDirectory);
            savegameHandler.HandleNewSaveGames(adapter.SourcePort, gameFile, GetNewSaveGames());
            savegameHandler.HandleUpdateSaveGames(adapter.SourcePort, gameFile, m_saveGames);
        }

        private void HandleRecordedDemo(GameFilePlayAdapter adapter, IGameFile gameFile)
        {
            DirectoryInfo di = new DirectoryInfo(AppConfiguration.TempDirectory.GetFullPath());
            FileInfo fiTemp = new FileInfo(adapter.RecordedFileName);
            FileInfo fi = di.GetFiles().FirstOrDefault(x => x.Name.Contains(fiTemp.Name));

            if (fi != null && fi.Exists)
            {
                DemoHandler demoHandler = new DemoHandler(DataSourceAdapter, AppConfiguration.DemoDirectory);
                demoHandler.HandleNewDemo(adapter.SourcePort, gameFile, fi.FullName,
                    m_currentPlayForm.RecordDescriptionText);
            }
            else
            {
                MessageBox.Show(this, "Could not find the demo file. Does this source port support recording?", "Demo Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<INewFileDetector> CreateDefaultScreenshotDetectors()
        {
            List<INewFileDetector> ret = new List<INewFileDetector>();

            foreach (string dir in AppConfiguration.ScreenshotCaptureDirectories)
            {
                if (Directory.Exists(dir))
                    ret.Add(CreateScreenshotDetector(dir));
            }

            return ret;
        }

        private List<INewFileDetector> CreateDefaulSaveGameDetectors()
        {
            return new List<INewFileDetector>();
        }

        private INewFileDetector CreateScreenshotDetector(string dir)
        {
            return new NewFileDetector(new[] { ".png", ".jpg", ".bmp" }, dir, true); //future - should be configurable
        }

        private INewFileDetector CreateSaveGameDetector(string dir)
        {
            return new NewFileDetector(new[] { ".zds", ".dsg", ".esg" }, dir, true); //future - should be configurable
        }

        private string[] GetNewScreenshots()
        {
            IEnumerable<string> newFiles = new string[] { };
            m_screenshotDetectors.ForEach(x => newFiles = newFiles.Union(x.GetNewFiles()));
            return newFiles.ToArray();
        }

        private string[] GetNewSaveGames()
        {
            IEnumerable<string> newFiles = new string[] { };
            m_saveFileDetectors.ForEach(x => newFiles = newFiles.Union(x.GetNewFiles()));

            IEnumerable<string> modifiedFiles = new string[] { };
            m_saveFileDetectors.ForEach(x => modifiedFiles = modifiedFiles.Union(x.GetModifiedFiles()));

            //modified files uses full path, m_saveGames does not. This section checks for modified files that were not part of the gamefile's save games
            //e.g save0.zds was a save game for this gamefile. User overwrites save1.zds for this gamefile. We now need to keep track of save1.zds as well.
            IEnumerable<string> saveFiles = m_saveGames.Select(x => x.OriginalFileName);
            List<string> ret = newFiles.ToList();
            foreach(string modifiedFile in modifiedFiles)
            {
                FileInfo fi = new FileInfo(modifiedFile);
                if (!saveFiles.Contains(fi.Name) && !ret.Contains(fi.Name))
                    ret.Add(modifiedFile);
            }

            return ret.ToArray();
        }
    }
}
