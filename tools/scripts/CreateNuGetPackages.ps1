$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installPackagePath = "$srcPath\nuget\content\Admin\tools\install-services.zip"
$scriptsSourcePath = "$srcPath\Storage\Data\SqlClient\Scripts"

# delete existing packages
Remove-Item $PSScriptRoot\*.nupkg

nuget pack $srcPath\Common\Common.csproj -properties Configuration=Release -OutputDirectory $PSScriptRoot
nuget pack $srcPath\BlobStorage\BlobStorage.csproj -properties Configuration=Release -OutputDirectory $PSScriptRoot
nuget pack $srcPath\Services\Services.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot

New-Item $srcPath\nuget\snadmin\install-services\scripts -ItemType directory -Force

Copy-Item $scriptsSourcePath\Create_SenseNet_Database.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Create_SenseNet_Azure_Database.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Install_Security.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Install_01_Schema.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Install_02_Procs.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Install_03_Data_Phase1.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Install_04_Data_Phase2.sql $srcPath\nuget\snadmin\install-services\scripts -Force

Compress-Archive -Path "$srcPath\nuget\snadmin\install-services\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath

nuget pack $srcPath\Services\Services.Install.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
