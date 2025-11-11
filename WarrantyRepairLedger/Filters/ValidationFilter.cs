using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WarrantyRepairLedger.Filters;

/// <summary>
/// Validates request DTOs using data annotations before invoking the endpoint.
/// </summary>
public class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        var isValid = Validator.TryValidateObject(request, validationContext, results, validateAllProperties: true);

        if (!isValid)
        {
            var errors = results
                .Where(r => r != ValidationResult.Success)
                .SelectMany(r =>
                {
                    var memberNames = r.MemberNames?.Any() == true
                        ? r.MemberNames
                        : new[] { string.Empty };

                    return memberNames.Select(name => new
                    {
                        Key = name,
                        r?.ErrorMessage
                    });
                })
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage ?? "Invalid input.").ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}
