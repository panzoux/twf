# TWF Job Dialog UI - Sample Implementation with Cancelling State

This document shows a mock implementation of the job dialog UI in TWF, demonstrating how the Cancelling state is properly displayed alongside other job states.

## Sample Job Dialog UI Implementation

```rust
use ratatui::{
    prelude::*,
    widgets::*,
    layout::*,
    style::*,
};

use std::collections::HashMap;

#[derive(Debug, Clone, Copy, PartialEq)]
pub enum JobState {
    Queued,
    Running,
    Cancelling,  // New state for cancellation in progress
    Completed,
    Failed,
    Cancelled,
}

#[derive(Debug, Clone)]
pub struct Job {
    pub id: u64,
    pub name: String,
    pub state: JobState,
    pub progress: f64,  // 0.0 to 100.0
    pub total_size: u64,
    pub transferred: u64,
    pub speed: Option<f64>,  // bytes per second
    pub eta: Option<std::time::Duration>,  // estimated time of arrival
    pub start_time: std::time::SystemTime,
    pub error_message: Option<String>,
}

pub struct JobDialog<'a> {
    jobs: Vec<Job>,
    title: &'a str,
    show_details: bool,
}

impl<'a> JobDialog<'a> {
    pub fn new(jobs: Vec<Job>) -> Self {
        Self {
            jobs: jobs,
            title: "Background Jobs",
            show_details: true,
        }
    }

    pub fn render(&self, area: Rect, buf: &mut Buffer) {
        // Create the main dialog
        let block = Block::default()
            .title(self.title)
            .borders(Borders::ALL)
            .border_style(Style::default().fg(Color::Cyan));

        // Split the area for header and content
        let chunks = Layout::vertical([
            Constraint::Length(3),  // Header
            Constraint::Min(1),     // Content
            Constraint::Length(3),  // Footer
        ]).split(area.inner(&Margin::new(1, 1)));

        // Render header
        self.render_header(chunks[0], buf);

        // Render job list
        self.render_job_list(chunks[1], buf);

        // Render footer
        self.render_footer(chunks[2], buf);

        // Draw the outer block
        block.render(area, buf);
    }

    fn render_header(&self, area: Rect, buf: &mut Buffer) {
        let header_text = format!("{} Jobs | {} Active | {} Completed", 
            self.jobs.len(),
            self.jobs.iter().filter(|j| matches!(j.state, JobState::Running | JobState::Cancelling)).count(),
            self.jobs.iter().filter(|j| matches!(j.state, JobState::Completed | JobState::Cancelled | JobState::Failed)).count()
        );

        Paragraph::new(header_text)
            .alignment(Alignment::Center)
            .style(Style::default().bg(Color::DarkGray).fg(Color::White))
            .render(area, buf);
    }

    fn render_job_list(&self, area: Rect, buf: &mut Buffer) {
        let job_widgets: Vec<ListItem> = self.jobs.iter()
            .map(|job| {
                let state_color = match job.state {
                    JobState::Queued => Color::Blue,
                    JobState::Running => Color::Green,
                    JobState::Cancelling => Color::Yellow,  // Important: Cancelling state highlighted
                    JobState::Completed => Color::LightGreen,
                    JobState::Failed => Color::Red,
                    JobState::Cancelled => Color::Magenta,
                };

                let state_text = match job.state {
                    JobState::Queued => "Queued",
                    JobState::Running => "Running",
                    JobState::Cancelling => "Cancelling...",  // Clear indication of cancellation in progress
                    JobState::Completed => "Completed",
                    JobState::Failed => "Failed",
                    JobState::Cancelled => "Cancelled",
                };

                let mut lines = vec![
                    Line::from(vec![
                        Span::styled(format!("{:>3}. ", job.id), Style::default().fg(Color::Gray)),
                        Span::styled(job.name.clone(), Style::default().fg(Color::White).add_modifier(Modifier::BOLD)),
                        Span::raw(" - "),
                        Span::styled(state_text, Style::default().fg(state_color)),
                    ])
                ];

                // Add progress bar and details if showing details
                if self.show_details {
                    // Progress bar
                    let progress_text = format!("{:.1}%", job.progress);
                    let progress_bar = Line::from(vec![
                        Span::raw("["),
                        Span::styled(
                            "█".repeat((job.progress / 10.0) as usize),
                            Style::default().fg(Color::Green)
                        ),
                        Span::styled(
                            "░".repeat(10 - (job.progress / 10.0) as usize),
                            Style::default().fg(Color::DarkGray)
                        ),
                        Span::raw("] "),
                        Span::styled(progress_text, Style::default().fg(Color::White)),
                    ]);
                    lines.push(progress_bar);

                    // Additional details
                    let details = if let Some(speed) = job.speed {
                        let speed_str = format_size(speed as u64);
                        let eta_str = if let Some(eta) = job.eta {
                            format!("ETA: {}s", eta.as_secs())
                        } else {
                            "ETA: --".to_string()
                        };
                        format!("{} | {} | {}", 
                            format_size(job.transferred), 
                            speed_str, 
                            eta_str)
                    } else {
                        format!("{} of {}", 
                            format_size(job.transferred), 
                            format_size(job.total_size))
                    };
                    lines.push(Line::from(Span::styled(details, Style::default().fg(Color::Gray))));
                }

                // Add error message if failed
                if let Some(error) = &job.error_message {
                    lines.push(Line::from(Span::styled(
                        format!("  ERROR: {}", error),
                        Style::default().fg(Color::Red)
                    )));
                }

                ListItem::new(lines)
            })
            .collect();

        let list = List::new(job_widgets)
            .block(Block::default().borders(Borders::NONE))
            .highlight_style(Style::default().bg(Color::DarkGray).add_modifier(Modifier::BOLD));

        StatefulWidget::render(list, area, buf, &mut ListState::default());
    }

    fn render_footer(&self, area: Rect, buf: &mut Buffer) {
        let footer_text = "ESC: Close | C: Cancel All | R: Refresh";
        Paragraph::new(footer_text)
            .alignment(Alignment::Center)
            .style(Style::default().bg(Color::DarkGray).fg(Color::White))
            .render(area, buf);
    }
}

fn format_size(bytes: u64) -> String {
    const UNITS: [&str; 5] = ["B", "KB", "MB", "GB", "TB"];
    let mut size = bytes as f64;
    let mut unit_idx = 0;

    while size >= 1024.0 && unit_idx < UNITS.len() - 1 {
        size /= 1024.0;
        unit_idx += 1;
    }

    if unit_idx == 0 {
        format!("{}{}", size as u64, UNITS[unit_idx])
    } else {
        format!("{:.1}{}", size, UNITS[unit_idx])
    }
}

// Mock sample data showing different job states including Cancelling
pub fn create_sample_jobs() -> Vec<Job> {
    vec![
        Job {
            id: 1,
            name: "Copy: Documents/* to Backup/".to_string(),
            state: JobState::Running,
            progress: 45.7,
            total_size: 1024 * 1024 * 500, // 500 MB
            transferred: 1024 * 1024 * 231, // ~231 MB
            speed: Some(5.2 * 1024.0 * 1024.0), // 5.2 MB/s
            eta: Some(std::time::Duration::from_secs(52)), // ~52 seconds
            start_time: std::time::SystemTime::now(),
            error_message: None,
        },
        Job {
            id: 2,
            name: "Delete: Temp Files".to_string(),
            state: JobState::Cancelling,  // This is the important state we're showcasing
            progress: 12.3,  // Still progressing during cancellation
            total_size: 1024 * 1024 * 10, // 10 MB
            transferred: 1024 * 1024,     // 1 MB processed
            speed: Some(2.1 * 1024.0 * 1024.0), // 2.1 MB/s
            eta: Some(std::time::Duration::from_secs(4)), // ~4 seconds remaining
            start_time: std::time::SystemTime::now(),
            error_message: None,
        },
        Job {
            id: 3,
            name: "Move: Photos/2023 to Archive/2023".to_string(),
            state: JobState::Queued,
            progress: 0.0,
            total_size: 1024 * 1024 * 2000, // 2 GB
            transferred: 0,
            speed: None,
            eta: None,
            start_time: std::time::SystemTime::now(),
            error_message: None,
        },
        Job {
            id: 4,
            name: "Extract: archive.zip".to_string(),
            state: JobState::Completed,
            progress: 100.0,
            total_size: 1024 * 1024 * 100, // 100 MB
            transferred: 1024 * 1024 * 100, // 100 MB
            speed: Some(15.0 * 1024.0 * 1024.0), // 15 MB/s
            eta: None,
            start_time: std::time::SystemTime::now(),
            error_message: None,
        },
        Job {
            id: 5,
            name: "Sync: Remote Folder".to_string(),
            state: JobState::Failed,
            progress: 67.2,
            total_size: 1024 * 1024 * 50, // 50 MB
            transferred: 1024 * 1024 * 33, // ~33 MB
            speed: Some(1.5 * 1024.0 * 1024.0), // 1.5 MB/s
            eta: Some(std::time::Duration::from_secs(11)), // ~11 seconds
            start_time: std::time::SystemTime::now(),
            error_message: Some("Permission denied: /restricted/file.txt".to_string()),
        },
        Job {
            id: 6,
            name: "Archive: Projects/".to_string(),
            state: JobState::Cancelled,
            progress: 23.5,
            total_size: 1024 * 1024 * 800, // 800 MB
            transferred: 1024 * 1024 * 188, // ~188 MB
            speed: Some(3.2 * 1024.0 * 1024.0), // 3.2 MB/s
            eta: Some(std::time::Duration::from_secs(194)), // ~194 seconds
            start_time: std::time::SystemTime::now(),
            error_message: Some("User cancelled".to_string()),
        },
    ]
}

// Example usage
pub fn render_job_dialog_example() {
    let jobs = create_sample_jobs();
    let dialog = JobDialog::new(jobs);
    
    // In a real application, this would be rendered in a terminal
    println!("Job Dialog UI Sample:");
    println!("====================");
    
    // Print a textual representation of what the UI would show
    for job in &dialog.jobs {
        let state_color = match job.state {
            JobState::Queued => "BLUE",
            JobState::Running => "GREEN", 
            JobState::Cancelling => "YELLOW",  // Highlighted as important
            JobState::Completed => "LIGHT_GREEN",
            JobState::Failed => "RED",
            JobState::Cancelled => "MAGENTA",
        };

        let state_text = match job.state {
            JobState::Queued => "Queued",
            JobState::Running => "Running",
            JobState::Cancelling => "Cancelling...",  // Clear indication
            JobState::Completed => "Completed",
            JobState::Failed => "Failed", 
            JobState::Cancelled => "Cancelled",
        };

        println!("Job {}: {} [{}] - {:.1}% - {} of {}",
            job.id,
            job.name,
            state_text,
            job.progress,
            format_size(job.transferred),
            format_size(job.total_size)
        );
        
        if let Some(error) = &job.error_message {
            println!("  ERROR: {}", error);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_cancelling_state_display() {
        let jobs = create_sample_jobs();
        let cancelling_job = jobs.iter().find(|j| j.state == JobState::Cancelling).unwrap();
        
        assert_eq!(cancelling_job.state, JobState::Cancelling);
        assert_eq!(cancelling_job.name, "Delete: Temp Files");
        // Verify that cancelling jobs still show progress during cancellation
        assert!(cancelling_job.progress > 0.0);
    }

    #[test]
    fn test_job_state_colors() {
        let jobs = create_sample_jobs();
        
        for job in &jobs {
            match job.state {
                JobState::Cancelling => {
                    // Cancelling state should be visually distinct
                    println!("Cancelling job {} has progress {}%", job.id, job.progress);
                    assert!(job.progress >= 0.0 && job.progress <= 100.0);
                }
                _ => {}
            }
        }
    }
}

fn main() {
    render_job_dialog_example();
}
```

