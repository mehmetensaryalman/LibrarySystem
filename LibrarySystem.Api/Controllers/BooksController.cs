using Microsoft.AspNetCore.Authorization;
using LibrarySystem.Application.DTOs.Books;
using LibrarySystem.Application.Interfaces.Books;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookService.GetAllAsync();

        return Ok(books);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBookRequestDto request)
    {
        var book = await _bookService.CreateAsync(request);

        return Ok(book);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _bookService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Kitap bulunamadı."
            });
        }

        return Ok(new
        {
            message = "Kitap başarıyla silindi."
        });
    }
}