[CmdletBinding()]
param([switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$versionProps = Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw -Encoding UTF8
$version = [string]$versionProps.Project.PropertyGroup.Version
$releasePath = Join-Path $projectRoot 'artifacts/release'
# Publish into a fresh directory to exclude local settings and unrelated files.
$publishPath = Join-Path $projectRoot "artifacts/publish/$([Guid]::NewGuid().ToString('N'))"

if (-not $SkipTests) {
    & dotnet test (Join-Path $projectRoot 'NugetUploadHelper.slnx') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed. Publishing stopped.' }
}

& dotnet publish (Join-Path $projectRoot 'src/NugetUploadHelper/NugetUploadHelper.csproj') `
    -c Release -p:PublishProfile=Portable -o $publishPath
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }

New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
$baseName = "NugetUploadHelper-v$version-win-x64"
$executable = Join-Path $releasePath "$baseName.exe"
$archive = Join-Path $releasePath "$baseName.zip"
Copy-Item -LiteralPath (Join-Path $publishPath 'NugetUploadHelper.exe') -Destination $executable
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/usage.md') -Destination (Join-Path $publishPath 'NugetUploadHelper-Guide.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Destination $publishPath
# Package only the application and its documentation.
$packageFiles = @('NugetUploadHelper.exe', 'NugetUploadHelper-Guide.md', 'CHANGELOG.md') |
    ForEach-Object { Join-Path $publishPath $_ }
Compress-Archive -LiteralPath $packageFiles -DestinationPath $archive -Force
$checksums = @($executable, $archive) | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))"
}
$checksums | Set-Content -LiteralPath (Join-Path $releasePath "$baseName-SHA256SUMS.txt") -Encoding ascii
Write-Output "Executable: $executable"
Write-Output "Archive: $archive"
