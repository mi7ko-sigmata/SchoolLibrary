# Система за управление на училищна библиотека (MVP)

ASP.NET Core 10 MVC + Entity Framework Core + SQLite (настроено за .NET 10.0).

## Стартиране във Visual Studio
1. Разархивирайте папката и отворете `SchoolLibrary.sln` във **Visual Studio**.
2. Натиснете **F5** (или Ctrl+F5). При първото стартиране NuGet сваля пакетите, а базата `library.db` се създава и пълни с примерни данни автоматично.

Или от терминал: `cd SchoolLibrary && dotnet run`.

## Примерни акаунти
| Роля | Потребител | Парола |
|------|-----------|--------|
| Библиотекар | `bibliotekar` | `Parola123!` |
| Ученик | `ivan` | `Parola123!` |
| Ученик | `elena` | `Parola123!` |
