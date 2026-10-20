#requires -Version 7.2
[CmdletBinding()]
Param (
    [string]$RepositoryUrl='https://127.0.0.1:18016',
    [string]$AdminUiOrigin='https://admin.example.invalid',
    [switch]$ExpectRepositoryRejection
)
$ErrorActionPreference='Stop'
$RepositoryUrl=$RepositoryUrl.TrimEnd('/')
$AdminUiOrigin=([uri]$AdminUiOrigin).GetLeftPart([UriPartial]::Authority)
$handler=[Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect=$False
$handler.ServerCertificateCustomValidationCallback=[Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$http=[Net.Http.HttpClient]::new($handler)
$http.Timeout=[TimeSpan]::FromSeconds(20)
$null=$http.DefaultRequestHeaders.TryAddWithoutValidation('Origin',$AdminUiOrigin)
function Assert-Origin($response, [string]$endpoint) {
    $allowed=@($response.Headers.GetValues('Access-Control-Allow-Origin'))
    if ($AdminUiOrigin -notin $allowed -and '*' -notin $allowed) { throw "Origin not allowed at $endpoint." }
}
try {
    # Use the same GET bootstrap and Origin header as the Admin UI.
    $bootstrap=$http.GetAsync("$RepositoryUrl/odata.svc/('Root')/GetClientRequestParameters?clientType=adminui").GetAwaiter().GetResult()
    $null=$bootstrap.EnsureSuccessStatusCode()
    Assert-Origin $bootstrap 'repository client discovery'
    $client=$bootstrap.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if (-not $client.client_id -or -not $client.authority) { throw 'Missing Admin UI client metadata.' }
    $authority=$client.authority.TrimEnd('/')
    $discovery=$http.GetAsync("$authority/.well-known/openid-configuration").GetAwaiter().GetResult()
    $null=$discovery.EnsureSuccessStatusCode()
    Assert-Origin $discovery 'IdentityServer discovery'
    $metadata=$discovery.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    $keys=$http.GetAsync($metadata.jwks_uri).GetAwaiter().GetResult()
    $null=$keys.EnsureSuccessStatusCode()
    Assert-Origin $keys 'IdentityServer signing keys'
    $redirect="$AdminUiOrigin/authentication/callback"
    $verifier=[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_')
    $challenge=[Convert]::ToBase64String([Security.Cryptography.SHA256]::HashData([Text.Encoding]::ASCII.GetBytes($verifier))).TrimEnd('=').Replace('+','-').Replace('/','_')
    $query=@{client_id=$client.client_id;redirect_uri=$redirect;response_type='code';scope='openid profile sensenet';snrepo=$RepositoryUrl;code_challenge=$challenge;code_challenge_method='S256';state=[guid]::NewGuid().ToString('N');nonce=[guid]::NewGuid().ToString('N')}
    $encoded=($query.GetEnumerator() | ForEach-Object { [Uri]::EscapeDataString($_.Key)+'='+[Uri]::EscapeDataString($_.Value) }) -join '&'
    $authorize=$http.GetAsync("$authority/connect/authorize?$encoded").GetAwaiter().GetResult()
    $next=$authorize.Headers.Location.OriginalString
    if ($next -notmatch '/Account/Login') { throw 'Authorization did not reach the login page.' }
    $page=$http.GetStringAsync(([uri]::new([uri]$authority,$next)).AbsoluteUri).GetAwaiter().GetResult()
    $form=[Collections.Generic.Dictionary[string,string]]::new()
    foreach ($tag in [regex]::Matches($page,'<input\b[^>]*>')) {
        $name=[regex]::Match($tag.Value,'\bname="([^"]*)"').Groups[1].Value
        if ($name) { $form[$name]=[Net.WebUtility]::HtmlDecode([regex]::Match($tag.Value,'\bvalue="([^"]*)"').Groups[1].Value) }
    }
    $form['Username']='admin';$form['Password']='admin';$form['button']='login'
    $login=$http.PostAsync("$authority/Account/Login",[Net.Http.FormUrlEncodedContent]::new($form)).GetAwaiter().GetResult()
    if ($ExpectRepositoryRejection) {
        $body=$login.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ([int]$login.StatusCode -ne 200 -or $body -notmatch 'Invalid repository\.') { throw 'Expected repository-host rejection was not reproduced.' }
        Write-Host "PASS: reproduced the rejected Admin UI login for $RepositoryUrl."
        return
    }
    if (-not $login.Headers.Location) { throw "Admin UI login did not redirect (HTTP $([int]$login.StatusCode)); check IdentityServer logs for the repository URL." }
    $next=$login.Headers.Location.OriginalString
    if ($next -notmatch '^/connect/authorize/callback') { throw 'Unexpected authorization callback.' }
    $callback=$http.GetAsync("$authority$next").GetAwaiter().GetResult()
    $destination=$callback.Headers.Location.OriginalString
    $code=[regex]::Match($destination,'[?&]code=([^&]+)')
    if (-not $code.Success) { throw 'Authorization callback returned no code.' }
    # Do not follow the external redirect or expose tokens/cookies/codes.
    $exchange=[Collections.Generic.Dictionary[string,string]]::new()
    $exchange['grant_type']='authorization_code';$exchange['client_id']=$client.client_id
    $exchange['redirect_uri']=$redirect;$exchange['code']=[Uri]::UnescapeDataString($code.Groups[1].Value);$exchange['code_verifier']=$verifier
    $tokenResponse=$http.PostAsync($metadata.token_endpoint,[Net.Http.FormUrlEncodedContent]::new($exchange)).GetAwaiter().GetResult()
    $null=$tokenResponse.EnsureSuccessStatusCode()
    Assert-Origin $tokenResponse 'IdentityServer token exchange'
    $token=$tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if (-not $token.access_token) { throw 'No access token returned.' }
    $http.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$token.access_token)
    $adminResponse=$http.GetAsync("$RepositoryUrl/odata.svc/Root/IMS/BuiltIn/Portal('Admin')?`$select=Id,Name").GetAwaiter().GetResult()
    $null=$adminResponse.EnsureSuccessStatusCode()
    Assert-Origin $adminResponse 'authenticated repository read'
    $admin=$adminResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($admin.d.Id -ne 1) { throw 'Authenticated Admin read failed.' }
    Write-Host "PASS: $RepositoryUrl Admin UI login, Origin=$AdminUiOrigin, PKCE token exchange and authenticated Admin(Id=1) read."
} finally { $http.Dispose();$handler.Dispose() }
