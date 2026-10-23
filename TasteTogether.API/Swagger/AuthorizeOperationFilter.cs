using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TasteTogether.API.Swagger;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // מצרפים את דרישת ה-Bearer לכל endpoint, גם לפתוחים.
        // כך Swagger שולח את הטוקן בכל בקשה אחרי Authorize.
        // ההרשאה האמיתית נקבעת בשרת לפי [Authorize] ו-[AllowAnonymous], לא לפי המסנן הזה
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            }
        };
    }
}