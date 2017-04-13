using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
        }

        public IActionResult GetAuthors()
        {
            var authorsFromRepo = libraryRepository.GetAuthors();
            return new JsonResult(authorsFromRepo);
        }
    }
}