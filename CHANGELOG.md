# Changelog

All notable changes to SdmaConnector will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-21

### Added
- **Project Browser**: Hierarchical tree view for SDMA projects, assets, and files
  - Folder hierarchy support for files with folder paths
  - Dynamic icons that change when expanded/collapsed
  - Resizable panes with GridSplitter between explorer and details
- **Authentication**: Secure CLI-based login with SDMA
- **File References**: Add SDMA file references to feature classes and tables
- **Field Management**: Automatic creation of SDMA reference fields with metadata
- **Image Preview**: Enhanced image viewer with pan and zoom capabilities
  - Universal panning at any zoom level with smooth drag interaction
  - Ctrl + Mouse Wheel zoom centered on cursor
  - Automatic fit-to-window on initial load with proper centering
  - Fit to window button resets view to centered, scaled image
  - Download support with progress tracking
- **Context Menu Integration**: 
  - Right-click on SDMA files to add references
  - "Open SDMA Link" command for opening file references from attribute tables
- **Smart Detection**: Automatic detection of tables with SDMA reference fields
- **Batch Operations**: Open multiple file links from selected table rows
- **Metadata Display**: Rich information display for projects, assets, and files
- **Error Handling**: Comprehensive error handling with user-friendly messages
- **Resource Management**: Proper disposal and memory management

### Technical Details
- Built on .NET 8.0 targeting Windows
- WPF-based UI with MVVM pattern
- ArcGIS Pro SDK 3.5+ integration
- YamlDotNet for CLI output parsing
- Support for File Geodatabase and Enterprise Geodatabase

### Supported File Types
- **Images**: JPG, JPEG, PNG, GIF, BMP, TIFF, TIF (full preview)
- **Documents**: PDF (info dialog)
- **Text**: TXT, CSV (info dialog)
- **Archives**: ZIP, RAR, 7Z (info dialog)
- **Other**: All file types (generic info dialog)

### Known Limitations
- Requires active map view for adding references
- Image preview only (no PDF preview in viewer)
- Requires SDMA Portal and CLI installations
- Windows only (ArcGIS Pro limitation)