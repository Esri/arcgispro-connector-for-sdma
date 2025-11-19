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
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using Microsoft.Win32;
using SdmaConnector.Helpers;
using SdmaProjectExplorer.Models;
using SdmaProjectExplorer.Services;

namespace SdmaConnector
{
    public class ImagePreviewViewModel : PropertyChangedBase, IDisposable
    {
        private readonly SdmaCliService _sdmaService;
        private readonly AwsCliService _awsService;
        private readonly SdmaFileItem _fileItem;
        private readonly AssetFile _file;
        private SdmaFileCredentials _credentials;
        private bool _disposed = false;

        public ImagePreviewViewModel(SdmaFileItem fileItem, AssetFile file)
        {
            _sdmaService = new SdmaCliService();
            _awsService = new AwsCliService();
            _fileItem = fileItem;
            _file = file;
            
            FilePath = file.Path;
            
            // Start loading the image
            _ = Task.Run(LoadImageAsync);
        }

        #region Properties



        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set { SetProperty(ref _filePath, value, () => FilePath); }
        }

        private bool _isLoading = true;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value, () => IsLoading); }
        }

        private bool _isDownloading = false;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set 
            { 
                SetProperty(ref _isDownloading, value, () => IsDownloading);
            }
        }

        private int _downloadProgress = 0;
        public int DownloadProgress
        {
            get { return _downloadProgress; }
            set { SetProperty(ref _downloadProgress, value, () => DownloadProgress); }
        }

        private BitmapImage _previewImage;
        public BitmapImage PreviewImage
        {
            get { return _previewImage; }
            set { SetProperty(ref _previewImage, value, () => PreviewImage); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set 
            { 
                SetProperty(ref _errorMessage, value, () => ErrorMessage);
                // Update visibility based on error message
                ErrorMessageVisible = !string.IsNullOrEmpty(value);
            }
        }

        private bool _errorMessageVisible = false;
        public bool ErrorMessageVisible
        {
            get { return _errorMessageVisible; }
            set { SetProperty(ref _errorMessageVisible, value, () => ErrorMessageVisible); }
        }

        #endregion

        #region Events

        public event Action OnImageLoaded;

        #endregion

        #region Commands

        public ICommand DownloadCommand
        {
            get
            {
                return new RelayCommand(async () => await DownloadFileAsync(), true);
            }
        }

        #endregion

        #region Methods

        private async Task LoadImageAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Get credentials from SDMA
                _credentials = await _sdmaService.GetFileCredentialsAsync(
                    _fileItem.ProjectId, _fileItem.AssetId, _file.Path);

                // Stream the image
                using var memoryStream = await _awsService.GetS3FileAsMemoryStreamAsync(_credentials);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = memoryStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        PreviewImage = bitmap;
                        
                        // Notify that image is loaded so window can fit it
                        OnImageLoaded?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Failed to load image: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine($"Failed to create bitmap: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Failed to load image: {ex.Message}";
                });
                System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadFileAsync()
        {
            try
            {
                if (_credentials == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No credentials available for download.", "Download Error");
                    return;
                }

                // Show save file dialog
                var saveDialog = new SaveFileDialog
                {
                    FileName = Path.GetFileName(_file.Path),
                    Filter = GetFileFilter(_file.Path),
                    Title = "Save Image As"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    IsDownloading = true;
                    DownloadProgress = 0;
                    
                    await _awsService.DownloadS3FileAsync(_credentials, saveDialog.FileName);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"File downloaded successfully to:\n{saveDialog.FileName}", 
                        "Download Complete");
                }
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Failed to download file: {ex.Message}", 
                    "Download Error");
            }
            finally
            {
                IsDownloading = false;
                DownloadProgress = 0;
            }
        }

        private string GetFileFilter(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
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

        public void Cleanup()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Clear event handlers to prevent memory leaks
                        OnImageLoaded = null;

                        // Dispose of the image properly
                        if (PreviewImage != null)
                        {
                            // BitmapImage doesn't implement IDisposable, but we can clear the reference
                            PreviewImage = null;
                        }

                        // Clear all properties
                        ErrorMessage = null;
                        ErrorMessageVisible = false;
                        IsLoading = false;
                        IsDownloading = false;
                        DownloadProgress = 0;
                        FilePath = null;
                        _credentials = null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during ViewModel disposal: {ex.Message}");
                    }
                }
                _disposed = true;
            }
        }

        ~ImagePreviewViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}