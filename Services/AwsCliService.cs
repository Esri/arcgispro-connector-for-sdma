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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SdmaProjectExplorer.Models;

namespace SdmaProjectExplorer.Services
{
    public class AwsCliService : IDisposable
    {
        private bool _disposed = false;
        public async Task<MemoryStream> GetS3FileAsMemoryStreamAsync(SdmaFileCredentials credentials)
        {
            try
            {
                var s3Uri = $"s3://{credentials.S3Bucket}/{credentials.ObjectKey}";
                
                System.Diagnostics.Debug.WriteLine($"AwsCliService: Loading to memory from S3: {s3Uri}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "aws",
                    Arguments = $"s3 cp \"{s3Uri}\" -",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set temporary AWS credentials
                processStartInfo.EnvironmentVariables["AWS_ACCESS_KEY_ID"] = credentials.AccessKeyId;
                processStartInfo.EnvironmentVariables["AWS_SECRET_ACCESS_KEY"] = credentials.SecretAccessKey;
                processStartInfo.EnvironmentVariables["AWS_SESSION_TOKEN"] = credentials.SessionToken;

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var memoryStream = new MemoryStream();
                await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"AWS CLI failed: {error}");
                }

                memoryStream.Position = 0; // Reset for reading
                return memoryStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AwsCliService: Failed to get S3 file: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DownloadS3FileAsync(SdmaFileCredentials credentials, string localFilePath)
        {
            try
            {
                var s3Uri = $"s3://{credentials.S3Bucket}/{credentials.ObjectKey}";
                
                System.Diagnostics.Debug.WriteLine($"AwsCliService: Downloading from S3 to: {localFilePath}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "aws",
                    Arguments = $"s3 cp \"{s3Uri}\" \"{localFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set temporary AWS credentials
                processStartInfo.EnvironmentVariables["AWS_ACCESS_KEY_ID"] = credentials.AccessKeyId;
                processStartInfo.EnvironmentVariables["AWS_SECRET_ACCESS_KEY"] = credentials.SecretAccessKey;
                processStartInfo.EnvironmentVariables["AWS_SESSION_TOKEN"] = credentials.SessionToken;

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"AWS CLI download failed: {error}");
                    throw new InvalidOperationException($"AWS CLI download failed: {error}");
                }

                System.Diagnostics.Debug.WriteLine($"Successfully downloaded file to: {localFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AwsCliService: Failed to download S3 file: {ex.Message}");
                throw;
            }
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
                    // No specific resources to dispose for this service
                }
                _disposed = true;
            }
        }

        ~AwsCliService()
        {
            Dispose(false);
        }
    }
}