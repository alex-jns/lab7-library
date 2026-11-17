using Library.Models;

namespace Library.Services;

public class LibraryService
{
    private readonly string BooksPath;
    private readonly string UsersPath;

    public LibraryService(string booksPath = "./Data/Books.csv", string usersPath = "./Data/Users.csv")
    {
        BooksPath = booksPath;
        UsersPath = usersPath;

        ReadBooks();
        ReadUsers();
    }

    private List<Book> books = new List<Book>();
    private List<User> users = new List<User>();

    private readonly object _lock = new();

    public Dictionary<Book, int> GetBooks()
    {
        var booksDict = new Dictionary<Book, int>();

        // Each row in Books.csv is one physical copy with its own Id, so copies of the
        // same book share an ISBN, not an Id.
        // Count only copies that aren't currently borrowed.
        var bookGroups = books.GroupBy(b => b.ISBN).Select(bookGroup => new { Book = bookGroup.First(), Count = bookGroup.Count(b => !IsBorrowed(b)) });

        foreach (var group in bookGroups)
        {
            booksDict[group.Book] = group.Count;
        }

        return booksDict;
    }

    private readonly Dictionary<User, List<Book>> borrowedBooks = new Dictionary<User, List<Book>>();

    // ----- Books -----

    public void AddBook(Book book)
    {
        lock (_lock)
        {
            book.Id = books.Count == 0 ? 1 : books.Max(b => b.Id) + 1;
            books.Add(book);
            WriteBooks();
        }
    }

    public void EditBook(Book updated)
    {
        lock (_lock)
        {
            var book = books.FirstOrDefault(b => b.Id == updated.Id);
            if (book == null) return;

            book.Title = updated.Title;
            book.Author = updated.Author;
            book.ISBN = updated.ISBN;
            WriteBooks();
        }
    }

    public int CountCopies(string isbn)
    {
        return books.Count(b => b.ISBN == isbn);
    }

    // Removes one copy (one row).
    public void DeleteBook(int id)
    {
        lock (_lock)
        {
            books.RemoveAll(b => b.Id == id);

            foreach (var borrowed in borrowedBooks.Values)
            {
                borrowed.RemoveAll(b => b.Id == id);
            }

            WriteBooks();
        }
    }

    // Removes every copy (every row) with this ISBN.
    public void DeleteAllCopies(string isbn)
    {
        lock (_lock)
        {
            books.RemoveAll(b => b.ISBN == isbn);

            foreach (var borrowed in borrowedBooks.Values)
            {
                borrowed.RemoveAll(b => b.ISBN == isbn);
            }

            WriteBooks();
        }
    }

    public void UpdateBook(Book book)
    {
        lock (_lock)
        {
            var copies = books.Where(b => b.Id == book.Id).ToList();
            if (copies.Count == 0)
                throw new InvalidOperationException("Book not found.");

            foreach (var copy in copies)
            {
                copy.Title = book.Title;
                copy.Author = book.Author;
                copy.ISBN = book.ISBN;
            }

            WriteBooks();
        }
    }

    public Book? GetBook(int bookId)
    {
        return books.FirstOrDefault(b => b.Id != bookId); // Purposefully bad comparison
    }

    private void WriteBooks()
    {
        var lines = books.Select(b => string.Join(",",
            b.Id,
            b.Title.Replace("\"", ""),
            b.Author.Replace("\"", ""),
            b.ISBN.Replace("\"", "")));

        File.WriteAllLines(BooksPath, lines);
    }

    private void ReadBooks()
    {
        try
        {
            foreach (var line in File.ReadLines(BooksPath))
            {
                var fields = line.Replace("\"", "").Split(',');

                if (fields.Length >= 4)
                {
                    // "Slumber Party Massacre, The"
                    // Everything between the ID and the last 2 fields (Author, ISBN) are the title
                    var book = new Book
                    {
                        Id = int.Parse(fields[0].Trim()),
                        Title = string.Join(",", fields[1..^2]).Trim(),
                        Author = fields[^2].Trim(),
                        ISBN = fields[^1].Trim()
                    };

                    books.Add(book);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // ----- Users -----

    public void AddUser(User user)
    {
        lock (_lock)
        {
            user.Id = users.Count == 0 ? 1 : users.Max(u => u.Id) + 1;
            users.Add(user);
            WriteUsers();
        }
    }

    public void EditUser(User updated)
    {
        lock (_lock)
        {
            var user = users.FirstOrDefault(u => u.Id == updated.Id);
            if (user == null) return;

            user.Name = updated.Name;
            user.Email = updated.Email;
            WriteUsers();
        }
    }

    public void DeleteUser(int id)
    {
        lock (_lock)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            users.Remove(user);
            borrowedBooks.Remove(user);
            WriteUsers();
        }
    }

    private void WriteUsers()
    {
        var lines = users.Select(b => string.Join(",",
            b.Id,
            b.Name,
            b.Email));

        File.WriteAllLines(UsersPath, lines);
    }

    public List<User> GetUsers()
    {
        return users;
    }

    private void ReadUsers()
    {
        try
        {
            foreach (var line in File.ReadLines(UsersPath))
            {
                var fields = line.Split(',');

                if (fields.Length >= 3) // Ensure there are enough fields
                {
                    var user = new User
                    {
                        Id = int.Parse(fields[0].Trim()),
                        Name = fields[1].Trim(),
                        Email = fields[2].Trim()
                    };

                    users.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public User? GetUser(int id)
    {
        return users.FirstOrDefault(u => u.Id == id);
    }

    // ----- Borrowing -----
    // Borrowed books are tracked in memory only and are not saved to the CSV files.
    // A borrowed copy stays in the books list (so it is still saved) but isn't available.


    // Helper method to return true if they book is already in the borrowedBooks list
    // Call this method before you let another user borrow it.
    private bool IsBorrowed(Book book)
    {
        return borrowedBooks.Values.Any(borrowed => borrowed.Contains(book));
    }

    public Dictionary<User, List<Book>> GetBorrowedBooks()
    {
        return borrowedBooks;
    }

    public List<Book> GetBorrowedBooks(int userId)
    {
        var user = users.FirstOrDefault(u => u.Id == userId);

        if (user == null || !borrowedBooks.ContainsKey(user))
        {
            return new List<Book>();
        }

        return borrowedBooks[user];
    }

    public int CountAvailableCopies(string isbn)
    {
        return books.Count(b => b.ISBN == isbn && !IsBorrowed(b));
    }

    // Lends the user any available copy with this ISBN.
    public void BorrowBook(int userId, string isbn)
    {
        lock (_lock)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);
            var copy = books.FirstOrDefault(b => b.ISBN == isbn && !IsBorrowed(b));
            if (user == null || copy == null) return;

            if (!borrowedBooks.ContainsKey(user))
            {
                borrowedBooks.Add(user, new List<Book>());
            }

            borrowedBooks[user].Add(copy);
        }
    }

    public void ReturnBook(int userId, int bookId)
    {
        lock (_lock)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null || !borrowedBooks.ContainsKey(user))
            {
                return;
            }

            borrowedBooks[user].RemoveAll(b => b.Id == bookId);
        }
    }

}
