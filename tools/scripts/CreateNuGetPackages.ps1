nuget pack ..\..\src\Common\Common.csproj -properties Configuration=Release
nuget pack ..\..\src\BlobStorage\BlobStorage.csproj -properties Configuration=Release
nuget pack ..\..\src\Preview\Preview.csproj -properties Configuration=Release
nuget pack ..\..\src\Services\Services.nuspec -properties Configuration=Release
nuget pack ..\..\src\Services\Services.Install.nuspec -properties Configuration=Release
