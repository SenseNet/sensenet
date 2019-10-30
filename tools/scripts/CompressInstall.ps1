$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installSourceSnAdminPath = "$srcPath\nuget\snadmin"
$installPackagePath = "$srcPath\SenseNet.Services.InstallData\install-services-core.zip"
$scriptsSourcePath = "$srcPath\Storage\Data\MsSqlClient\Scripts"

New-Item $srcPath\nuget\snadmin\install-services-core\scripts -ItemType directory -Force
New-Item $installSourceSnAdminPath\install-services-core\import -ItemType directory -Force

Copy-Item "$installSourceSnAdminPath\install-services\import\*" $installSourceSnAdminPath\install-services-core\import -recurse -Force

Copy-Item $scriptsSourcePath\Create_SenseNet_Database.sql $srcPath\nuget\snadmin\install-services-core\scripts -Force
Copy-Item $scriptsSourcePath\Create_SenseNet_Azure_Database.sql $srcPath\nuget\snadmin\install-services-core\scripts -Force
Copy-Item $scriptsSourcePath\MsSqlInstall_Security.sql $srcPath\nuget\snadmin\install-services-core\scripts -Force
Copy-Item $scriptsSourcePath\MsSqlInstall_Schema.sql $srcPath\nuget\snadmin\install-services-core\scripts -Force

Compress-Archive -Path "$srcPath\nuget\snadmin\install-services-core\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath