# CI/CD Workflows

This directory contains GitHub Actions workflows for the CivOne project.

## release.yml - Build and Release Workflow

This workflow automates the build process and creates GitHub releases.

### Triggers

The workflow runs on:

1. **Push to master branch**: Builds and tests the code on every push to master
2. **Git tags matching `v*.*.*`**: Creates a GitHub Release when a semantic version tag is pushed

### What it does

#### Build Job (runs on every trigger)
- Checks out the repository
- Sets up .NET 9.0 SDK
- Restores NuGet dependencies
- Builds the solution in Release configuration
- Runs tests (failures are informational only, won't block release)

#### Release Job (runs only on tag push)
- Requires the build job to succeed
- Rebuilds the project in Release configuration
- Packages the binaries into a ZIP file
- Creates a GitHub Release with:
  - Version number from the tag
  - Auto-generated release notes based on commits
  - Compiled binaries as downloadable assets

### How to create a release

To create a new release:

1. Make sure all your changes are committed and pushed to master
2. Create and push a semantic version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

This will trigger the workflow to:
- Build and test the code
- Create a GitHub Release named "Release v1.0.0"
- Attach the compiled binaries as a ZIP file
- Generate release notes from recent commits

### Semantic Versioning

Follow [Semantic Versioning](https://semver.org/) guidelines:
- **MAJOR** version: Incompatible API changes (e.g., v2.0.0)
- **MINOR** version: Add functionality in a backwards compatible manner (e.g., v1.1.0)
- **PATCH** version: Backwards compatible bug fixes (e.g., v1.0.1)

### Requirements

- Tags must follow the pattern `v*.*.*` (e.g., `v1.0.0`, `v2.1.3`)
- The workflow requires `contents: write` permission (already configured)
- Build must succeed before a release is created
