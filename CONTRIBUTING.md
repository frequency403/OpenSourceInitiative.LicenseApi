# Contributing to OpenSourceInitiative.LicenseApi

Thank you for your interest in contributing! This project welcomes issues and pull requests from the community.

Before contributing, please take a moment to read this document, our [Code of Conduct](CODE_OF_CONDUCT.md), and our [Security Policy](SECURITY.md).

## Development quickstart

Requirements:
- .NET SDK 10 (latest) — repo also targets .NET Standard 2.0 for broad compatibility

Build and test:
```
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

CI enforces line coverage ≥ 90% (Coverlet/Cobertura). Please include or update tests with your changes.

## Project layout
- `OpenSourceInitiative.LicenseApi/` — core client library
- `OpenSourceInitiative.LicenseApi.DependencyInjection/` — DI registration extensions
- `OpenSourceInitiative.LicenseApi.Example/` — runnable sample app
- `OpenSourceInitiative.LicenseApi.Tests/` — unit/integration tests

## Coding style
- Follow existing file/project style (formatting, naming, imports).
- Prefer clear, self‑documenting code over comments. Add comments where intent is non‑obvious.
- Public API changes require a major version bump (SemVer). Consider API stability.

## Pull requests
1. Open or reference an issue for non‑trivial changes.
2. Include tests and documentation updates.
3. Keep commits focused and messages clear (conventional style appreciated but not required).
4. Ensure `dotnet test -c Release` passes locally.

## Running the example
```
dotnet run --project OpenSourceInitiative.LicenseApi.Example
```

## Reporting issues and asking questions
- Use GitHub Issues for bugs and feature requests.
- For security issues, do NOT open a public issue — see [SECURITY.md](SECURITY.md).

## Contact
For Code of Conduct incidents or questions, email: frequency403@gmail.com