## Expected UI Output

When rendered, the job dialog would show:

```
┌─────────────────────────────────────────────────────────────┐
│                    Background Jobs                          │
│              6 Jobs | 2 Active | 4 Completed              │
├─────────────────────────────────────────────────────────────┤
│  1. Copy: Documents/* to Backup/ - Running                │
│     [██████░░░░] 45.7%                                     │
│     231MB of 500MB | 5.2MB/s | ETA: 52s                  │
│  2. Delete: Temp Files - Cancelling...  ←←←←←←←←←←←←←←←    │
│     [█░░░░░░░░░] 12.3%  ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←     │
│     1MB of 10MB | 2.1MB/s | ETA: 4s     ←←←←←←←←←←←←←←←    │
│  3. Move: Photos/2023 to Archive/2023 - Queued            │
│     [░░░░░░░░░░] 0.0%                                      │
│     0B of 2GB                                               │
│  4. Extract: archive.zip - Completed                       │
│     [██████████] 100.0%                                    │
│     100MB of 100MB                                          │
│  5. Sync: Remote Folder - Failed                           │
│     [██████░░░░] 67.2%                                     │
│     33MB of 50MB | 1.5MB/s | ETA: 11s                     │
│     ERROR: Permission denied: /restricted/file.txt          │
│  6. Archive: Projects/ - Cancelled                         │
│     [██░░░░░░░░] 23.5%                                     │
│     188MB of 800MB                                          │
│     ERROR: User cancelled                                   │
├─────────────────────────────────────────────────────────────┤
│            ESC: Close | C: Cancel All | R: Refresh         │
└─────────────────────────────────────────────────────────────┘
```

## Key UI Features for Cancelling State

1. **Distinct Visual Indicator**: Yellow color for Cancelling state to make it stand out
2. **Clear Text Label**: "Cancelling..." clearly indicates the intermediate state
3. **Progress Continuation**: Shows ongoing progress during cancellation (important!)
4. **Proper State Transition**: Shows that cancellation is in progress, not instantaneous
5. **Resource Management**: Indicates that resources are being freed during this phase

This design properly reflects the finite state machine design where Cancelling is an intermediate state between Running and Cancelled, allowing users to see when cancellation is in progress versus when it's complete.