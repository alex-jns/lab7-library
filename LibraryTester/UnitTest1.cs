using Library.Models;
using Library.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace LibraryTester;

/// <summary>
/// Common naming conventions for C# unit test functions include patterns such as:
/// MethodName_StateUnderTest_ExpectedBehavior.
/// In this case, the function is GetUser().
/// The state under test is "when user does not exist"
/// and the expected behavior is that it will return null.
/// </summary>
public class UnitTest1
{
    // Book Tests

    [Fact]
    public void GetBook_WhenBookDoesNotExist_ReturnsNull()
    {
        // Arrange - Create the object needed for testing
        var service = new LibraryService();

        // Act - Service attempts to find a book id that does not exist
        var result = service.GetBook(-1);

        // Assert - Expect to find null
        Assert.Null(result);
    }

    [Fact]
    public void GetBook_WhenBookExists_ReturnsMatchingBook()
    {
        // Arrange - Create what is needed needed for testing
        var service = new LibraryService();
        int bookId = 2; // i deleted book 1 so use 2 :)

        // Act - Service attempts to fetch bookId = 2 
        var result = service.GetBook(bookId);

        // Assert - Expect to return a non-null value (book = 2)
        Assert.NotNull(result);
        Assert.Equal(bookId, result.Id);
    }

    [Fact]
    public void ReadBooks_WhenFileContainsBook_LoadsBookDetails()
    {
        // Arrange - Create what is needed needed for testing
        var service = new LibraryService();

        // Act - Service attempts to fetch bookId = 2 
        var book = service.GetBook(2);

        // Assert - Book is not null and string properties match precisely
        Assert.NotNull(book);
        Assert.Equal("Slumber Party Massacre, The", book.Title);
        Assert.Equal("Alecia Polycote", book.Author);
        Assert.Equal("057219088-3", book.ISBN);
    }

    [Fact]
    public void ReadBooks_WhenFileIsEmpty_LoadsNoBooks()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create empty CSV files used for testing
            File.WriteAllText(booksPath, "");
            File.WriteAllText(usersPath, "");

            // Act - The constructor reads the empty files
            var service = new LibraryService(booksPath, usersPath);

            // Assert - No books should be loaded
            Assert.Empty(service.GetBooks());
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ReadBooks_WhenRowHasTooFewFields_SkipsRowAndLoadsValidBook()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create an incomplete row followed by a valid book (tests malformed data)
            File.WriteAllLines(booksPath, new[]
            {
                "1,Incomplete Book",
                "2,Valid Book,Test Author,123456"
            });

            File.WriteAllText(usersPath, "");

            // Act - The constructor reads the test files
            var service = new LibraryService(booksPath, usersPath);

            // Assert - The incomplete record should not be loaded
            Assert.Null(service.GetBook(1));

            // The valid record should still be loaded
            var book = service.GetBook(2);
            Assert.NotNull(book);
            Assert.Equal("Valid Book", book.Title);
            Assert.Equal("Test Author", book.Author);
            Assert.Equal("123456", book.ISBN);
            Assert.Single(service.GetBooks());
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void UpdateBook_WhenBookDoesNotExist_ThrowsException()
    {
        // Arrange - Create what is needed needed for testing
        var service = new LibraryService();

        // Updated information for a book that doesn't exist
        var book = new Book
        {
            Id = -1,
            Title = "Missing Book",
            Author = "Test Author",
            ISBN = "123456"
        };

        // Act - UpdateBook() will not find a book with ID = -1
        var exception = Record.Exception(() => service.UpdateBook(book));

        // Assert - We expect to catch the error
        var error = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Book not found.", error.Message);
    }

