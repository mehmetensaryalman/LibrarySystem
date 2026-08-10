using System.Security.Claims;
using LibrarySystem.Application.Interfaces.Borrow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class BorrowController : ControllerBase
{
    private readonly IBorrowService _borrowService;

    public BorrowController(IBorrowService borrowService)
    {
        _borrowService = borrowService;
    }

    [HttpPost("borrow/{bookId:int}")]
    public async Task<IActionResult> Borrow(int bookId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _borrowService.BorrowAsync(
            userId,
            bookId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("return/{bookId:int}")]
    public async Task<IActionResult> Return(int bookId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _borrowService.ReturnAsync(
            userId,
            bookId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("borrow/my-books")]
    public async Task<IActionResult> GetMyBooks()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var books =
            await _borrowService.GetMyBooksAsync(userId);

        return Ok(books);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }
}