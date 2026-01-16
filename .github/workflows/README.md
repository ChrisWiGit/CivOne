# CI/CD Workflows

This directory contains GitHub Actions workflows for the CivOne project.

## `release.yml` – Build and Release Workflow

This workflow builds, tests, publishes, and releases CivOne for multiple platforms.

## Triggers

The workflow runs on:

1. **Push to `master` branch**

   * Builds and tests the code on every push to ensure stability.

2. **Git tags matching `v*.*.*`**

   * Builds platform-specific binaries and creates a GitHub Release when a semantic version tag is pushed.

## What it does

### Build Job (runs on every trigger)

Runs once on Linux to validate the codebase.

* Checks out the repository
* Sets up **.NET 9.0 SDK**
* Restores NuGet dependencies
* Builds the solution in **Release** configuration
* Runs tests

  * Test failures are **informational only** and do **not block** the release process

### Release Job (runs only on tag push)

Runs after the build job and publishes binaries for multiple platforms using a matrix build.

#### Platforms

* **Linux x64** (`linux-x64`)
* **Windows x64** (`win-x64`)

#### Steps

* Checks out the repository
* Sets up **.NET 9.0 SDK**
* Restores dependencies
* Publishes the application using `dotnet publish`

  * Self-contained (no .NET runtime required)
  * Single-file executables
* Packages the published output into platform-specific ZIP archives
* Uploads build artifacts

### GitHub Release Job

* Downloads all platform artifacts
* Creates a GitHub Release with:

  * Version number taken from the Git tag
  * Auto-generated release notes
  * Separate downloadable ZIP files per platform:

    * `CivOne-vX.Y.Z-linux-x64.zip`
    * `CivOne-vX.Y.Z-win-x64.zip`

## How to create a release

To create a new release:

1. Ensure all changes are committed and pushed to `master`
2. Create and push a semantic version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

This will trigger the workflow to:

* Build and test the code
* Publish Linux and Windows binaries
* Create a GitHub Release named **“Release v1.0.0”**
* Attach platform-specific ZIP archives
* Generate release notes from recent commits

## Semantic Versioning

The project follows [Semantic Versioning](https://semver.org/):

* **MAJOR** – Incompatible API changes (e.g. `v2.0.0`)
* **MINOR** – Backwards-compatible functionality (e.g. `v1.1.0`)
* **PATCH** – Backwards-compatible bug fixes (e.g. `v1.0.1`)

## Requirements & Notes

* Tags must follow the pattern `v*.*.*` (e.g. `v1.0.0`, `v2.1.3`)
* The workflow requires `contents: write` permission (already configured)
* The **build job must complete** before any release artifacts are published
* Release binaries are **self-contained** and do **not require a pre-installed .NET runtime**