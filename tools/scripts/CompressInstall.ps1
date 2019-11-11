$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installSourceSnAdminPath = "$srcPath\nuget\snadmin"
$installPackagePath = "$srcPath\Services.Core.Install\install-services-core.zip"

New-Item $installSourceSnAdminPath\install-services-core\import -ItemType directory -Force

Copy-Item "$installSourceSnAdminPath\install-services\import\*" $installSourceSnAdminPath\install-services-core\import -recurse -Force

Compress-Archive -Path "$srcPath\nuget\snadmin\install-services-core\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath