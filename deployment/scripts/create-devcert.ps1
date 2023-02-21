dotnet dev-certs https -ep "$($PSScriptRoot)/../certificates/aspnetapp.pfx" -p QWEasd123%
dotnet dev-certs https --trust

# cleanup
# dotnet dev-certs https --clean

# https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide
