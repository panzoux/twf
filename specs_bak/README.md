# TWF Rust Port Specifications

This directory contains specifications for porting the TWF (Two-pane Window Filer) file manager from C#/.NET to Rust.

## Overview

TWF is a comprehensive keyboard-driven, dual-pane file manager application for Windows, inspired by the classic AFxW file manager. The Rust port aims to maintain all existing functionality while leveraging Rust's performance, memory safety, and cross-platform capabilities.

## Goals

1. **Feature Parity**: Maintain all existing functionality from the C# version
2. **Performance**: Leverage Rust's performance characteristics for faster operations
3. **Memory Safety**: Eliminate memory-related bugs through Rust's ownership system
4. **Cross-Platform**: Support Windows, macOS, and Linux from a single codebase
5. **Maintainability**: Create clean, well-documented code with proper error handling

## Architecture Overview

The Rust port will follow a similar architecture to the C# version but adapted for Rust idioms:

- **Models**: Data structures and business logic entities
- **Services**: Core functionality modules (file operations, search, etc.)
- **Controllers**: Application flow and coordination
- **UI**: Terminal-based user interface using Rust TUI libraries
- **Providers**: System integration and external resource access
- **Infrastructure**: Logging, configuration, and cross-cutting concerns

## Key Components to Port

1. **Dual-Pane Interface**: Side-by-side directory browsing
2. **Background Operations**: Non-blocking copy, move, delete with job management
3. **High Performance & Caching**: Asynchronous directory listing and smart caching
4. **Tab Management**: Multiple tabbed browsing with custom colors
5. **Power User Tools**: Custom functions, wildcard marking, regex filtering
6. **Archive Support**: Browse and extract archives as virtual folders
7. **Search & Filtering**: Advanced search with Migemo support for Japanese
8. **Configuration System**: JSON-based configuration with hot-reload
9. **Key Bindings**: Customizable keyboard shortcuts
10. **File Viewers**: Text and image viewers with syntax highlighting

## Technical Approach

The Rust implementation will use:

- **Terminal UI**: `ratatui` (formerly `tui-rs`) for terminal user interface
- **Async Runtime**: `tokio` for asynchronous operations
- **Configuration**: `serde_json` for JSON configuration
- **File System**: `tokio` and `std::fs` for file operations
- **Archives**: `zip`, `tar`, and other relevant crates for archive support
- **Logging**: `tracing` crate for structured logging
- **CLI Parsing**: `clap` for command-line argument parsing
- **Cross-platform Paths**: `path_absolutize` and similar crates

## Specification Documents

- `architecture.md`: Detailed architectural design
- `models.md`: Data model specifications
- `services.md`: Service layer specifications  
- `ui_components.md`: UI component specifications
- `configuration.md`: Configuration system design
- `key_bindings.md`: Key binding system design
- `file_operations.md`: File operation implementation details
- `background_jobs.md`: Background job management
- `testing_strategy.md`: Testing approach and requirements