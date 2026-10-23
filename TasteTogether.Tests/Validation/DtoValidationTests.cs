using System.ComponentModel.DataAnnotations;
using TasteTogether.Core.DTOs;

namespace TasteTogether.Tests.Validation;

public class DtoValidationTests
{
    // מריצה את ה-Data Annotations של ה-DTO, כמו ש-ASP.NET עושה בבקשה אמיתית, ומחזירה את השגיאות
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    // האם יש שגיאה שקשורה לשדה מסוים
    private static bool HasErrorFor(List<ValidationResult> results, string propertyName)
    {
        return results.Any(r => r.MemberNames.Contains(propertyName));
    }

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        FullName = "Dana Cohen",
        Email = "dana@example.com",
        Password = "MySecret123!"
    };

    private static LoginRequest ValidLoginRequest() => new()
    {
        Email = "dana@example.com",
        Password = "MySecret123!"
    };

    private static UpdateRecipeRequest ValidUpdateRecipeRequest() => new()
    {
        Title = "Challah",
        Description = "Braided bread",
        IngredientsText = new List<string> { "Flour" },
        InstructionsText = new List<string> { "Knead the dough" }
    };

    private static CreateWorkshopRequest ValidCreateWorkshopRequest() => new()
    {
        Title = "Bread Workshop",
        Description = "Learn to bake",
        Location = "Jerusalem",
        StartsAt = DateTime.UtcNow.AddDays(7),
        Capacity = 10
    };

    // ---------- RegisterRequest ----------

    [Fact]
    public void RegisterRequest_AllFieldsValid_HasNoErrors()
    {
        Assert.Empty(Validate(ValidRegisterRequest()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void RegisterRequest_FullNameWithinLimits_HasNoError(int length)
    {
        var request = ValidRegisterRequest();
        request.FullName = new string('a', length);

        Assert.False(HasErrorFor(Validate(request), nameof(RegisterRequest.FullName)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void RegisterRequest_FullNameOutOfLimits_HasError(int length)
    {
        var request = ValidRegisterRequest();
        request.FullName = new string('a', length);

        Assert.True(HasErrorFor(Validate(request), nameof(RegisterRequest.FullName)));
    }

    [Fact]
    public void RegisterRequest_FullNameIsNull_HasError()
    {
        var request = ValidRegisterRequest();
        request.FullName = null!;

        Assert.True(HasErrorFor(Validate(request), nameof(RegisterRequest.FullName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("dana@")]
    public void RegisterRequest_InvalidEmail_HasError(string email)
    {
        var request = ValidRegisterRequest();
        request.Email = email;

        Assert.True(HasErrorFor(Validate(request), nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void RegisterRequest_EmailTooLong_HasError()
    {
        var request = ValidRegisterRequest();
        request.Email = new string('a', 200) + "@example.com";

        Assert.True(HasErrorFor(Validate(request), nameof(RegisterRequest.Email)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(100)]
    public void RegisterRequest_PasswordWithinLimits_HasNoError(int length)
    {
        var request = ValidRegisterRequest();
        request.Password = new string('a', length);

        Assert.False(HasErrorFor(Validate(request), nameof(RegisterRequest.Password)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(101)]
    public void RegisterRequest_PasswordOutOfLimits_HasError(int length)
    {
        var request = ValidRegisterRequest();
        request.Password = new string('a', length);

        Assert.True(HasErrorFor(Validate(request), nameof(RegisterRequest.Password)));
    }

    // ---------- LoginRequest ----------

    [Fact]
    public void LoginRequest_AllFieldsValid_HasNoErrors()
    {
        Assert.Empty(Validate(ValidLoginRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void LoginRequest_InvalidEmail_HasError(string email)
    {
        var request = ValidLoginRequest();
        request.Email = email;

        Assert.True(HasErrorFor(Validate(request), nameof(LoginRequest.Email)));
    }

    [Fact]
    public void LoginRequest_PasswordEmpty_HasError()
    {
        var request = ValidLoginRequest();
        request.Password = string.Empty;

        Assert.True(HasErrorFor(Validate(request), nameof(LoginRequest.Password)));
    }

    [Fact]
    public void LoginRequest_PasswordTooLong_HasError()
    {
        var request = ValidLoginRequest();
        request.Password = new string('a', 101);

        Assert.True(HasErrorFor(Validate(request), nameof(LoginRequest.Password)));
    }

    [Fact]
    public void LoginRequest_ShortPassword_HasNoError()
    {
        // אורך מינימלי נדרש רק בהרשמה. בהתחברות אין לגלות לאיזה אורך הסיסמה צריכה להיות
        var request = ValidLoginRequest();
        request.Password = "abc";

        Assert.False(HasErrorFor(Validate(request), nameof(LoginRequest.Password)));
    }

    // ---------- UpdateRecipeRequest ----------

    [Fact]
    public void UpdateRecipeRequest_AllFieldsValid_HasNoErrors()
    {
        Assert.Empty(Validate(ValidUpdateRecipeRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdateRecipeRequest_TitleMissing_HasError(string? title)
    {
        var request = ValidUpdateRecipeRequest();
        request.Title = title!;

        Assert.True(HasErrorFor(Validate(request), nameof(UpdateRecipeRequest.Title)));
    }

    [Fact]
    public void UpdateRecipeRequest_IngredientsEmpty_HasError()
    {
        var request = ValidUpdateRecipeRequest();
        request.IngredientsText = new List<string>();

        Assert.True(HasErrorFor(Validate(request), nameof(UpdateRecipeRequest.IngredientsText)));
    }

    [Fact]
    public void UpdateRecipeRequest_InstructionsEmpty_HasError()
    {
        var request = ValidUpdateRecipeRequest();
        request.InstructionsText = new List<string>();

        Assert.True(HasErrorFor(Validate(request), nameof(UpdateRecipeRequest.InstructionsText)));
    }

    [Fact]
    public void UpdateRecipeRequest_DescriptionEmpty_HasNoError()
    {
        // התיאור אינו שדה חובה
        var request = ValidUpdateRecipeRequest();
        request.Description = string.Empty;

        Assert.Empty(Validate(request));
    }

    // ---------- CreateWorkshopRequest ----------

    [Fact]
    public void CreateWorkshopRequest_AllFieldsValid_HasNoErrors()
    {
        Assert.Empty(Validate(ValidCreateWorkshopRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateWorkshopRequest_TitleMissing_HasError(string? title)
    {
        var request = ValidCreateWorkshopRequest();
        request.Title = title!;

        Assert.True(HasErrorFor(Validate(request), nameof(CreateWorkshopRequest.Title)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateWorkshopRequest_LocationMissing_HasError(string? location)
    {
        var request = ValidCreateWorkshopRequest();
        request.Location = location!;

        Assert.True(HasErrorFor(Validate(request), nameof(CreateWorkshopRequest.Location)));
    }

    [Fact]
    public void CreateWorkshopRequest_StartsAtMissing_HasError()
    {
        var request = ValidCreateWorkshopRequest();
        request.StartsAt = null;

        Assert.True(HasErrorFor(Validate(request), nameof(CreateWorkshopRequest.StartsAt)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateWorkshopRequest_CapacityBelowOne_HasError(int capacity)
    {
        var request = ValidCreateWorkshopRequest();
        request.Capacity = capacity;

        Assert.True(HasErrorFor(Validate(request), nameof(CreateWorkshopRequest.Capacity)));
    }

    [Fact]
    public void CreateWorkshopRequest_CapacityIsOne_HasNoError()
    {
        var request = ValidCreateWorkshopRequest();
        request.Capacity = 1;

        Assert.False(HasErrorFor(Validate(request), nameof(CreateWorkshopRequest.Capacity)));
    }

    [Fact]
    public void CreateWorkshopRequest_DescriptionEmpty_HasNoError()
    {
        // התיאור אינו שדה חובה
        var request = ValidCreateWorkshopRequest();
        request.Description = string.Empty;

        Assert.Empty(Validate(request));
    }
}