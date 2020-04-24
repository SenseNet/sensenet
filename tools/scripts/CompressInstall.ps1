$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installSourceSnAdminPath = "$srcPath\nuget\snadmin"
$installPackagePath = "$srcPath\Services.Core.Install\install-services-core.zip"
$source = "$installSourceSnAdminPath\install-services\import"
$destination = "$installSourceSnAdminPath\install-services-core\import"

# Create the destination folder for the import structure.

New-Item $installSourceSnAdminPath\install-services-core\import -ItemType directory -Force

# Copy import structure from its original place excluding applications.

$exclude = @("(apps).Content", "WebRoot.Content", "ErrorMessages.Content")
$excludeMatch = @("(apps)", "WebRoot", "ErrorMessages")
[regex] $excludeMatchRegEx = ‘(?i)‘ + (($excludeMatch |foreach {[regex]::escape($_)}) –join “|”) + ‘’
Get-ChildItem -Path $source -Recurse -Exclude $exclude | 
 where { $excludeMatch -eq $null -or $_.FullName.Replace($source, "") -notmatch $excludeMatchRegEx} |
 Copy-Item -Destination {
  if ($_.PSIsContainer) {
   Join-Path $destination $_.Parent.FullName.Substring($source.length)
  } else {
   Join-Path $destination $_.FullName.Substring($source.length)
  }
 } -Force -Exclude $exclude

 # Copy import items from the netcore-only folder. We cannot store files in source 
 # control directly in the install-services-core folder, because it would confuse
 # git what to commit and what to ignore.

 Copy-Item $installSourceSnAdminPath\install-services\importNetCore\** $destination -Container -Recurse -Force

# Create the install package.
Compress-Archive -Path "$installSourceSnAdminPath\install-services-core\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePath