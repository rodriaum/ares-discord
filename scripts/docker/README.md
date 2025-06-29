# Docker Scripts

Useful scripts to manage Docker containers for the Ares project.

## Usage

### Main Script
```powershell
# Build
.\scripts\docker\docker.ps1 build
.\scripts\docker\docker.ps1 build -NoCache
.\scripts\docker\docker.ps1 build -Service ares-api

# Start
.\scripts\docker\docker.ps1 start
.\scripts\docker\docker.ps1 start -Build

# Stop
.\scripts\docker\docker.ps1 stop
.\scripts\docker\docker.ps1 stop -Remove

# Restart
.\scripts\docker\docker.ps1 restart

# Logs
.\scripts\docker\docker.ps1 logs
.\scripts\docker\docker.ps1 logs -Service ares-api -Follow

# Status
.\scripts\docker\docker.ps1 status

# Clean
.\scripts\docker\docker.ps1 clean -All
```

### Individual Scripts
```powershell
# Build
.\scripts\docker\build.ps1
.\scripts\docker\build.ps1 -NoCache
.\scripts\docker\build.ps1 -Service ares-api

# Start
.\scripts\docker\start.ps1
.\scripts\docker\start.ps1 -Build

# Stop
.\scripts\docker\stop.ps1
.\scripts\docker\stop.ps1 -Remove -Volumes

# Logs
.\scripts\docker\logs.ps1
.\scripts\docker\logs.ps1 -Service ares-api -Follow

# Status
.\scripts\docker\status.ps1

# Clean
.\scripts\docker\clean.ps1 -All
```

## Usage Examples

```powershell
# Start everything for the first time
.\scripts\docker\docker.ps1 start -Build

# View API logs
.\scripts\docker\docker.ps1 logs -Service ares-api -Follow

# Restart only the API
.\scripts\docker\docker.ps1 stop
.\scripts\docker\docker.ps1 build -Service ares-api
.\scripts\docker\docker.ps1 start

# Clean everything and start over
.\scripts\docker\docker.ps1 clean -All
.\scripts\docker\docker.ps1 start -Build
```
```