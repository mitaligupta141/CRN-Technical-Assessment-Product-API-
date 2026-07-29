using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductApi.Application.DTOs;

namespace ProductApi.API.Filters;

/// <summary>
/// Short-circuits the pipeline with a consistent 400 response when model binding /
/// data-annotation validation fails, before the request ever reaches a controller action.
/// </summary>
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        context.Result = new BadRequestObjectResult(new ApiErrorResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = "One or more validation failures occurred.",
            Errors = errors,
            TraceId = context.HttpContext.TraceIdentifier
        });
    }
}
