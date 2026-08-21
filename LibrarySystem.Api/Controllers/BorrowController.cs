using System.Security.Claims;
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.DTOs.Borrow;
using LibrarySystem.Application.Interfaces.Borrow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class BorrowController :
    ControllerBase
{
    private readonly
        IBorrowService _borrowService;

    public BorrowController(
        IBorrowService borrowService)
    {
        _borrowService =
            borrowService;
    }

    [HttpPost("borrow/{bookId:int}")]
    [Authorize(
        Policy = "BorrowerOnly")]
    public async Task<IActionResult>
        Borrow(
            int bookId)
    {
        var userId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var result =
            await _borrowService
                .BorrowAsync(
                    userId,
                    bookId);

        if (!result.Success)
        {
            return BadRequest(
                result);
        }

        return Ok(
            result);
    }

    [HttpPost(
        "borrow/{bookId:int}/return-request")]
    [Authorize(
        Policy = "BorrowerOnly")]
    public async Task<IActionResult>
        RequestReturn(
            int bookId)
    {
        var userId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var result =
            await _borrowService
                .RequestReturnAsync(
                    userId,
                    bookId);

        if (!result.Success)
        {
            return BadRequest(
                result);
        }

        return Ok(
            result);
    }

    [HttpGet("borrow/my-books")]
    [Authorize(
        Policy = "BorrowerOnly")]
    public async Task<IActionResult>
        GetMyBooks()
    {
        var userId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var books =
            await _borrowService
                .GetMyBooksAsync(
                    userId);

        return Ok(
            books);
    }

    [HttpGet(
        "borrow/my-penalty-status")]
    [Authorize(
        Policy = "BorrowerOnly")]
    public async Task<IActionResult>
        GetMyPenaltyStatus()
    {
        var userId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var penaltyStatus =
            await _borrowService
                .GetMyPenaltyStatusAsync(
                    userId);

        return Ok(
            penaltyStatus);
    }

    [HttpGet("admin/borrows")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        GetAllBorrowsForAdmin()
    {
        var borrows =
            await _borrowService
                .GetAllBorrowsForAdminAsync();

        return Ok(
            borrows);
    }

    [HttpGet(
        "admin/borrow-requests")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        GetPendingBorrowRequestsForAdmin()
    {
        var requests =
            await _borrowService
                .GetPendingBorrowRequestsForAdminAsync();

        return Ok(
            requests);
    }

    [HttpPut(
        "admin/borrow-requests/{borrowRequestId:int}/approve")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        ApproveBorrowRequest(
            int borrowRequestId)
    {
        var adminUserId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                adminUserId))
        {
            return Unauthorized();
        }

        var result =
            await _borrowService
                .ApproveBorrowRequestAsync(
                    borrowRequestId,
                    adminUserId);

        if (!result.Success)
        {
            return BadRequest(
                result);
        }

        return Ok(
            result);
    }

    [HttpPut(
        "admin/borrow-requests/{borrowRequestId:int}/reject")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        RejectBorrowRequest(
            int borrowRequestId,
            [FromBody]
            RejectBorrowRequestRequestDto?
                request)
    {
        var adminUserId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                adminUserId))
        {
            return Unauthorized();
        }

        var result =
            await _borrowService
                .RejectBorrowRequestAsync(
                    borrowRequestId,
                    adminUserId,
                    request?.Reason);

        if (!result.Success)
        {
            return BadRequest(
                result);
        }

        return Ok(
            result);
    }

    [HttpPost(
        "admin/borrows/{borrowRecordId:int}/return")]
    [Authorize(
        Roles = RoleNames.Admin)]
    public async Task<IActionResult>
        ReturnForAdmin(
            int borrowRecordId)
    {
        var adminUserId =
            GetUserId();

        if (
            string.IsNullOrWhiteSpace(
                adminUserId))
        {
            return Unauthorized();
        }

        var result =
            await _borrowService
                .ReturnForAdminAsync(
                    borrowRecordId,
                    adminUserId);

        if (!result.Success)
        {
            return BadRequest(
                result);
        }

        return Ok(
            result);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }
}