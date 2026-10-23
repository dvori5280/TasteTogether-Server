using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TasteTogether.API.Controllers;

namespace TasteTogether.Tests.Api;

public class ControllerAuthorizationTests
{
    private const string Anonymous = "Anonymous";
    private const string Authenticated = "Authenticated";
    private const string Admin = "Admin";
    private const string Unprotected = "Unprotected";

    // מחזירה את רמת הגישה של Action: פתוח לכולן, למחוברות בלבד, לתפקיד מסוים, או לא מוגן בכלל.
    // [AllowAnonymous] על ה-Action גובר, ואחרת [Authorize] על ה-Action, ואם אין אז [Authorize] על ה-Controller
    private static string GetAccessLevel(Type controllerType, MethodInfo method)
    {
        if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null)
            return Anonymous;

        var authorize = method.GetCustomAttribute<AuthorizeAttribute>()
            ?? controllerType.GetCustomAttribute<AuthorizeAttribute>();

        if (authorize == null)
            return Unprotected;

        return string.IsNullOrEmpty(authorize.Roles) ? Authenticated : authorize.Roles;
    }

    [Theory]
    // AuthController
    [InlineData(typeof(AuthController), "LoginAsync", Anonymous)]
    [InlineData(typeof(AuthController), "RegisterAsync", Anonymous)]
    // UsersController
    [InlineData(typeof(UsersController), "GetAllAsync", Admin)]
    [InlineData(typeof(UsersController), "GetByIdAsync", Admin)]
    // RecipesController
    [InlineData(typeof(RecipesController), "GetAllAsync", Anonymous)]
    [InlineData(typeof(RecipesController), "GetByIdAsync", Anonymous)]
    [InlineData(typeof(RecipesController), "CreateAsync", Admin)]
    [InlineData(typeof(RecipesController), "UpdateAsync", Admin)]
    [InlineData(typeof(RecipesController), "DeleteAsync", Admin)]
    // WorkshopsController
    [InlineData(typeof(WorkshopsController), "GetAllAsync", Anonymous)]
    [InlineData(typeof(WorkshopsController), "GetByIdAsync", Anonymous)]
    [InlineData(typeof(WorkshopsController), "GetRecipesAsync", Anonymous)]
    [InlineData(typeof(WorkshopsController), "CreateAsync", Admin)]
    [InlineData(typeof(WorkshopsController), "UpdateStatusAsync", Admin)]
    // RecipeWorkshopLinksController
    [InlineData(typeof(RecipeWorkshopLinksController), "LinkRecipeAsync", Admin)]
    [InlineData(typeof(RecipeWorkshopLinksController), "UnlinkRecipeAsync", Admin)]
    // RegistrationsController
    [InlineData(typeof(RegistrationsController), "GetByWorkshopAsync", Admin)]
    [InlineData(typeof(RegistrationsController), "RegisterAsync", Authenticated)]
    [InlineData(typeof(RegistrationsController), "CancelAsync", Authenticated)]
    // MyRegistrationsController
    [InlineData(typeof(MyRegistrationsController), "GetMineAsync", Authenticated)]
    public void Endpoint_HasExpectedAccessLevel(Type controllerType, string actionName, string expectedAccess)
    {
        var method = controllerType.GetMethod(actionName);

        Assert.NotNull(method);
        Assert.Equal(expectedAccess, GetAccessLevel(controllerType, method));
    }

    [Fact]
    public void AllControllerActions_HaveExplicitAccessLevel()
    {
        // עוברת על כל ה-Actions בכל ה-Controllers של ה-API, כולל כאלה שיתווספו בעתיד,
        // ומוודאת שאף אחד מהם לא נשאר בלי [Authorize] או [AllowAnonymous]
        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        var unprotected = new List<string>();

        foreach (var controller in controllers)
        {
            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

            foreach (var action in actions)
            {
                if (GetAccessLevel(controller, action) == Unprotected)
                    unprotected.Add($"{controller.Name}.{action.Name}");
            }
        }

        Assert.Empty(unprotected);
    }
}