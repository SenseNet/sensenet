﻿$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installPackagePath = "$srcPath\nuget\content\Admin\tools\install-services.zip"
$scriptsSourcePath = "$srcPath\ContentRepository.MsSql\Scripts"

# delete existing packages
Remove-Item $PSScriptRoot\*.nupkg

# nuget pack $srcPath\Services\SenseNet.Services.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
# nuget pack $srcPath\Tests\SenseNet.Tests\SenseNet.Tests.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
# nuget pack $srcPath\Tests\SenseNet.Tests.Hosting\SenseNet.Tests.Hosting.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot

New-Item $srcPath\nuget\snadmin\install-services\scripts -ItemType directory -Force

Copy-Item $scriptsSourcePath\Create_SenseNet_Database.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\Create_SenseNet_Azure_Database.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\MsSqlInstall_Security.sql $srcPath\nuget\snadmin\install-services\scripts -Force
Copy-Item $scriptsSourcePath\MsSqlInstall_Schema.sql $srcPath\nuget\snadmin\install-services\scripts -Force

Compress-Archive -Path "$srcPath\nuget\snadmin\install-services\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath

# nuget pack $srcPath\Services\SenseNet.Services.Install.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot

# assemble legacy packages
# nuget pack $srcPath\Scripting.JScript\SenseNet.Scripting.JScript.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
# nuget pack $srcPath\TextExtractors.Pdf\SenseNet.TextExtractors.Pdf.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
