[CmdletBinding()]
Param (
    [ValidateSet('InMem','InSql','InPostgres','Is','Auth','Search','All')][string]$ImageType='InSql',
    [ValidateSet('Standard','Preview','LocalAuth','PostgreSql')][string]$WebApp='Standard',
    [string]$ImageTag='',
    [bool]$LocalSn=$False,
    [string]$SensenetGitRepo='https://github.com/SenseNet/sensenet',
    [string]$SensenetGitBranch='develop',
    [string]$SensenetFolderPath='',
    [string]$SensenetDockerImage='',
    [string]$IdentityGitRepo='https://github.com/SenseNet/sn-identityserver',
    [string]$IdentityGitBranch='main',
    [string]$IdentityDockerImage='sensenetcsp/sn-identityserver:latest',
    [string]$AuthGitRepo='https://github.com/SenseNet/sn-auth',
    [string]$AuthGitBranch='develop',
    [string]$AuthDockerImage='sensenetcsp/sn-auth:preview',
    [bool]$SearchService=$False,
    [string]$SearchGitRepo='https://github.com/SenseNet/sn-search-lucene29',
    [string]$SearchGitBranch='master',
    [string]$SearchDockerImage='sensenetcsp/sn-searchservice:preview',
    [bool]$DryRun=$False
)
. "$PSScriptRoot/helper-functions.ps1"
Test-Docker
if ($ImageType -eq 'InPostgres') { $WebApp = 'PostgreSql' }
if (-not $SensenetFolderPath) {
    if ($LocalSn) { $SensenetFolderPath = "$PSScriptRoot/../.." }
    else { $SensenetFolderPath = "$PSScriptRoot/../temp/Sensenet" }
}
$creationList = @()
if ($ImageType -in 'InMem','InSql','InPostgres','All') {
    if ($WebApp -eq 'PostgreSql') { $SensenetGitBranch = 'postgres-provider' }
    if (-not $LocalSn) { Get-GitRepo -Url $SensenetGitRepo -TargetPath $SensenetFolderPath -BranchName $SensenetGitBranch -DryRun $DryRun -ErrorAction Stop }
    if ($ImageType -eq 'All' -and $SensenetDockerImage) { throw 'Select one ImageType when overriding SensenetDockerImage.' }
    $selections = @(Get-SensenetImage -SnType $ImageType -WebApp $WebApp -SearchService $SearchService -ImageTag $ImageTag)
    if ($ImageType -eq 'All') {
        $selections = @(
            (Get-SensenetImage -SnType InMem -ImageTag $ImageTag),
            (Get-SensenetImage -WebApp $WebApp -ImageTag $ImageTag)
        )
        if ($WebApp -in 'Standard','Preview') { $selections += Get-SensenetImage -WebApp $WebApp -SearchService $True -ImageTag $ImageTag }
    }
    foreach ($selection in $selections) {
        $image = $selection.Image
        if ($SensenetDockerImage) { $image = $SensenetDockerImage }
        $creationList += @{ SolutionPath=$SensenetFolderPath; DockerImage=$image; DockerFilePath="$SensenetFolderPath/src/WebApps/$($selection.Project)/Dockerfile" }
    }
}
$dependencies = @(
    @{ Type='Is'; Folder='Identity'; LocalFolder='sn-identityserver'; Repo=$IdentityGitRepo; Branch=$IdentityGitBranch; Image=$IdentityDockerImage; Dockerfile='SenseNet.IdentityServer4.Web/Dockerfile' },
    @{ Type='Auth'; Folder='Auth'; LocalFolder='sn-auth'; Repo=$AuthGitRepo; Branch=$AuthGitBranch; Image=$AuthDockerImage; Dockerfile='sn-auth/Dockerfile' },
    @{ Type='Search'; Folder='SearchService'; LocalFolder='sn-search-lucene29'; Repo=$SearchGitRepo; Branch=$SearchGitBranch; Image=$SearchDockerImage; Dockerfile='SenseNet.Search.Lucene29.Centralized.GrpcService/Dockerfile' }
)
foreach ($dependency in $dependencies) {
    if ($ImageType -ne $dependency.Type -and $ImageType -ne 'All') { continue }
    $folder = "$PSScriptRoot/../temp/$($dependency.Folder)"
    if ($LocalSn) { $folder = "$PSScriptRoot/../../../$($dependency.LocalFolder)" }
    else { Get-GitRepo -Url $dependency.Repo -TargetPath $folder -BranchName $dependency.Branch -DryRun $DryRun -ErrorAction Stop }
    $context = "$folder/src"
    # SNAuth copies src/sn-auth from the repository root; the other Dockerfiles copy from src.
    if ($dependency.Type -eq 'Auth') { $context = $folder }
    $creationList += @{ SolutionPath=$folder; BuildContext=$context; DockerImage=$dependency.Image; DockerFilePath="$folder/src/$($dependency.Dockerfile)" }
}
foreach ($item in $creationList) {
    if (-not $DryRun -and -not (Test-Path -LiteralPath $item.DockerFilePath)) {
        throw "Dockerfile missing: $($item.DockerFilePath). Select the branch containing this webapp, or build without -LocalSn."
    }
    New-DockerImage @item -DryRun $DryRun -ErrorAction Stop
}
