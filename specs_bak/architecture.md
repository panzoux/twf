# TWF Rust Port - Architecture Specification

## Overview

This document outlines the architectural design for the Rust port of TWF (Two-pane Window Filer). The architecture follows a layered approach similar to the C# version but adapted for Rust idioms and ecosystem.

## High-Level Architecture

```
┌─────────────────┐
│   UI Layer      │  <- ratatui-based terminal interface
├─────────────────┤
│ Controller Layer│  <- Application logic and state management
├─────────────────┤
│  Service Layer  │  <- Business logic and operations
├─────────────────┤
│ Provider Layer  │  <- System integration and external resources
├─────────────────┤
│ Infrastructure  │  <- Logging, config, utilities
└─────────────────┘
```

## Module Structure

```
twf-rs/
├── src/
│   ├── main.rs              # Application entry point
│   ├── app.rs               # Main application state and controller
│   ├── config/              # Configuration management
│   │   ├── mod.rs
│   │   ├── models.rs        # Configuration data structures
│   │   └── loader.rs        # Configuration loading/saving
│   ├── models/              # Core data models
│   │   ├── mod.rs
│   │   ├── file_entry.rs    # File and directory representations
│   │   ├── pane_state.rs    # Pane state management
│   │   ├── tab_session.rs   # Tab session data
│   │   └── ui_mode.rs       # UI mode definitions
│   ├── services/            # Core services
│   │   ├── mod.rs
│   │   ├── file_ops.rs      # File operations (copy, move, delete)
│   │   ├── search.rs        # Search and filtering engine
│   │   ├── archive.rs       # Archive management
│   │   ├── jobs.rs          # Background job management
│   │   ├── history.rs       # Navigation history
│   │   └── viewer.rs        # File viewing services
│   ├── providers/           # System providers
│   │   ├── mod.rs
│   │   ├── fs_provider.rs   # File system operations
│   │   ├── list_provider.rs # Directory listing
│   │   └── keybindings.rs   # Key binding management
│   ├── ui/                  # User interface components
│   │   ├── mod.rs
│   │   ├── app_ui.rs        # Main application UI
│   │   ├── pane_view.rs     # Individual pane view
│   │   ├── tab_bar.rs       # Tab bar component
│   │   ├── dialogs/         # Various dialog windows
│   │   │   ├── mod.rs
│   │   │   ├── file_ops.rs  # File operation dialogs
│   │   │   ├── search.rs    # Search dialogs
│   │   │   └── settings.rs  # Settings dialogs
│   │   └── widgets/         # Reusable UI widgets
│   │       ├── mod.rs
│   │       ├── file_list.rs # File list widget
│   │       ├── status_bar.rs # Status bar widget
│   │       └── progress.rs   # Progress indicators
│   ├── utils/               # Utility functions
│   │   ├── mod.rs
│   │   ├── path_utils.rs    # Path manipulation utilities
│   │   ├── char_width.rs    # Character width calculations (for CJK)
│   │   └── validation.rs    # Input validation
│   └── infrastructure/      # Cross-cutting concerns
│       ├── mod.rs
│       ├── logging.rs       # Logging setup
│       └── error.rs         # Error types and handling
├── Cargo.toml               # Project manifest
└── docs/                    # Documentation
```

## Component Responsibilities

### App State (`app.rs`)
- Manages global application state
- Coordinates between UI and services
- Handles application lifecycle
- Maintains active tabs and panes

### Configuration (`config/`)
- Loads and saves JSON configuration files
- Provides runtime configuration access
- Handles configuration validation
- Supports hot-reloading of settings

### Models (`models/`)
- Defines core data structures
- Implements domain logic for file entries
- Manages pane and tab states
- Handles serialization/deserialization

### Services (`services/`)
- Implements business logic for file operations
- Manages background jobs
- Provides search and filtering capabilities
- Handles archive operations
- Manages history and bookmarks

### Providers (`providers/`)
- Abstracts system-level operations
- Provides file system access
- Handles key binding resolution
- Manages external integrations

### UI Components (`ui/`)
- Implements terminal-based user interface
- Manages UI state and rendering
- Handles user input
- Provides dialog systems

## Data Flow

1. **User Input**: Terminal events captured by UI layer
2. **Event Processing**: Events translated to application commands
3. **Service Execution**: Commands processed by service layer
4. **State Update**: Application state updated based on results
5. **UI Update**: UI components refreshed to reflect new state

## Error Handling Strategy

- Use `anyhow` for application-level error handling
- Use custom error types for domain-specific errors
- Implement proper error propagation with `?` operator
- Provide user-friendly error messages in UI
- Log detailed errors for debugging

## Async Operations

- Use `tokio` for concurrent file operations
- Implement proper cancellation for long-running operations
- Provide progress updates for background tasks
- Use async/await for I/O-bound operations

## Memory Management

- Leverage Rust's ownership system for memory safety
- Use `Arc<Mutex<T>>` for shared mutable state when needed
- Implement proper caching strategies with lifetime management
- Avoid unnecessary clones of large data structures

## Testing Strategy

- Unit tests for individual components
- Integration tests for service interactions
- Mock external dependencies for testing
- Property-based testing where applicable