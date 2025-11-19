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
    public class SdmaProjectItem : SdmaItemBase
    {
        private readonly SdmaCliService _sdmaService;
        private readonly Project _project;

        public SdmaProjectItem(SdmaCliService sdmaService, Project project)
        {
            _sdmaService = sdmaService;
            _project = project;
            Name = project.ProjectName;
            ProjectId = project.ProjectId;
        }

        public string ProjectId { get; private set; }

        public SdmaProjectExplorer.Models.Project GetProject()
        {
            return _project;
        }

        public override void LoadChildren()
        {
            System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: LoadChildren called for project '{_project.ProjectName}' (ID: {_project.ProjectId}, AssetCount: {_project.AssetCount})");

            // Use Task.Run to avoid async void
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_project.AssetCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Loading assets for project {_project.ProjectName}");

                        var assets = await _sdmaService.GetAssetsForProjectAsync(_project.ProjectId);

                        System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Retrieved {assets.Count} assets, updating UI");

                        // Update UI on main thread
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Adding {assets.Count} assets to Children collection");

                            // Clear any existing children first (should just be the lazy load child)
                            Children.Clear();

                            foreach (var asset in assets)
                            {
                                var assetItem = new SdmaAssetItem(_sdmaService, asset);
                                Children.Add(assetItem);
                                System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Added asset '{asset.AssetName}' to children");
                            }

                            System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Children collection now has {Children.Count} items");
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Project {_project.ProjectName} has no assets, clearing children");

                        // Clear children if no assets
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Children.Clear();
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SdmaProjectItem: Error loading assets for project {_project.ProjectName}: {ex}");

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Error loading assets for project {_project.ProjectName}: {ex.Message}");
                    });
                }
            });
        }
    }
}