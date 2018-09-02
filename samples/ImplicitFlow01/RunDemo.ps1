$root = $PSScriptRoot;
. $root\..\Shared.ps1

Push-Location "$root/AppWithFetchClient"
dotnet restore
dotnet build --no-incremental
Start-Process dotnet -ArgumentList "watch run" -PassThru
Pop-Location

Start-Browser "http://localhost:5000"
