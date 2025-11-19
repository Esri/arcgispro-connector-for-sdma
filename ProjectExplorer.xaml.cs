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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ArcGIS.Desktop.Framework.Dialogs;
using Microsoft.Win32;
using SdmaConnector.Helpers;
using SdmaProjectExplorer.Models;
using SdmaProjectExplorer.Services;


namespace SdmaConnector
{
    /// <summary>
    /// Interaction logic for ProjectExplorerView.xaml
    /// </summary>
    public partial class ProjectExplorerView : UserControl
    {
        public ProjectExplorerView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ProjectExplorerViewModel viewModel && e.NewValue is SdmaConnector.Helpers.SdmaItemBase selectedItem)
            {
                viewModel.SelectedItem = selectedItem;
            }
        }

        private void PreviewImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is SdmaFileItem fileItem)
            {
                var file = fileItem.GetFile();
                if (IsImageFile(file.Path))
                {
                    var fileName = System.IO.Path.GetFileName(file.Path);
                    var previewWindow = new ImagePreviewWindow(fileName);
                    var viewModel = new ImagePreviewViewModel(fileItem, file);
                    previewWindow.DataContext = viewModel;
                    // Don't set Owner to prevent binding inheritance issues
                    // previewWindow.Owner = Window.GetWindow(this);
                    previewWindow.Show();
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Selected file is not an image.", "Preview Error");
                }
            }
        }

        private async void DownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is SdmaFileItem fileItem)
            {
                try
                {
                    var file = fileItem.GetFile();

                    // Show save file dialog
                    var saveDialog = new SaveFileDialog
                    {
                        FileName = System.IO.Path.GetFileName(file.Path),
                        Filter = GetFileFilter(file.Path),
                        Title = "Save File As"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // Show progress window
                        var progressWindow = new Window
                        {
                            Title = "Downloading...",
                            Width = 400,
                            Height = 150,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = Window.GetWindow(this),
                            ResizeMode = ResizeMode.NoResize
                        };

                        var progressContent = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Children =
                            {
                                new TextBlock { Text = "Downloading file...", FontSize = 14, Margin = new Thickness(0, 0, 0, 10) },
                                new ProgressBar { IsIndeterminate = true, Height = 8, Margin = new Thickness(0, 0, 0, 10) },
                                new TextBlock { Text = System.IO.Path.GetFileName(file.Path), FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) }
                            }
                        };

                        progressWindow.Content = progressContent;
                        progressWindow.Show();

                        try
                        {
                            var sdmaService = new SdmaCliService();
                            var awsService = new AwsCliService();

                            // Get credentials
                            var credentials = await sdmaService.GetFileCredentialsAsync(
                                fileItem.ProjectId, fileItem.AssetId, file.Path);

                            // Download file
                            await awsService.DownloadS3FileAsync(credentials, saveDialog.FileName);

                            await CloseProgressWindowSafelyAsync(progressWindow);
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"File downloaded successfully to:\n{saveDialog.FileName}",
                                "Download Complete");
                        }
                        finally
                        {
                            await CloseProgressWindowSafelyAsync(progressWindow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Failed to download file: {ex.Message}",
                        "Download Error");
                }
            }
        }

        private bool IsImageFile(string path)
        {
            var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp" };
            return imageExtensions.Contains(extension);
        }

        private async void DownloadAsset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is SdmaAssetItem assetItem)
            {
                try
                {
                    var asset = assetItem.GetAsset();

                    // Show folder selection using SaveFileDialog workaround
                    var saveDialog = new SaveFileDialog
                    {
                        Title = "Select Download Folder",
                        FileName = $"{asset.AssetName}_Files",
                        DefaultExt = ".txt",
                        Filter = "Select Folder Location|*.txt",
                        CheckFileExists = false,
                        CheckPathExists = true
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // Get the directory from the selected path
                        var selectedFolder = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            await DownloadAllAssetFilesAsync(assetItem, selectedFolder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Failed to download asset: {ex.Message}",
                        "Download Error");
                }
            }
        }

        private async Task DownloadAllAssetFilesAsync(SdmaAssetItem assetItem, string downloadFolder)
        {
            var asset = assetItem.GetAsset();
            var sdmaService = new SdmaCliService();
            var awsService = new AwsCliService();

            // Create progress window
            var progressWindow = new Window
            {
                Title = "Downloading Asset Files...",
                Width = 500,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var progressContent = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var titleText = new TextBlock
            {
                Text = $"Downloading files from: {asset.AssetName}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var statusText = new TextBlock
            {
                Text = "Preparing download...",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var progressBar = new ProgressBar
            {
                Height = 8,
                Margin = new Thickness(0, 0, 0, 10),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var detailText = new TextBlock
            {
                Text = "",
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.Gray)
            };

            var speedText = new TextBlock
            {
                Text = "",
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 5, 0, 0)
            };

            progressContent.Children.Add(titleText);
            progressContent.Children.Add(statusText);
            progressContent.Children.Add(progressBar);
            progressContent.Children.Add(detailText);
            progressContent.Children.Add(speedText);

            progressWindow.Content = progressContent;
            progressWindow.Show();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Get all files for the asset
                statusText.Text = "Getting file list...";
                var files = await sdmaService.GetFilesForAssetAsync(asset.AssetId, asset.ProjectId);

                if (files.Count == 0)
                {
                    statusText.Text = "No files found in asset.";
                    await Task.Delay(2000);
                    await CloseProgressWindowSafelyAsync(progressWindow);
                    return;
                }

                progressBar.Maximum = files.Count;
                var downloadedCount = 0;
                var failedCount = 0;

                // Create asset folder
                var assetFolder = System.IO.Path.Combine(downloadFolder, SanitizeFileName(asset.AssetName));
                System.IO.Directory.CreateDirectory(assetFolder);

                // OPTIMIZATION 1: Get credentials for first file and try to reuse
                statusText.Text = "Getting credentials...";
                SdmaFileCredentials sharedCredentials = null;
                if (files.Count > 0)
                {
                    try
                    {
                        sharedCredentials = await sdmaService.GetFileCredentialsAsync(
                            asset.ProjectId, asset.AssetId, files[0].Path);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to get shared credentials: {ex.Message}");
                    }
                }

                // OPTIMIZATION 2: Parallel downloads with semaphore for controlled concurrency
                var maxConcurrency = Math.Min(Environment.ProcessorCount * 2, 8); // Limit concurrent downloads
                using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                var downloadTasks = new List<Task>();

                foreach (var file in files)
                {
                    var downloadTask = ProcessFileDownloadAsync(
                        file, asset, sdmaService, awsService, assetFolder,
                        sharedCredentials, semaphore,
                        () => {
                            downloadedCount++;
                            var elapsed = stopwatch.Elapsed;
                            var rate = downloadedCount / elapsed.TotalSeconds;

                            Dispatcher.Invoke(() => {
                                progressBar.Value = downloadedCount;
                                statusText.Text = $"Downloaded {downloadedCount} of {files.Count} files...";
                                detailText.Text = $"Current: {System.IO.Path.GetFileName(file.Path)}";
                                speedText.Text = $"Speed: {rate:F1} files/sec | Elapsed: {elapsed:mm\\:ss}";
                            });
                        },
                        () => {
                            failedCount++;
                        });

                    downloadTasks.Add(downloadTask);
                }

                // Wait for all downloads to complete
                await Task.WhenAll(downloadTasks);

                // Clear task list to free memory
                downloadTasks.Clear();
                downloadTasks = null;

                // Clean window closure to prevent binding errors
                await CloseProgressWindowSafelyAsync(progressWindow);
                stopwatch.Stop();

                // Force garbage collection after bulk operation
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Show completion message
                var totalTime = stopwatch.Elapsed;
                var avgRate = downloadedCount / totalTime.TotalSeconds;
                var message = $"Asset download completed!\n\n" +
                             $"Downloaded: {downloadedCount} files\n" +
                             $"Failed: {failedCount} files\n" +
                             $"Time: {totalTime:mm\\:ss}\n" +
                             $"Average Rate: {avgRate:F1} files/sec\n" +
                             $"Location: {assetFolder}";

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Download Complete");

                // Open the folder in Windows Explorer
                if (downloadedCount > 0)
                {
                    System.Diagnostics.Process.Start("explorer.exe", assetFolder);
                }
            }
            catch (Exception ex)
            {
                await CloseProgressWindowSafelyAsync(progressWindow);
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Failed to download asset files: {ex.Message}",
                    "Download Error");
            }
        }

        private async Task ProcessFileDownloadAsync(
            AssetFile file,
            Asset asset,
            SdmaCliService sdmaService,
            AwsCliService awsService,
            string assetFolder,
            SdmaFileCredentials sharedCredentials,
            SemaphoreSlim semaphore,
            Action onSuccess,
            Action onFailure)
        {
            await semaphore.WaitAsync();
            try
            {
                // Create subdirectories if needed
                var relativePath = file.Path.Replace('/', System.IO.Path.DirectorySeparatorChar);
                var fullFilePath = System.IO.Path.Combine(assetFolder, relativePath);
                var fileDirectory = System.IO.Path.GetDirectoryName(fullFilePath);

                if (!string.IsNullOrEmpty(fileDirectory))
                {
                    System.IO.Directory.CreateDirectory(fileDirectory);
                }

                SdmaFileCredentials credentials = sharedCredentials;

                // OPTIMIZATION 3: Try shared credentials first, fall back to individual if needed
                if (sharedCredentials != null)
                {
                    try
                    {
                        // Try using shared credentials with updated object key
                        var modifiedCredentials = new SdmaFileCredentials
                        {
                            AccessKeyId = sharedCredentials.AccessKeyId,
                            SecretAccessKey = sharedCredentials.SecretAccessKey,
                            SessionToken = sharedCredentials.SessionToken,
                            S3Bucket = sharedCredentials.S3Bucket,
                            ObjectKey = $"SpatialDataManagementAssets/Data/{file.Key}.xxh128", // Construct object key
                            Hash = file.Key,
                            Path = file.Path
                        };

                        await awsService.DownloadS3FileAsync(modifiedCredentials, fullFilePath);
                        onSuccess();
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Shared credentials failed for {file.Path}, getting individual credentials: {ex.Message}");
                    }
                }

                // Fall back to individual credentials if shared credentials don't work
                credentials = await sdmaService.GetFileCredentialsAsync(
                    asset.ProjectId, asset.AssetId, file.Path);

                await awsService.DownloadS3FileAsync(credentials, fullFilePath);
                onSuccess();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to download file {file.Path}: {ex.Message}");
                onFailure();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task CloseProgressWindowSafelyAsync(Window progressWindow)
        {
            try
            {
                if (progressWindow == null) return;

                // Clear DataContext and bindings to prevent ArcGIS Pro binding errors
                progressWindow.DataContext = null;
                progressWindow.Owner = null;

                // Clear bindings on all child elements
                ClearBindingsRecursively(progressWindow);

                // Clear content to break references
                if (progressWindow.Content is StackPanel stackPanel)
                {
                    stackPanel.Children.Clear();
                    progressWindow.Content = null;
                }

                // Give time for animations to complete
                await Task.Delay(100);

                // Hide first, then close
                progressWindow.Hide();
                await Task.Delay(50);

                progressWindow.Close();

                // Explicitly null the reference
                progressWindow = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing progress window: {ex.Message}");
                try { progressWindow?.Close(); } catch { }
            }
        }

        private void ClearBindingsRecursively(DependencyObject obj)
        {
            try
            {
                if (obj == null) return;

                // Clear bindings on this object
                BindingOperations.ClearAllBindings(obj);

                // Recursively clear bindings on children
                int childCount = VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);
                    ClearBindingsRecursively(child);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing bindings: {ex.Message}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Asset" : sanitized;
        }

        private string GetFileFilter(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".png" => "PNG Images (*.png)|*.png|All Files (*.*)|*.*",
                ".jpg" => "JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|All Files (*.*)|*.*",
                ".jpeg" => "JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|All Files (*.*)|*.*",
                ".gif" => "GIF Images (*.gif)|*.gif|All Files (*.*)|*.*",
                ".bmp" => "Bitmap Images (*.bmp)|*.bmp|All Files (*.*)|*.*",
                ".tiff" => "TIFF Images (*.tiff;*.tif)|*.tiff;*.tif|All Files (*.*)|*.*",
                ".tif" => "TIFF Images (*.tiff;*.tif)|*.tiff;*.tif|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };
        }
    }
}