    [Fact]
    public void AddBook_WhenLibraryIsEmpty_AddsBookWithIdOne()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines and creates two empty CSV files used for testing
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            File.WriteAllText(booksPath, "");
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                ISBN = "123456"
            };

            // Act - First book in an empty CSV should have ID = 1
            service.AddBook(book);

            // Assert - Check if first book ID = 1
            Assert.Equal(1, book.Id);

            // Should be able to GetBook() and see its information
            var addedBook = service.GetBook(book.Id);
            Assert.NotNull(addedBook);
            Assert.Equal("Test Book", addedBook.Title);
            Assert.Equal("Test Author", addedBook.Author);
            Assert.Equal("123456", addedBook.ISBN);
            Assert.Equal(1, service.CountCopies(book.ISBN));
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AddBook_WhenBooksExist_AssignsNextId()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create a book that starts at ID = 5
            File.WriteAllText(booksPath, "5,Existing Book,Existing Author,111111");
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                ISBN = "123456"
            };

            // Act - Since the only book is ID = 5, next book added should be ID = 6
            service.AddBook(book);

            // Assert - Check if book ID = 6
            Assert.Equal(6, book.Id);

            // Should be able to GetBook() and see its information
            var addedBook = service.GetBook(book.Id);
            Assert.NotNull(addedBook);
            Assert.Equal("Test Book", addedBook.Title);
            Assert.Equal("Test Author", addedBook.Author);
            Assert.Equal("123456", addedBook.ISBN);
            Assert.Equal(1, service.CountCopies(book.ISBN));

            // The first book added should still be there too
            var existingBook = service.GetBook(5);
            Assert.NotNull(existingBook);
            Assert.Equal("Existing Book", existingBook.Title);
            Assert.Equal("Existing Author", existingBook.Author);
            Assert.Equal("111111", existingBook.ISBN);
            Assert.Equal(1, service.CountCopies(existingBook.ISBN));
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AddBook_WhenIdsHaveGaps_AssignsHighestIdPlusOne()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create existing books with a gap between their IDs.
            // Put the highest ID first to check that row order doesn't matter.
            File.WriteAllLines(booksPath, new[]
            {
                "5,Existing Book,Existing Author,111111",
                "2,Another Book,Another Author,222222"
            });
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            var book = new Book
            {
                Title = "New Book",
                Author = "New Author",
                ISBN = "333333"
            };

            // Act - Add the book to the library, should be assigned ID = 6
            service.AddBook(book);

            // Assert - New book receives the highest ID plus one
            Assert.Equal(6, book.Id);

            // Check that the new book was added
            var addedBook = service.GetBook(6);
            Assert.NotNull(addedBook);
            Assert.Equal("New Book", addedBook.Title);
            Assert.Equal("New Author", addedBook.Author);
            Assert.Equal("333333", addedBook.ISBN);

            // Check that both existing books remain
            Assert.NotNull(service.GetBook(2));
            Assert.NotNull(service.GetBook(5));
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void EditBook_WhenBookExists_UpdatesBookDetails()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create a book that starts at ID = 5
            File.WriteAllText(booksPath, "5,Original Title,Original Author,111111");
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            // Prepare to edit book whose ID = 5
            var updatedBook = new Book
            {
                Id = 5,
                Title = "Updated Title",
                Author = "Updated Author",
                ISBN = "222222"
            };

            // Act - Edit the book
            service.EditBook(updatedBook);

            // Assert - Check if the book was updated properly if the book exists
            var result = service.GetBook(5);

            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Author", result.Author);
            Assert.Equal("222222", result.ISBN);

            // Verify the changes were saved to CSV.
            var reloadedService = new LibraryService(booksPath, usersPath);
            var savedBook = reloadedService.GetBook(5);

            Assert.NotNull(savedBook);
            Assert.Equal(5, savedBook.Id);
            Assert.Equal("Updated Title", savedBook.Title);
            Assert.Equal("Updated Author", savedBook.Author);
            Assert.Equal("222222", savedBook.ISBN);
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void DeleteBook_WhenBookExists_RemovesBook()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create a book that starts at ID = 5
            File.WriteAllText(booksPath, "5,Test Book,Test Author,123456");
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            // Act - Delete the book
            service.DeleteBook(5);

            // Assert - Attempting to get the book should return null
            Assert.Null(service.GetBook(5));

            // Verify the deletion was saved to CSV.
            var reloadedService = new LibraryService(booksPath, usersPath);

            // Should still be null and empty on a reload
            Assert.Null(reloadedService.GetBook(5));
            Assert.Empty(reloadedService.GetBooks());
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    // User Tests

    [Fact]
    public void GetUser_WhenUserExists_ReturnsMatchingUser()
    {
        // Arrange - Create what is needed needed for testing
        var service = new LibraryService();
        int userId = 1;

        // Act - Service attempts to fetch userId = 1
        var result = service.GetUser(userId);

        // Assert - Expect to return a non-null value (user = 1)
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public void GetUser_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange - Create the object needed for testing
        var service = new LibraryService();

        // Act - Service attempts to find a user id that does not exist
        var result = service.GetUser(-1);

        // Assert - Expect to find null
        Assert.Null(result);
    }

    [Fact]
    public void AddUser_WhenLibraryIsEmpty_AddsUserWithIdOne()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create empty CSV files used for testing
            File.WriteAllText(booksPath, "");
            File.WriteAllText(usersPath, "");

            var service = new LibraryService(booksPath, usersPath);

            // Prepare the user to add
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com"
            };

            // Act - Add the user
            service.AddUser(user);

            // Assert - Check the assigned ID and user details
            Assert.Equal(1, user.Id);

            var addedUser = service.GetUser(user.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("Test User", addedUser.Name);
            Assert.Equal("test@example.com", addedUser.Email);

            // Checks that the collection has exactly one user
            Assert.Single(service.GetUsers());

            // Verify the changes were saved to CSV
            var reloadedService = new LibraryService(booksPath, usersPath);
            var savedUser = reloadedService.GetUser(user.Id);

            // Check again after reloading
            Assert.NotNull(savedUser);
            Assert.Equal(1, savedUser.Id);
            Assert.Equal("Test User", savedUser.Name);
            Assert.Equal("test@example.com", savedUser.Email);
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void EditUser_WhenUserExists_UpdatesUserDetails()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create an existing user with ID = 5
            File.WriteAllText(booksPath, "");
            File.WriteAllText(usersPath, "5,Original User,original@example.com");

            var service = new LibraryService(booksPath, usersPath);

            // Prepare the updated user details
            var updatedUser = new User
            {
                Id = 5,
                Name = "Updated User",
                Email = "updated@example.com"
            };

            // Act - Edit the user
            service.EditUser(updatedUser);

            // Assert - Check the ID and updated user details
            var result = service.GetUser(5);

            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.Equal("Updated User", result.Name);
            Assert.Equal("updated@example.com", result.Email);
            Assert.Single(service.GetUsers());

            // Verify the changes were saved to CSV
            var reloadedService = new LibraryService(booksPath, usersPath);
            var savedUser = reloadedService.GetUser(5);

            // Check again after reloading
            Assert.NotNull(savedUser);
            Assert.Equal(5, savedUser.Id);
            Assert.Equal("Updated User", savedUser.Name);
            Assert.Equal("updated@example.com", savedUser.Email);
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void DeleteUser_WhenUserExists_RemovesUser()
    {
        // Arrange - Create a temporary path used for testing
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        try
        {
            // Defines the path for the test CSVs
            string booksPath = Path.Combine(folder, "Books.csv");
            string usersPath = Path.Combine(folder, "Users.csv");

            // Create an existing user with ID = 5
            File.WriteAllText(booksPath, "");
            File.WriteAllText(usersPath, "5,Test User,test@example.com");

            var service = new LibraryService(booksPath, usersPath);

            // Act - Delete the user
            service.DeleteUser(5);

            // Assert - Attempting to get the user should return null
            Assert.Null(service.GetUser(5));
            Assert.Empty(service.GetUsers());

            // Verify the deletion was saved to CSV
            var reloadedService = new LibraryService(booksPath, usersPath);

            // Should still be null and empty on a reload
            Assert.Null(reloadedService.GetUser(5));
            Assert.Empty(reloadedService.GetUsers());
        }
        finally // Clean up regardless of test pass
        {
            // Delete the folder and the CSV files
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void UserValidation_WhenEmailIsInvalid_ReturnsValidationError()
    {
        // Arrange - Prepare a user with an invalid email
        var user = new User
        {
            Name = "Test User",
            Email = "not-an-email"
        };

        var context = new ValidationContext(user);
        var results = new List<ValidationResult>();

        // Act - Run the validation rules used by the form
        bool isValid = Validator.TryValidateObject(
            user,
            context,
            results,
            validateAllProperties: true);

        // Assert - Validation should fail for the email field
        Assert.False(isValid);
        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(User.Email)));
    }
}
