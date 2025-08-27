# Orbital Docker UI

![Build](https://github.com/Nechja/Orbital/workflows/Build/badge.svg)
![Release](https://github.com/Nechja/Orbital/workflows/Release/badge.svg)
![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![Platform](https://img.shields.io/badge/platform-linux%20%7C%20windows%20%7C%20macos-lightgrey)

A free, open-source Docker GUI

No subscriptions, no corporate restrictions, just a simple tool to manage your containers.

<img width="1228" height="766" alt="image" src="https://github.com/user-attachments/assets/1a7d88f9-b0d0-4b24-9db2-f88c59b7e929" />

  - Core Features
    - Real-time container monitoring via Docker events
    - Stack (Docker Compose) detection and management
    - Container stats (CPU, Memory, Network, Disk I/O)

  - UI/UX
    - An Interface!
    - Dark/Light/System theme support (For you light theme people)
    - System tray integration 
    - Reactive updates 
    - Search/filter functionality

  - Performance
    - Native performance with .NET 9

## Building from Source

### Prerequisites
- .NET 9 SDK
- Docker Engine running

### Build and Run
```bash

git clone https://github.com/Nechja/Orbital
cd Orbital

dotnet run
```

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
