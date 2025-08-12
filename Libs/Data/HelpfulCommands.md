# EF Core
- `dotnet ef migrations add <Migration Title> -c <Context File Name> -o <Migration Output Location> --project Libs --startup-project API`
- `dotnet ef database update -c <Context File Name> --project Libs --startup-project API`