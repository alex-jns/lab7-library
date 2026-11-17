# Note

Because of the way the unit tests need to test CRUD operations,
LibraryService.cs was modified to allow the tests to provide a different file path.

Library was included with the modified files to more easily allow using
LibraryTester to perform its 17 tests.

Requires a project reference and "dotnet test" in the terminal.

Uses xUnit.
