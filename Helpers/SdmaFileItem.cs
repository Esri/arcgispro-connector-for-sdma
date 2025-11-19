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

using SdmaProjectExplorer.Models;
using SdmaConnector.Commands;
using SdmaConnector.Services;
using System;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;

namespace SdmaConnector.Helpers
{
    public class SdmaFileItem : SdmaItemBase
    {
        private readonly AssetFile _file;
        private ICommand _addReferenceCommand;

        public SdmaFileItem(AssetFile file, string projectId, string assetId, string projectName = null, string assetName = null)
        {
            _file = file;
            ProjectId = projectId;
            AssetId = assetId;
            ProjectName = projectName ?? projectId; // Fallback to ID if name not provided
            AssetName = assetName ?? assetId; // Fallback to ID if name not provided
            
            // Extract just the filename from the path
            var pathParts = file.Path.Split('/');
            Name = pathParts[pathParts.Length - 1];
            FilePath = file.Path;
            
            // Initialize the command
            _addReferenceCommand = new RelayCommand(() => ExecuteAddReference(), () => CanExecuteAddReference());
        }

        public string FilePath { get; private set; }
        public string ProjectId { get; }
        public string AssetId { get; }
        public string ProjectName { get; }
        public string AssetName { get; }

        /// <summary>
        /// Command for adding this file as a reference to selected features
        /// </summary>
        public ICommand AddReferenceCommand => _addReferenceCommand;

        public AssetFile GetFile()
        {
            return _file;
        }

        public override bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                SetProperty(ref _isSelected, value, () => IsSelected);
                if (_isSelected)
                {
                    // TODO: Implement file handling logic here
                    // For now, just show the file path
                    System.Diagnostics.Debug.WriteLine($"Selected SDMA file: {FilePath}");
                }
            }
        }

        public override void LoadChildren()
        {
            // Files don't have children, so clear the lazy load child
            Children.Clear();
        }

        private async void ExecuteAddReference()
        {
            try
            {
                await SdmaReferenceService.AddReferenceToSelectedFeaturesAsync(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing add reference command: {ex.Message}");
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Error adding reference: {ex.Message}", "Error");
            }
        }

        private bool CanExecuteAddReference()
        {
            // Always allow the command - validation will happen in the command itself
            return true;
        }
    }
}