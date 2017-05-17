nuget pack ..\..\src\Common\Common.csproj -properties Configuration=Release
nuget pack ..\..\src\BlobStorage\BlobStorage.csproj -properties Configuration=Release
nuget pack ..\..\src\Preview\Preview.csproj -properties Configuration=Release
nuget pack ..\..\src\Services\Services.nuspec -properties Configuration=Release

New-Item ..\..\src\nuget\snadmin\install-services\scripts -ItemType directory -Force

Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Create_SenseNet_Database.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Create_SenseNet_Azure_Database.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Install_Security.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Install_01_Schema.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Install_02_Procs.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Install_03_Data_Phase1.sql ..\..\src\nuget\snadmin\install-services\scripts -Force
Copy-Item ..\..\src\Storage\Data\SqlClient\Scripts\Install_04_Data_Phase2.sql ..\..\src\nuget\snadmin\install-services\scripts -Force

Compress-Archive -Path "..\..\src\nuget\snadmin\install-services\*" -Force -CompressionLevel Optimal -DestinationPath "..\..\src\nuget\content\Admin\tools\install-services.zip"
nuget pack ..\..\src\Services\Services.Install.nuspec -properties Configuration=Release
