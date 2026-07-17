[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [string] $SharePath = '\\10.100.100.4\Website\App_IT\Remote',

    [string] $Repository = 'Oncology-Hospital/Remote',

    [ValidateRange(1, 60)]
    [int] $ReleaseTimeoutMinutes = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string] $Command,

        [Parameter(ValueFromRemainingArguments)]
        [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $Command $($Arguments -join ' ')"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Required command 'git' was not found."
}

$repoRoot = (git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
    throw 'Run this script from inside the RemoteDesktop Git repository.'
}

Push-Location $repoRoot
$downloadDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "remote-release-$Version-$([guid]::NewGuid().ToString('N'))"

try {
    $status = git status --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not read Git working tree status.'
    }

    if ($status) {
        throw 'The working tree is not clean. Commit the release changes before publishing.'
    }

    [xml] $buildProperties = Get-Content -LiteralPath '.\Directory.Build.props' -Raw
    $projectVersion = [string] $buildProperties.Project.PropertyGroup.Version
    if ($projectVersion -ne $Version) {
        throw "Directory.Build.props has version '$projectVersion', but -Version is '$Version'."
    }

    if (-not $SharePath.StartsWith('\\')) {
        throw "SharePath must be a UNC path. Received: $SharePath"
    }

    if (-not (Test-Path -LiteralPath $SharePath -PathType Container)) {
        throw "The release share is unavailable: $SharePath"
    }

    $tag = "v$Version"
    $headCommit = (git rev-parse HEAD).Trim()
    $existingTagCommit = git rev-list -n 1 $tag 2>$null

    if ($existingTagCommit) {
        if ($existingTagCommit.Trim() -ne $headCommit) {
            throw "Tag $tag already points to another commit."
        }
    }
    else {
        Invoke-Checked git tag -a $tag -m "Release $Version"
    }

    Write-Host 'Pushing the current commit and release tag to GitHub...'
    Invoke-Checked git push origin HEAD
    Invoke-Checked git push origin $tag

    Write-Host 'Waiting for GitHub Actions to finish the release...'
    $deadline = [DateTimeOffset]::Now.AddMinutes($ReleaseTimeoutMinutes)
    $releaseReady = $false
    $releaseAssets = @()
    $apiHeaders = @{
        Accept = 'application/vnd.github+json'
        'User-Agent' = 'OncologyHospital-Remote-Release-Script'
        'X-GitHub-Api-Version' = '2022-11-28'
    }
    $releaseApiUrl = "https://api.github.com/repos/$Repository/releases/tags/$tag"

    while ([DateTimeOffset]::Now -lt $deadline) {
        try {
            $release = Invoke-RestMethod -Uri $releaseApiUrl -Headers $apiHeaders
            $releaseAssets = @($release.assets)
            $assetNames = @($releaseAssets | ForEach-Object name)
            $hasInstaller = $assetNames -contains 'CNTT_Remote.exe'
            $hasFeed = $assetNames -contains 'releases.win.json'
            $hasPackage = @($assetNames | Where-Object { $_ -like '*.nupkg' }).Count -gt 0
            if ($hasInstaller -and $hasFeed -and $hasPackage) {
                $releaseReady = $true
                break
            }
        }
        catch {
            # A 404 is expected while the GitHub Actions release job is running.
        }

        Start-Sleep -Seconds 15
    }

    if (-not $releaseReady) {
        throw "GitHub release $tag was not ready after $ReleaseTimeoutMinutes minute(s)."
    }

    New-Item -ItemType Directory -Path $downloadDirectory | Out-Null
    $assetsToMirror = @($releaseAssets | Where-Object {
        $_.name -eq 'CNTT_Remote.exe'
        -or $_.name -eq 'releases.win.json'
        -or $_.name -like '*.nupkg'
    })
    foreach ($asset in $assetsToMirror) {
        $destination = Join-Path $downloadDirectory $asset.name
        Invoke-WebRequest -Uri $asset.browser_download_url -Headers $apiHeaders -OutFile $destination
    }

    $releaseFiles = @(Get-ChildItem -LiteralPath $downloadDirectory -File)
    if (-not ($releaseFiles.Name -contains 'releases.win.json')) {
        throw 'The downloaded release does not contain releases.win.json.'
    }

    Write-Host "Mirroring release files to $SharePath..."
    $feedFile = $releaseFiles | Where-Object Name -eq 'releases.win.json'
    $otherFiles = $releaseFiles | Where-Object Name -ne 'releases.win.json'

    # Publish packages first and the feed index last, so clients never see an
    # index that references a package which has not finished copying yet.
    foreach ($file in @($otherFiles) + @($feedFile)) {
        $destination = Join-Path $SharePath $file.Name
        if (Test-Path -LiteralPath $destination) {
            Remove-Item -LiteralPath $destination -Force
        }

        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }

    Write-Host "Release $tag is available on GitHub and the LAN share."
}
finally {
    Pop-Location

    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $resolvedDownloadDirectory = [System.IO.Path]::GetFullPath($downloadDirectory)
    $isInTempRoot = $resolvedDownloadDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)
    $hasExpectedName = ([System.IO.Path]::GetFileName($resolvedDownloadDirectory)).StartsWith('remote-release-')
    if ((Test-Path -LiteralPath $resolvedDownloadDirectory) -and $isInTempRoot -and $hasExpectedName) {
        Remove-Item -LiteralPath $resolvedDownloadDirectory -Recurse -Force
    }
}
