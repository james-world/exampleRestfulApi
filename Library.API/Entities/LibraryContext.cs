using Microsoft.EntityFrameworkCore;

namespace Library.API.Entities
{
    // ReSharper disable once ClassNeverInstantiated.Global (Instantiated by DI)
    public sealed class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options)
            : base(options)
        {
            Database.Migrate();
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
    }
}