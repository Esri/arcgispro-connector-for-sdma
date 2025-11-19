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

using ArcGIS.Desktop.Framework.Dialogs;
using SdmaProjectExplorer.Models;
using SdmaProjectExplorer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SdmaConnector.Helpers
{
    public class SdmaAssetItem : SdmaItemBase
    {
        private readonly SdmaCliService _sdmaService;
        private readonly Asset _asset;

        public SdmaAssetItem(SdmaCliService sdmaService, Asset asset)
        {
            _sdmaService = sdmaService;
            _asset = asset;
            Name = asset.AssetName;
            AssetId = asset.AssetId;
            ProjectId = asset.ProjectId;

            System.Diagnostics.Debug.WriteLine($"SdmaAssetItem: Created asset item - Name: '{Name}', AssetId: '{AssetId}', FileCount: {asset.FileCount}");
        }

        public string AssetId { get; private set; }
        public string ProjectId { get; private set; }

        public SdmaProjectExplorer.Models.Asset GetAsset()
        {
            return _asset;
        }

        public override async void LoadChildren()
        {
            try
            {
                if (_asset.FileCount > 0)
                {
                    var files = await _sdmaService.GetFilesForAssetAsync(_asset.AssetId, _asset.ProjectId);
                    BuildFolderHierarchy(files);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files for asset {_asset.AssetName}: {ex.Message}");
            }
        }

        private void BuildFolderHierarchy(List<SdmaProjectExplorer.Models.AssetFile> files)
        {
            // Dictionary to track folder items by their path
            var folderMap = new Dictionary<string, SdmaFolderItem>();

            foreach (var file in files)
            {
                var pathParts = file.Path.Split('/');
                
                if (pathParts.Length == 1)
                {
                    // File at root level - add directly to asset
                    Children.Add(new SdmaFileItem(file, _asset.ProjectId, _asset.AssetId, _asset.ProjectName, _asset.AssetName));
                }
                else
                {
                    // File is in a folder structure
                    SdmaItemBase currentParent = this;
                    string currentPath = "";

                    // Process each folder in the path (excluding the filename)
                    for (int i = 0; i < pathParts.Length - 1; i++)
                    {
                        currentPath = string.IsNullOrEmpty(currentPath) 
                            ? pathParts[i] 
                            : $"{currentPath}/{pathParts[i]}";

                        if (!folderMap.ContainsKey(currentPath))
                        {
                            // Create new folder item
                            var folderItem = new SdmaFolderItem(pathParts[i]);
                            folderMap[currentPath] = folderItem;
                            currentParent.Children.Add(folderItem);
                            currentParent = folderItem;
                        }
                        else
                        {
                            currentParent = folderMap[currentPath];
                        }
                    }

                    // Add the file to its parent folder
                    currentParent.Children.Add(new SdmaFileItem(file, _asset.ProjectId, _asset.AssetId, _asset.ProjectName, _asset.AssetName));
                }
            }
        }
    }
}