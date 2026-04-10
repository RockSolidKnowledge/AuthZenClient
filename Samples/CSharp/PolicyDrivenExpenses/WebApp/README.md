# WebApp

This app uses ASP.NET Core Identity with an in-memory EF Core store and includes a Razor Pages sign-in flow.

## Seeded users

- `alice` / `Passw0rd!`
- `bob` / `Passw0rd!`

## Run

```zsh
cd /Users/andyclymer/git/AuthZenExample/WebApp
dotnet run
```

Then open the printed local URL and go to `/Account/Login`.

## Notes

- User data is in-memory only; restarting the app resets users and sessions.
- `/secure` requires authentication.

