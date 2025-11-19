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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using SdmaProjectExplorer.Models;

namespace SdmaProjectExplorer.Services
{
    public class SdmaCliService : IDisposable
    {
        private readonly IDeserializer _yamlDeserializer;
        private bool _disposed = false;

        public SdmaCliService()
        {
            _yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public async Task<string> LoginAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== LoginAsync: Starting authentication ===");
            var output = await ExecuteCommandAsync("spatial-data-mgmt", "auth login");
            System.Diagnostics.Debug.WriteLine($"LoginAsync: Raw output: {output}");

            // Parse the profile name from the output
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                System.Diagnostics.Debug.WriteLine($"LoginAsync: Parsing line: {line}");
                if (line.Contains("Successfully logged in:") && line.Contains("profile:"))
                {
                    var profileStart = line.IndexOf("profile:") + "profile:".Length;
                    var profileName = line.Substring(profileStart).Trim();
                    System.Diagnostics.Debug.WriteLine($"LoginAsync: Found profile: {profileName}");
                    return profileName;
                }
            }

            System.Diagnostics.Debug.WriteLine("LoginAsync: No profile found in output, returning 'Unknown Profile'");
            return "Unknown Profile";
        }

        public async Task LogoutAsync()
        {
            await ExecuteCommandAsync("spatial-data-mgmt", "auth logout");
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            System.Diagnostics.Debug.WriteLine("SdmaCliService: Starting GetAllProjectsAsync");

            try
            {
                var output = await ExecuteCommandAsync("spatial-data-mgmt", "project list");

                System.Diagnostics.Debug.WriteLine($"SdmaCliService: CLI output length: {output?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: CLI output: {output}");

                var result = ParseYamlOutput<Project>(output);

                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Parsed {result.Count} projects");

                // Debug the parsed project details
                foreach (var project in result)
                {
                    System.Diagnostics.Debug.WriteLine($"SdmaCliService: Project - ID: '{project.ProjectId}', Name: '{project.ProjectName}', AssetCount: {project.AssetCount}");
                }

                return result;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("401") || ex.Message.Contains("token has expired"))
            {
                System.Diagnostics.Debug.WriteLine("SdmaCliService: Token expired, attempting re-authentication");
                throw new InvalidOperationException("Authentication token has expired. Please click Refresh to re-authenticate.", ex);
            }
        }

        public async Task<List<Asset>> GetAssetsForProjectAsync(string projectId)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Starting GetAssetsForProjectAsync for project: {projectId}");

            var output = await ExecuteCommandAsync("spatial-data-mgmt", $"asset list --project-id {projectId}");

            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Asset CLI output length: {output?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Asset CLI output: {output}");

            var result = ParseYamlOutput<Asset>(output);

            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Parsed {result.Count} assets for project {projectId}");

            // Debug the parsed asset details
            foreach (var asset in result)
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Asset - ID: '{asset.AssetId}', Name: '{asset.AssetName}', FileCount: {asset.FileCount}");
            }

            return result;
        }

        public async Task<List<AssetFile>> GetFilesForAssetAsync(string assetId, string projectId)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Starting GetFilesForAssetAsync for asset: {assetId}, project: {projectId}");

            var output = await ExecuteCommandAsync("spatial-data-mgmt", $"asset file list --asset-id {assetId} --project-id {projectId}");

            System.Diagnostics.Debug.WriteLine($"SdmaCliService: File CLI output length: {output?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: File CLI output: {output}");

            // Parse as AssetFileResponse (single object with files property)
            var response = ParseYamlSingleObject<AssetFileResponse>(output);
            var result = response?.Files ?? new List<AssetFile>();

            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Parsed {result.Count} files for asset {assetId}");

            // Debug the parsed file details
            foreach (var file in result)
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: File - Path: '{file.Path}', Size: {file.Size}, Type: '{file.Type}'");
            }

            return result;
        }

        private async Task<string> ExecuteCommandAsync(string command, string arguments)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Executing command: {command} {arguments}");

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Command exit code: {process.ExitCode}");
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Command output: {output}");
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Command error: {error}");

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Exception executing command: {ex}");
                throw new InvalidOperationException($"Error executing command '{command} {arguments}': {ex.Message}", ex);
            }
        }

        private List<T> ParseYamlOutput<T>(string yamlOutput)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: ParseYamlOutput called for type {typeof(T).Name}");

            if (string.IsNullOrWhiteSpace(yamlOutput))
            {
                System.Diagnostics.Debug.WriteLine("SdmaCliService: YAML output is null or empty");
                return new List<T>();
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Attempting to deserialize YAML: {yamlOutput.Substring(0, Math.Min(200, yamlOutput.Length))}...");

                var items = _yamlDeserializer.Deserialize<List<T>>(yamlOutput);
                var result = items ?? new List<T>();

                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Successfully parsed {result.Count} items");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: YAML parsing error: {ex}");
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Raw YAML that failed: {yamlOutput}");
                throw new InvalidOperationException($"Error parsing YAML output: {ex.Message}", ex);
            }
        }

        public async Task<SdmaFileCredentials> GetFileCredentialsAsync(string projectId, string assetId, string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Getting credentials for file: {filePath}");

            var output = await ExecuteCommandAsync("spatial-data-mgmt", 
                $"asset file get-credentials --project-id {projectId} --asset-id {assetId} --path \"{filePath}\"");

            System.Diagnostics.Debug.WriteLine($"SdmaCliService: Credentials output: {output}");

            var credentials = ParseYamlSingleObject<SdmaFileCredentials>(output);
            
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to parse file credentials");
            }

            return credentials;
        }

        private T ParseYamlSingleObject<T>(string yamlOutput)
        {
            System.Diagnostics.Debug.WriteLine($"SdmaCliService: ParseYamlSingleObject called for type {typeof(T).Name}");

            if (string.IsNullOrWhiteSpace(yamlOutput))
            {
                System.Diagnostics.Debug.WriteLine("SdmaCliService: YAML output is null or empty");
                return default(T);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Attempting to deserialize single YAML object: {yamlOutput.Substring(0, Math.Min(200, yamlOutput.Length))}...");

                var item = _yamlDeserializer.Deserialize<T>(yamlOutput);

                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Successfully parsed single object");

                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: YAML parsing error: {ex}");
                System.Diagnostics.Debug.WriteLine($"SdmaCliService: Raw YAML that failed: {yamlOutput}");
                throw new InvalidOperationException($"Error parsing YAML output: {ex.Message}", ex);
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
                    // The deserializer doesn't implement IDisposable
                }
                _disposed = true;
            }
        }

        ~SdmaCliService()
        {
            Dispose(false);
        }
    }
}