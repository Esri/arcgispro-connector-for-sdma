/*
Copyright 2025 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. See the License for the specific language governing
permissions and limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using SdmaConnector.Helpers;
using SdmaProjectExplorer.Services;

namespace SdmaConnector
{
    internal class ProjectExplorerViewModel : DockPane
    {
        private const string _dockPaneID = "SdmaConnector_ProjectExplorer";
        private readonly SdmaCliService _sdmaService;
        private readonly AwsCliService _awsService;
        private bool _isLoggedIn = false;

        protected ProjectExplorerViewModel()
        {
            _sdmaService = new SdmaCliService();
            _awsService = new AwsCliService();
        }

        protected override void OnHidden()
        {
            // Clear collections when hidden to free memory
            if (SdmaItems != null)
            {
                foreach (var item in SdmaItems)
                {
                    if (item is IDisposable disposableItem)
                    {
                        disposableItem.Dispose();
                    }
                }
            }
            base.OnHidden();
        }

        #region Properties

        private List<SdmaItemBase> _sdmaItems;
        public List<SdmaItemBase> SdmaItems
        {
            get { return _sdmaItems; }
            set
            {
                SetProperty(ref _sdmaItems, value, () => SdmaItems);
            }
        }

        private string _loginStatus = "Not logged in";
        public string LoginStatus
        {
            get { return _loginStatus; }
            set
            {
                SetProperty(ref _loginStatus, value, () => LoginStatus);
            }
        }

        private SdmaItemBase _selectedItem;
        public SdmaItemBase SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetProperty(ref _selectedItem, value, () => SelectedItem);
                UpdateSelectedItemDetails();
            }
        }

        private string _selectedItemDetails = "Select an item to view details";
        public string SelectedItemDetails
        {
            get { return _selectedItemDetails; }
            set
            {
                SetProperty(ref _selectedItemDetails, value, () => SelectedItemDetails);
            }
        }


        #endregion Properties

        #region Commands



        public ICommand CmdLoginAndLoadSdma
        {
            get
            {
                return new RelayCommand(async () => {
                    try
                    {
                        LoginStatus = "Logging in...";
                        var profileName = await _sdmaService.LoginAsync();
                        LoginStatus = $"Logged in: {profileName}";
                        _isLoggedIn = true;

                        // Load SDMA projects
                        await LoadSdmaProjectsAsync();
                    }
                    catch (Exception ex)
                    {
                        LoginStatus = $"Error: {ex.Message}";
                        MessageBox.Show($"Failed to login or load projects: {ex.Message}", "SDMA Error");
                    }
                }, true);
            }
        }

        public ICommand CmdRefreshSdma
        {
            get
            {
                return new RelayCommand(async () => {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("=== REFRESH: Starting refresh process ===");
                        
                        // Re-authenticate to refresh token
                        LoginStatus = "Re-authenticating...";
                        System.Diagnostics.Debug.WriteLine("REFRESH: Calling LoginAsync...");
                        
                        var profileName = await _sdmaService.LoginAsync();
                        
                        System.Diagnostics.Debug.WriteLine($"REFRESH: LoginAsync completed. Profile: {profileName}");
                        LoginStatus = $"Re-authenticated: {profileName}";
                        _isLoggedIn = true;

                        // Add a small delay to ensure token is fully written/cached
                        System.Diagnostics.Debug.WriteLine("REFRESH: Waiting 1 second for token to be fully cached...");
                        await Task.Delay(1000);

                        // Load SDMA projects
                        System.Diagnostics.Debug.WriteLine("REFRESH: Calling LoadSdmaProjectsAsync...");
                        await LoadSdmaProjectsAsync();
                        
                        System.Diagnostics.Debug.WriteLine("=== REFRESH: Refresh completed successfully ===");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"REFRESH ERROR: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"REFRESH ERROR Stack: {ex.StackTrace}");
                        LoginStatus = $"Error: {ex.Message}";
                        MessageBox.Show($"Failed to refresh projects: {ex.Message}", "SDMA Error");
                        _isLoggedIn = false;
                    }
                }, true);
            }
        }





        #endregion Commands

        #region Helper Methods

        private async Task LoadSdmaProjectsAsync()
        {
            LoginStatus = "Loading projects...";

            try
            {
                var projects = await _sdmaService.GetAllProjectsAsync();

                var sdmaItems = new List<SdmaItemBase>();
                foreach (var project in projects)
                {
                    sdmaItems.Add(new SdmaProjectItem(_sdmaService, project));
                }

                // Clear existing items first
                SdmaItems = null;
                SdmaItems = sdmaItems;

                LoginStatus = $"Loaded {projects.Count} projects";


            }
            catch (Exception ex)
            {
                LoginStatus = $"Error loading projects: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"LoadSdmaProjectsAsync Error: {ex}");
                throw;
            }
        }

        private void UpdateSelectedItemDetails()
        {
            if (_selectedItem == null)
            {
                SelectedItemDetails = "Select an item to view detailed information";
                return;
            }

            var details = new System.Text.StringBuilder();

            if (_selectedItem is SdmaProjectItem projectItem)
            {
                var project = GetProjectFromItem(projectItem);
                if (project != null)
                {
                    details.AppendLine("PROJECT INFORMATION");
                    details.AppendLine("═══════════════════════════════════════");
                    details.AppendLine($"Name:           {project.ProjectName}");
                    details.AppendLine($"ID:             {project.ProjectId}");
                    details.AppendLine($"Description:    {project.Description}");
                    details.AppendLine();
                    details.AppendLine("STATISTICS");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Assets:         {project.AssetCount:N0}");
                    details.AppendLine($"Files:          {project.FileCount:N0}");
                    details.AppendLine($"Total Size:     {FormatBytes(project.TotalSize)}");
                    details.AppendLine();
                    details.AppendLine("TIMELINE");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Created:        {project.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    details.AppendLine($"Created By:     {project.CreatedBy}");
                }
            }
            else if (_selectedItem is SdmaAssetItem assetItem)
            {
                var asset = GetAssetFromItem(assetItem);
                if (asset != null)
                {
                    details.AppendLine("ASSET INFORMATION");
                    details.AppendLine("═══════════════════════════════════════");
                    details.AppendLine($"Name:           {asset.AssetName}");
                    details.AppendLine($"ID:             {asset.AssetId}");
                    details.AppendLine($"Project:        {asset.ProjectName}");
                    details.AppendLine($"Description:    {asset.Description}");
                    details.AppendLine();
                    details.AppendLine("STATISTICS");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Files:          {asset.FileCount:N0}");
                    details.AppendLine($"Total Size:     {FormatBytes(asset.TotalSize)}");
                    details.AppendLine();
                    details.AppendLine("TIMELINE");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Created:        {asset.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    details.AppendLine($"Created By:     {asset.CreatedBy}");
                }
            }
            else if (_selectedItem is SdmaFileItem fileItem)
            {
                var file = GetFileFromItem(fileItem);
                if (file != null)
                {
                    details.AppendLine("FILE INFORMATION");
                    details.AppendLine("═══════════════════════════════════════");
                    details.AppendLine($"Path:           {file.Path}");
                    details.AppendLine($"Size:           {FormatBytes(file.Size)}");
                    details.AppendLine($"Type:           {file.Type.ToUpper()}");
                    details.AppendLine($"State:          {file.State}");
                    details.AppendLine();
                    details.AppendLine("ANALYSIS");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Analysis:       {file.AnalysisState}");
                    details.AppendLine($"Metadata:       {(file.HasSuggestedMetadata ? "Available" : "None")}");
                    details.AppendLine();
                    details.AppendLine("TECHNICAL");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Key:            {file.Key}");
                    details.AppendLine();
                    details.AppendLine("TIMELINE");
                    details.AppendLine("─────────────────────────────────────");
                    details.AppendLine($"Added:          {file.AddedAt:yyyy-MM-dd HH:mm:ss}");
                    details.AppendLine($"Modified:       {DateTimeOffset.FromUnixTimeMilliseconds(file.Mtime / 1000).DateTime:yyyy-MM-dd HH:mm:ss}");
                }
            }

            SelectedItemDetails = details.ToString();
        }

        private SdmaProjectExplorer.Models.Project GetProjectFromItem(SdmaProjectItem item)
        {
            // We need to store the original project data in the item
            return item.GetProject();
        }

        private SdmaProjectExplorer.Models.Asset GetAssetFromItem(SdmaAssetItem item)
        {
            // We need to store the original asset data in the item
            return item.GetAsset();
        }

        private SdmaProjectExplorer.Models.AssetFile GetFileFromItem(SdmaFileItem item)
        {
            // We need to store the original file data in the item
            return item.GetFile();
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        #endregion Helper Methods

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Project Explorer";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane and trigger SDMA login.
    /// </summary>
    internal class ProjectExplorer_ShowButton : Button
    {
        protected override void OnClick()
        {
            ProjectExplorerViewModel.Show();

            // Get the ViewModel instance and trigger login
            var pane = FrameworkApplication.DockPaneManager.Find("SdmaConnector_ProjectExplorer") as ProjectExplorerViewModel;
            if (pane != null)
            {
                // Execute the login command
                if (pane.CmdLoginAndLoadSdma.CanExecute(null))
                {
                    pane.CmdLoginAndLoadSdma.Execute(null);
                }
            }
        }
    }
}
