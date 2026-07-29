using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductApi.API.Filters;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;

namespace ProductApi.API.Controllers.V1;

/// <summary>
/// Full CRUD operations for inventory Items tied to a Product.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/items")]
[ValidateModel]
[Produces("application/json")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    /// <summary>Gets a paginated list of items.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ItemDto>>> GetAll([FromQuery] PaginationQuery query, CancellationToken ct)
    {
        var result = await _itemService.GetAllAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Gets a single item by id.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _itemService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Creates a new inventory item for a product.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> Create([FromBody] CreateItemDto dto, CancellationToken ct)
    {
        var result = await _itemService.CreateAsync(dto, CurrentUser, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    /// <summary>Updates the quantity of an existing item.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> Update(int id, [FromBody] UpdateItemDto dto, CancellationToken ct)
    {
        var result = await _itemService.UpdateAsync(id, dto, CurrentUser, ct);
        return Ok(result);
    }

    /// <summary>Deletes an item.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _itemService.DeleteAsync(id, ct);
        return NoContent();
    }

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "system";
}
