[CmdletBinding()]
Param (
    [string]$ProjectName="sensenet-inpostgres-cdb",
    [string]$NetworkName="sensenet",
    [string]$VolumeBasePath="./volumes",
    [string]$SqlUser="postgres",
    [Parameter(Mandatory=$True)][string]$SqlPsw,
    [bool]$UseVolume=$False,
    [bool]$DryRun=$False
)
. "$PSScriptRoot/helper-functions.ps1"
Test-Docker
$container = "$ProjectName-snsql"
$database = "$ProjectName-sndb"
$params = "run", "-d", "--net", $NetworkName, "--name", $container,
    "-e", "POSTGRES_USER=$SqlUser", "-e", "POSTGRES_PASSWORD=$SqlPsw", "-e", "POSTGRES_DB=$database",
    "-p", "127.0.0.1:5532:5432"
if ($UseVolume) { $params += "-v", "$VolumeBasePath/$ProjectName-snapp/postgres:/var/lib/postgresql/data" }
$params += "postgres:16"
$null = Manage-Container -ContainerName $container -DockerRunParams $params -DryRun $DryRun
if ($DryRun) { return }
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    docker exec $container pg_isready -U $SqlUser -d $database | Out-Null
    if ($LASTEXITCODE -eq 0) { return }
    Start-Sleep -Seconds 2
}
throw "PostgreSQL container $container did not become ready."
