$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '/../..'))
$installSourceSnAdminPath = [System.IO.Path]::GetFullPath("$srcPath/nuget/snadmin")
$installPackagePath = [System.IO.Path]::GetFullPath("$srcPath/Services.Core.Install/install-services-core.zip")
$source = "$installSourceSnAdminPath/install-services/import"
$destination = "$installSourceSnAdminPath/install-services-core/import"

# Create the destination folder for the import structure.

New-Item "$installSourceSnAdminPath/install-services-core/import" -ItemType directory -Force

# Copy import structure from its original place excluding applications.
Write-Host "Copying common source items from $source"

$exclude = @("(apps).Content", "WebRoot.Content", "ErrorMessages.Content")
$excludeMatch = @("(apps)", "WebRoot", "ErrorMessages")
[regex] $excludeMatchRegEx = ‘(?i)‘ + (($excludeMatch |foreach {[regex]::escape($_)}) –join “|”) + ‘’
Get-ChildItem -Path $source -Recurse -Exclude $exclude | 
 where { $excludeMatch -eq $null -or $_.FullName.Replace($source, "") -notmatch $excludeMatchRegEx} |
 Copy-Item -Destination {
  if ($_.PSIsContainer) {
   Write-Host $_.Parent.FullName
   Join-Path $destination $_.Parent.FullName.Substring($source.length)
  } else {
   Write-Host $_.FullName
   Join-Path $destination $_.FullName.Substring($source.length)
  }
 } -Force -Exclude $exclude

 # Copy import items from the netcore-only folder. We cannot store files in source 
 # control directly in the install-services-core folder, because it would confuse
 # git what to commit and what to ignore.
 Write-Host "Copying netcore source items..."

 Copy-Item "$installSourceSnAdminPath/install-services/importNetCore/**" $destination -Container -Recurse -Force

# Create the install package.
Write-Host "Compressing package..."

Compress-Archive -Path "$installSourceSnAdminPath/install-services-core/*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath