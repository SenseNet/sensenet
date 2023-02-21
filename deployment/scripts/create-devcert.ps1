dotnet dev-certs https -ep "$($PSScriptRoot)/../certificates/aspnetapp.pfx" -p QWEasd123%
dotnet dev-certs https --trust

# cleanup
# dotnet dev-certs https --clean