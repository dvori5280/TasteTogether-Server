using TasteTogether.Service;

namespace TasteTogether.Tests.Services;

public class PasswordHasherTests
{
    // ל-PasswordHasher אין תלויות, ולכן בודקים אותו ישירות בלי Mocks
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ValidPassword_ReturnsHashInExpectedFormatWithoutPlainPassword()
    {
        // Arrange
        const string password = "MySecret123!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert: הפורמט הוא שלושה חלקים מופרדים בנקודה (חזרות, מלח, גיבוב), והסיסמה עצמה לא מופיעה בו
        Assert.Equal(3, hash.Split('.').Length);
        Assert.DoesNotContain(password, hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string password = "MySecret123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.Hash("MySecret123!");

        // Act
        var result = _hasher.Verify("WrongPassword!", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ReturnsDifferentHashesThatBothVerify()
    {
        // Arrange
        const string password = "MySecret123!";

        // Act: בכל גיבוב נבחר מלח אקראי חדש
        var firstHash = _hasher.Hash(password);
        var secondHash = _hasher.Hash(password);

        // Assert: הגיבובים שונים, אבל שניהם מאמתים את אותה סיסמה
        Assert.NotEqual(firstHash, secondHash);
        Assert.True(_hasher.Verify(password, firstHash));
        Assert.True(_hasher.Verify(password, secondHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("1.2")]
    [InlineData("abc.def.ghi")]
    [InlineData("600000.!!!.!!!")]
    public void Verify_MalformedHash_ReturnsFalse(string malformedHash)
    {
        // Act
        var result = _hasher.Verify("MySecret123!", malformedHash);

        // Assert: גיבוב פגום לא קורס, אלא פשוט נדחה
        Assert.False(result);
    }
}