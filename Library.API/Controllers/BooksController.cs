using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository libraryRepository;
        private readonly ILogger<BooksController> logger;
        private readonly IUrlHelper urlHelper;

        public BooksController(ILibraryRepository libraryRepository,
            ILogger<BooksController> logger,
            IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
            this.logger = logger;
            this.libraryRepository = libraryRepository;
        }
        
        [HttpGet(Name="GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorFromRepo = libraryRepository.GetBooksForAuthor(authorId);

            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            booksForAuthor = booksForAuthor.Select(CreateLinksForBook);

            var wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);
            return Ok(CreateLinksForBooks(wrapper));
        }

        [HttpGet("{bookId}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid bookId)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, bookId);

            if (bookForAuthorFromRepo == null)
                return NotFound();

            var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(CreateLinksForBook(bookForAuthor));
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId,
            [FromBody]BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = Mapper.Map<Book>(book);

            libraryRepository.AddBookForAuthor(authorId, bookEntity);

            if (!libraryRepository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save.");

            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId, bookId = bookToReturn.Id},
                CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{bookId}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid bookId)
        {
            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)
                return NotFound();

            libraryRepository.DeleteBook(bookForAuthorFromRepo);

            if (!libraryRepository.Save())
                throw new Exception($"Deleting book {bookId} for author {authorId} failed on save.");

            logger.LogInformation(100, "Book {BookId} for author {AuthorId} was deleted.", bookId, authorId);

            return NoContent();
        }

        [HttpPut("{bookId}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody]BookForUpdateDto book)
        {
            if (book == null)
                return BadRequest();

            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            var bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = bookId;

                libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!libraryRepository.Save())
                    throw new Exception($"Upserting book {bookId} for author {authorId} failed on save.");

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new {authorId, bookId = bookToReturn.Id}, bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!libraryRepository.Save())
                throw new Exception($"Updating book {bookId} for author {authorId} failed on save.");

            return NoContent();
        }

        [HttpPatch("{bookId}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody]JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                    return new UnprocessableEntityObjectResult(ModelState);

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = bookId;
                
                libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if(!libraryRepository.Save())
                    throw new Exception($"Upserting book {bookId} for author {authorId} failed on save.");

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new {authorId, bookId}, bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch, ModelState);

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            // TODO: Add validation

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if(!libraryRepository.Save())
                throw new Exception($"Patching book {bookId} for author {authorId} failed on save.");

            return NoContent();
        }

        private BookDto CreateLinksForBook(BookDto book)
        {
            void AddLink(string action, string rel, string method)
            {
                book.Links.Add(new LinkDto(
                    urlHelper.Link(action, new { bookId = book.Id}), rel, method));
            }

            AddLink("GetBookForAuthor", "self", "GET");
            AddLink("DeleteBookForAuthor", "delete_book", "DELETE");
            AddLink("UpdateBookForAuthor", "update_book", "PUT");
            AddLink("PartiallyUpdateBookForAuthor", "partially_update_book", "PATCH");
            return book;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(new LinkDto(
                urlHelper.Link("GetBooksForAuthor", new { }), "self", "GET"));

            return booksWrapper;
        }
    }
}