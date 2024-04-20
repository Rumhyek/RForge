using Microsoft.AspNetCore.Mvc;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;

namespace RForgeDocs.EndPoints
{
    public static class UserEndpoints
    {
        public static void MapUserApi(this WebApplication app)
        {
            app.MapGet("/api/users/{*userId}",
                async ([FromServices] IGetUserProcessor processor, int userId) =>
                {
                    return Results.Ok(await processor.GetUser(userId));
                });

            app.MapPut("/api/users",
                async ([FromServices] ISaveUserProcessor processor, [FromBody] UserAddSaveData user) =>
                {
                    return Results.Ok(await processor.AddUser(user));
                });

            app.MapPost("/api/users",
                async ([FromServices] ISaveUserProcessor processor, [FromBody] UserSaveData user) =>
                {
                    return Results.Ok(await processor.SaveUser(user));
                });
        }

    }
}
