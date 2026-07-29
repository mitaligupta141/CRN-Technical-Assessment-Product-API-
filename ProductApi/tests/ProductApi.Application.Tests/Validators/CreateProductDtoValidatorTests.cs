using FluentValidation.TestHelper;
using ProductApi.Application.DTOs;
using ProductApi.Application.Validators;
using Xunit;

namespace ProductApi.Application.Tests.Validators;

public class CreateProductDtoValidatorTests
{
    private readonly CreateProductDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ProductName_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateProductDto { ProductName = "" });
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void Should_Have_Error_When_ProductName_Exceeds_MaxLength()
    {
        var result = _validator.TestValidate(new CreateProductDto { ProductName = new string('a', 256) });
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_ProductName()
    {
        var result = _validator.TestValidate(new CreateProductDto { ProductName = "Valid Product" });
        result.ShouldNotHaveValidationErrorFor(x => x.ProductName);
    }
}
