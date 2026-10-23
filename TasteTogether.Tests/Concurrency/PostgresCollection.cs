namespace TasteTogether.Tests.Concurrency;

// כל מחלקת בדיקות שמסומנת [Collection("Postgres")] משתמשת באותה Fixture, ורצה אחת אחרי השנייה
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
}