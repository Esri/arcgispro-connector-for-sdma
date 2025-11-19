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
using SdmaConnector.Helpers;
using SdmaConnector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SdmaConnector.Handlers
{
    public static class SdmaUrlHandler
    {
        /// <summary>
        /// Handles sdma-file:// URLs and opens appropriate preview
        /// </summary>
        public static void HandleSdmaUrl(string url)
        {
            try
            {
                if (!url.StartsWith("sdma-file://", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Invalid SDMA URL format.", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var parsedUrl = ParseSdmaUrl(url);
                if (parsedUrl == null)
                {
                    MessageBox.Show("Could not parse SDMA URL.", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Handle based on file type
                switch (parsedUrl.FileType.ToLowerInvariant())
                {
                    case "image":
                        OpenImagePreview(parsedUrl);
                        break;
                    default:
                        ShowFileInfo(parsedUrl);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling SDMA URL: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static SdmaUrlInfo ParseSdmaUrl(string url)
        {
            try
            {
                // Remove the protocol
                var urlWithoutProtocol = url.Substring("sdma-file://".Length);
                
                // Split into path and query parts
                var parts = urlWithoutProtocol.Split('?');
                if (parts.Length != 2) return null;

                var pathPart = parts[0];
                var queryPart = parts[1];

                // Parse path: project/asset/filepath
                var pathSegments = pathPart.Split('/');
                if (pathSegments.Length < 3) return null;

                var projectId = Uri.UnescapeDataString(pathSegments[0]);
                var assetId = Uri.UnescapeDataString(pathSegments[1]);
                var filePath = Uri.UnescapeDataString(string.Join("/", pathSegments.Skip(2)));

                // Parse query parameters
                var queryParams = HttpUtility.ParseQueryString(queryPart);
                var fileType = queryParams["type"] ?? "unknown";
                var s3Path = queryParams["s3path"];

                return new SdmaUrlInfo
                {
                    ProjectId = projectId,
                    AssetId = assetId,
                    FilePath = filePath,
                    FileType = fileType,
                    S3Path = s3Path != null ? Uri.UnescapeDataString(s3Path) : null
                };
            }
            catch
            {
                return null;
            }
        }

        private static async void OpenImagePreview(SdmaUrlInfo urlInfo)
        {
            try
            {
                // Create a mock AssetFile from the URL info
                // Extract key from S3 path for the AssetFile
                var key = urlInfo.S3Path?.Replace("s3://sdma-bucket/", "") ?? urlInfo.FilePath;
                
                var assetFile = new SdmaProjectExplorer.Models.AssetFile
                {
                    Path = urlInfo.FilePath,
                    Key = key,
                    Type = urlInfo.FileType
                };

                // Create a mock SdmaFileItem
                var fileItem = new SdmaFileItem(assetFile, urlInfo.ProjectId, urlInfo.AssetId, urlInfo.ProjectId, urlInfo.AssetId);
                
                // Create and show the image preview window using existing components
                var imagePreviewWindow = new ImagePreviewWindow();
                var viewModel = new ImagePreviewViewModel(fileItem, assetFile);
                
                imagePreviewWindow.DataContext = viewModel;
                imagePreviewWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening image preview: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static void ShowFileInfo(SdmaUrlInfo urlInfo)
        {
            var message = $"SDMA File Information:\n\n" +
                         $"Project: {urlInfo.ProjectId}\n" +
                         $"Asset: {urlInfo.AssetId}\n" +
                         $"File: {urlInfo.FilePath}\n" +
                         $"Type: {urlInfo.FileType}\n" +
                         $"S3 Path: {urlInfo.S3Path}\n\n" +
                         $"Preview is only available for image files.";

            MessageBox.Show(message, "SDMA File Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private class SdmaUrlInfo
        {
            public string ProjectId { get; set; }
            public string AssetId { get; set; }
            public string FilePath { get; set; }
            public string FileType { get; set; }
            public string S3Path { get; set; }
        }
    }
}