# Quantum Course Helper

A powerful course selection tool for zdbk, built with .NET 8 and Electron.NET.

## Features

- Course list scraping and information gathering
- Teacher rating integration
- Course selection probability calculation
- Graduation requirement checking
- Smart course filtering
- Automated schedule optimization

## Project Structure

- **Quantum.Core**: Contains the core business logic and interfaces
- **Quantum.Infrastructure**: Implements the core interfaces and handles external interactions
- **Quantum.UI**: Blazor-based user interface with Electron.NET integration

## Development Requirements

- .NET 8 SDK
- Node.js (for Electron.NET)
- Visual Studio 2022 or later (recommended)

## Getting Started

1. Clone the repository
2. Install the required dependencies:
   ```bash
   dotnet restore
   ```
3. Run the application:
   ```bash
   cd Quantum.UI
   dotnet electronize start
   ```

## TODO List

- [ ] Implement zdbk authentication logic
- [ ] Implement course scraping functionality
- [ ] Implement graduation requirement parsing
- [ ] Add course filtering algorithms
- [ ] Add schedule optimization algorithms
- [ ] Complete UI implementation
- [ ] Add mobile support

## Contributing

This project is currently under development. The core scraping and authentication logic will be implemented separately.

## License

[MIT License](LICENSE)
