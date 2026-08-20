using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LibrarySystem.Api.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider
                .GetAllSchemesAsync();

        if (!authenticationSchemes.Any(
            scheme =>
                scheme.Name == "Bearer"))
        {
            return;
        }

        var securitySchemes =
            new Dictionary<
                string,
                IOpenApiSecurityScheme>
            {
                ["Bearer"] =
                    new OpenApiSecurityScheme
                    {
                        Type =
                            SecuritySchemeType.Http,

                        Scheme =
                            "bearer",

                        In =
                            ParameterLocation.Header,

                        BearerFormat =
                            "JWT"
                    }
            };

        document.Components ??=
            new OpenApiComponents();

        document.Components
            .SecuritySchemes =
                securitySchemes;

        foreach (
            var pathItem
            in document.Paths.Values)
        {
            var operations =
                pathItem?.Operations;

            if (operations is null)
            {
                continue;
            }

            foreach (
                var operation
                in operations.Values)
            {
                if (operation is null)
                {
                    continue;
                }

                operation.Security ??=
                    [];

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document)
                        ] = []
                    });
            }
        }
    }
}