using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace PricingAnalytics.Api.OpenApi;

/// <summary>
/// Declares the bearer token scheme in the generated OpenAPI document. Without it the
/// documentation UI offers no place to paste a token, so every protected endpoint would
/// answer 401 and could not be tried out.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Token dobiven pozivom POST /auth/login. Zalijepite samo token, bez prefiksa \"Bearer\".",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marks every endpoint that carries [Authorize] as requiring the bearer token, and notes
/// in its description when an administrator role is needed.
/// </summary>
internal sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authorizeData = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .ToList();

        if (authorizeData.Count == 0)
        {
            return Task.CompletedTask;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = BearerSecuritySchemeTransformer.SchemeName,
                    },
                }] = Array.Empty<string>(),
            },
        ];

        var roles = authorizeData
            .Select(data => data.Roles)
            .FirstOrDefault(roles => !string.IsNullOrWhiteSpace(roles));

        var note = roles is null
            ? "Zahtijeva prijavu."
            : $"Zahtijeva prijavu i ulogu: {roles}.";

        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? note
            : $"{operation.Description}\n\n{note}";

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Nedostaje ili je istekao token." });
        if (roles is not null)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Prijavljeni korisnik nema potrebnu ulogu." });
        }

        return Task.CompletedTask;
    }
}
