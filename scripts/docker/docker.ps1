# Main script to manage Docker
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("build", "start", "stop", "restart", "logs", "status", "clean")]
    [string]$Action,
    
    [string]$Service = "",
    [switch]$NoCache,
    [switch]$Build,
    [switch]$Follow,
    [switch]$Remove,
    [switch]$Volumes,
    [switch]$All
)

# Navigate to project root
Set-Location $PSScriptRoot\..\..

switch ($Action) {
    "build" {
        & "$PSScriptRoot\build.ps1" -NoCache:$NoCache -Service $Service
    }
    "start" {
        & "$PSScriptRoot\start.ps1" -Build:$Build
    }
    "stop" {
        & "$PSScriptRoot\stop.ps1" -Remove:$Remove -Volumes:$Volumes
    }
    "restart" {
        & "$PSScriptRoot\stop.ps1" -Remove
        & "$PSScriptRoot\start.ps1" -Build
    }
    "logs" {
        & "$PSScriptRoot\logs.ps1" -Service $Service -Follow:$Follow
    }
    "status" {
        & "$PSScriptRoot\status.ps1"
    }
    "clean" {
        & "$PSScriptRoot\clean.ps1" -All:$All -Images:$Images -Volumes:$Volumes
    }
}