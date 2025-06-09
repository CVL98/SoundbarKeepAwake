# SoundbarKeepAwake

## Overview
A simple utility that prevents audio devices from going to sleep by periodically
playing an inaudible beep. Particularly useful for Soundbars that automatically
power down when inactive.

## Requirements
- .NET 6.0 or higher
- Windows operating system

## Build Instructions
```
# Restore dependencies
dotnet restore

# Add NAudio package (if not already referenced)
dotnet add package NAudio

# Build the application
dotnet build

# Publish a release version
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o C:\Programs\SoundbarKeepAwake
```

## Installation
Copy the published files to your preferred location. No formal installation required.

## Usage
Run the executable from the installation directory:
```
C:\Programs\SoundbarKeepAwake\SoundbarKeepAwake.exe
```

The app will:
- Search for audio devices containing "Soundbar" in their name
- Play a beep every so often to keep the device awake
- Automatically reconnect if the device is disconnected
- Can be stopped gracefully with Ctrl+C

## Configuration
Edit the constants at the top of Program.cs to customize:
- DeviceNameToFind: Name of your audio device (default: "Soundbar")
- BeepIntervalMinutes: How often to play the beep (default: 10 minutes)
- RetryIntervalSeconds: How often to look for the device when not found (default: 60 seconds)

## Technical Features
- Efficiently loads audio data once at startup
- Properly manages audio device resources
- Handles device disconnections and reconnections
- Provides error feedback for playback issues
- Uses asynchronous operations for better performance
- Graceful termination with resource cleanup

## PowerShell Commands
```
# Stop the application if it's running
Stop-Process -Name SoundbarKeepAwake

# Check if the application is running
Get-Process -Name SoundbarKeepAwake
```