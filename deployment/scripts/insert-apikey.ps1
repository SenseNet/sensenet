Param(
    [Parameter(Mandatory=$False)]
	[string]$RepoUrl="localhost:51016",
    [Parameter(Mandatory=$False)]
	[string]$AuthUrl="localhost:51017",
    [Parameter(Mandatory=$False)]
	[string]$DataSource="sensenet-insql-cdb-sndb",
    [Parameter(Mandatory=$False)]
	[string]$DataContainer="sensenet-insql-cdb-snsql",
	[Parameter(Mandatory=$False)]
	[switch]$StepByStep    
)

if (-not (Get-Command "Invoke-Cli" -DryRun $DryRun -ErrorAction SilentlyContinue)) {
	Write-Output "load helper functions"
	. "$($PSScriptRoot)/helper-functions.ps1"
}

Test-Docker

$user="sa"
$password="SuP3rS3CuR3P4sSw0Rd"

$clientId = "pr3Gen3R4Ted"
$clientSecret = "pr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Ted"
$apiKey = "pr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Tedpr3Gen3R4Ted"

# check if client exists
$query = "SELECT * FROM [AccessTokens] WHERE [Value] = '$apiKey' AND [UserId] = 1 FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER"
$params = "exec", $DataContainer, "/opt/mssql-tools/bin/sqlcmd", "-d", $DataSource, "-U", $user, "-P", $password, "-Q", $query
$result = Invoke-Cli -execFile "docker" -params $params -ErrorAction SilentlyContinue
if ($result -ne "" -and $result -ne $null -and $result.Count -gt 1 -and $result[2] -ne $null) {
    $obj = $result[2] | ConvertFrom-Json
    if ($obj -ne $null) {
        # Write-Host "ApiKey $($obj.Value) already exists."
        $apiKeyExists = $true
    }
}


# check if client exists
$query = "SELECT * FROM [ClientApps] WHERE [ClientId] = '$($clientId)' AND [Authority] = '$authUrl' FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER"
$params = "exec", $DataContainer, "/opt/mssql-tools/bin/sqlcmd", "-d", $DataSource, "-U", $user, "-P", $password, "-Q", $query
$result = Invoke-Cli -execFile "docker" -params $params -ErrorAction SilentlyContinue
if ($result -ne "" -and $result -ne $null -and $result.Count -gt 1 -and $result[2] -ne $null) {
    $obj = $result[2] | ConvertFrom-Json
    if ($obj -ne $null) {
        # Write-Host "Client with ClientId $($obj.ClientId) already exists."
        $clientExists = $true
    }
}

if ($apiKeyExists -and $clientExists) {
    Write-Host "Both Client with ClientId and ApiKey already exists."
    return
}

# default values

$charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
$rndmId = -join (1..16 | ForEach-Object { Get-Random -InputObject $charSet.ToCharArray() })
$creationDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fffffff"

# Check if apikey exists and insert or update accordingly
$clientappScript = @"
IF NOT EXISTS (SELECT 1 FROM [AccessTokens] WHERE [Value] = '$apiKey' AND [UserId] = 1)
BEGIN
    INSERT INTO [AccessTokens]
         ([UserId], [Value], [Feature], [CreationDate], [ExpirationDate])
    VALUES
        ('1', '$apiKey', 'apikey', '$creationDate', '9999-12-31 23:59:59.9999999')
END
"@

$params = "exec", $DataContainer, "/opt/mssql-tools/bin/sqlcmd", "-d", $DataSource, "-U", $user, "-P", $password, "-Q", $clientappScript
Invoke-Cli -execFile "docker" -params $params -ErrorAction Stop

# Check if clientapp exists and insert or update accordingly
$clientappScript = @"
IF EXISTS (SELECT 1 FROM [ClientApps] WHERE [ClientId] = '$clientId')
BEGIN
    UPDATE [ClientApps]
    SET [Name] = '$clientId', [Repository] = '$repoUrl', [UserName] = 'builtin\admin', [Authority] = '$authUrl', [Type] = 4
    WHERE [ClientId] = '$clientId'
END
ELSE
BEGIN
    INSERT INTO [ClientApps]
        ([ClientId], [Name], [Repository], [UserName], [Authority], [Type])
    VALUES
        ('$clientId', '$clientId', '$repoUrl', 'builtin\admin', '$authUrl', 4)
END
"@

$params = "exec", $DataContainer, "/opt/mssql-tools/bin/sqlcmd", "-d", $DataSource, "-U", $user, "-P", $password, "-Q", $clientappScript
Invoke-Cli -execFile "docker" -params $params -ErrorAction Stop

# Check if client secret exists and insert or update accordingly
$clientsecretScript = @"
IF EXISTS (SELECT 1 FROM [ClientSecrets] WHERE [ClientId] = '$clientId')
BEGIN
    UPDATE [ClientSecrets]
    SET [Value] = '$clientSecret', [CreationDate] = '$creationDate', [ValidTill] = '9999-12-31 23:59:59.9999999'
    WHERE [ClientId] = '$clientId'
END
ELSE
BEGIN
    INSERT INTO [ClientSecrets]
        ([Id], [ClientId], [Value], [CreationDate], [ValidTill])
    VALUES
        ('$rndmId', '$clientId', '$clientSecret', '$creationDate', '9999-12-31 23:59:59.9999999')
END
"@

$params = "exec", $DataContainer, "/opt/mssql-tools/bin/sqlcmd", "-d", $DataSource, "-U", $user, "-P", $password, "-Q", $clientsecretScript
Invoke-Cli -execFile "docker" -params $params -ErrorAction Stop


