# Contributing to FixWidget

First off, thank you for considering contributing to FixWidget! 🎉

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When creating a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** - Include message JSON, screenshots, logs
- **Describe the behavior you observed** and what you expected
- **Include your environment**: Windows version, .NET version, WebView2 version

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description** of the suggested enhancement
- **Explain why this enhancement would be useful**
- **List some examples** of how it would be used

### Pull Requests

1. Fork the repository
2. Create a new branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. Make your changes
4. Test thoroughly
5. Commit with clear messages:
   ```bash
   git commit -m "Add feature: description of feature"
   ```
6. Push to your fork:
   ```bash
   git push origin feature/my-new-feature
   ```
7. Open a Pull Request

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/fixwidget.git
cd fixwidget

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

## Coding Guidelines

### C# Style

- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small

### Code Organization

- Place models in `Models/` folder
- Place services in `Services/` folder
- Keep UI logic in code-behind files
- Use async/await for I/O operations

### Comments

- Write clear, concise comments
- Explain *why*, not *what*
- Use XML documentation for public APIs
- Keep comments up-to-date with code changes

## Testing

- Test on different Windows versions (10, 11)
- Test with various screen resolutions
- Test with different widget URLs
- Test edge cases (invalid JSON, network errors, etc.)

## Commit Messages

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit first line to 72 characters
- Reference issues and pull requests when relevant

Examples:
```
Add support for custom window shapes
Fix crash when ntfy connection drops
Update README with new configuration options
```

## Questions?

Feel free to open an issue with the `question` label!

Thank you for contributing! 🚀
