using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Common.Exceptions;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.DTOs.Books;
using LibrarySystem.Application.Interfaces.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly
        IBookService _bookService;

    public BooksController(
        IBookService bookService)
    {
        _bookService =
            bookService;
    }

    [HttpGet]
    public async Task<IActionResult>
        GetAll()
    {
        var books =
            await _bookService
                .GetAllAsync();

        return Ok(books);
    }

    [HttpGet("paged")]
    public async Task<IActionResult>
        GetPaged(
            [FromQuery]
            BookFilterRequestDto request)
    {
        var result =
            await _bookService
                .GetPagedAsync(request);

        return Ok(result);
    }

    [HttpGet("archived")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        GetArchived()
    {
        var books =
            await _bookService
                .GetArchivedAsync();

        return Ok(books);
    }

    [HttpPost]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        Create(
            CreateBookRequestDto request)
    {
        try
        {
            var book =
                await _bookService
                    .CreateAsync(request);

            return Ok(book);
        }
        catch (
            DuplicateBookException exception)
        {
            return Conflict(new
            {
                message =
                    exception.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        Update(
            int id,
            UpdateBookRequestDto request)
    {
        try
        {
            var updatedBook =
                await _bookService
                    .UpdateAsync(
                        id,
                        request);

            if (updatedBook is null)
            {
                return NotFound(new
                {
                    message =
                        "Kitap bulunamadı."
                });
            }

            return Ok(updatedBook);
        }
        catch (
            DuplicateBookException exception)
        {
            return Conflict(new
            {
                message =
                    exception.Message
            });
        }
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        Restore(
            int id)
    {
        var book =
            await _bookService
                .RestoreAsync(id);

        if (book is null)
        {
            return NotFound(new
            {
                message =
                    "Arşivlenmiş kitap bulunamadı."
            });
        }

        return Ok(new
        {
            message =
                "Kitap başarıyla arşivden geri alındı.",

            book
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        Delete(
            int id)
    {
        var result =
            await _bookService
                .DeleteAsync(id);

        return result.Status switch
        {
            DeleteBookStatus.NotFound =>
                NotFound(new
                {
                    message =
                        result.Message
                }),

            DeleteBookStatus
                .ActiveBorrowExists =>
                Conflict(new
                {
                    message =
                        result.Message
                }),

            DeleteBookStatus.Archived =>
                Ok(new
                {
                    message =
                        result.Message
                }),

            DeleteBookStatus.Deleted =>
                Ok(new
                {
                    message =
                        result.Message
                }),

            _ =>
                StatusCode(
                    StatusCodes
                        .Status500InternalServerError)
        };
    }
}