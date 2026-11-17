# About This Lab

Because of the way the unit tests need to test CRUD operations,
LibraryService.cs was modified to allow the tests to provide a different file path.

Library was included with the modified files to more easily allow using
Library.Tests to perform its 17 tests.

Requires a project reference and "dotnet test" in the terminal.

Uses xUnit.

# Continuous Integration

The workflow for GitHub Actions is located at .github/workflows/ci.yml

Restores, builds, and tests using Library.slnx and Library.Tests

See the testing/service-failure branch for induced failure testing of LibraryServices.cs
