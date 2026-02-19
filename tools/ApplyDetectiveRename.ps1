<#
.SYNOPSIS
  Apply project-wide rename: replace 'CarToll' → 'DetectiveGame' (namespaces, class/type identifiers and file names).

.DESCRIPTION
  - Performs ordered replacements (namespaces/usings first, then identifier-wide).
  - Optionally makes a backup of changed files into '.rename-backup'.
  - Supports a dry-run using -WhatIf to preview changes without writing files or renaming.
  - Renames files whose filenames contain 'CarToll' to use 'DetectiveGame'.

.PARAMETER Root
  Root folder to scan. Defaults to current directory.

.PARAMETER Backup
  Create a backup copy of modified files under '.rename-backup\<timestamp>\'.

.PARAMETER WhatIf
  Dry-run: report planned changes but do not write files or rename.

.EXAMPLE
  .\ApplyDetectiveRename.ps1 -Root "C:\Detective Game Repository\Detective Game" -Backup
#>

param(
    [string]$Root = ".",
    [switch]$Backup,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$rootFull = (Resolve-Path -Path $Root).Path
Write-Host "Scanning: $rootFull"

# Ordered replacement rules (apply longer explicit replacements first)
$rules = @(
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\.UI'; Replacement = 'namespace Eduzo.Games.DetectiveGame.UI' ; Regex = $false },
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\.Data'; Replacement = 'namespace Eduzo.Games.DetectiveGame.Data' ; Regex = $false },
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\b'; Replacement = 'namespace Eduzo.Games.DetectiveGame' ; Regex = $true },
    @{ Pattern = 'using\s+Eduzo\.Games\.CarToll\.UI'; Replacement = 'using Eduzo.Games.DetectiveGame.UI' ; Regex = $false },
    @{ Pattern = 'using\s+Eduzo\.Games\.CarToll\.Data'; Replacement = 'using Eduzo.Games.DetectiveGame.Data' ; Regex = $false },
    # Generic identifier replacement using word boundary to avoid partial matches
    @{ Pattern = '\bCarToll\b'; Replacement = 'DetectiveGame' ; Regex = $true }
)

# Prepare backup folder
$backupFolder = ""
if ($Backup -and -not $WhatIf) {
    $ts = (Get-Date).ToString("yyyyMMdd_HHmmss")
    $backupFolder = Join-Path $rootFull ".rename-backup\$ts"
    New-Item -ItemType Directory -Force -Path $backupFolder | Out-Null
    Write-Host "Backup folder: $backupFolder"
}

$csFiles = Get-ChildItem -Path $rootFull -Recurse -Include *.cs -File

$modified = @()
$renamed = @()

foreach ($file in $csFiles) {
    try {
        $orig = Get-Content -Raw -LiteralPath $file.FullName -ErrorAction Stop
    } catch {
        Write-Warning "Unable to read $($file.FullName): $_"
        continue
    }

    $updated = $orig
    foreach ($r in $rules) {
        if ($r.Regex) {
            $updated = [System.Text.RegularExpressions.Regex]::Replace($updated, $r.Pattern, $r.Replacement)
        } else {
            $updated = $updated -replace [regex]::Escape($r.Pattern), $r.Replacement
        }
    }

    if ($updated -ne $orig) {
        $relative = $file.FullName.Substring($rootFull.Length).TrimStart('\','/')
        $modified += $relative

        Write-Host "MODIFY: $relative"

        if ($WhatIf) {
            continue
        }

        # Backup single file
        if ($Backup) {
            $dest = Join-Path $backupFolder $relative
            $destDir = Split-Path -Path $dest -Parent
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
            Copy-Item -LiteralPath $file.FullName -Destination $dest -Force
        }

        # Write updated content (preserve original encoding by writing as UTF8 without BOM)
        $updated | Set-Content -LiteralPath $file.FullName -Encoding utf8
    }

    # File rename: if filename contains 'CarToll' replace with 'DetectiveGame'
    if ($file.Name -match 'CarToll') {
        $newName = $file.Name -replace 'CarToll','DetectiveGame'
        $newFull = Join-Path $file.DirectoryName $newName
        $relOld = $file.FullName.Substring($rootFull.Length).TrimStart('\','/')
        $relNew = $newFull.Substring($rootFull.Length).TrimStart('\','/')

        Write-Host "RENAME: $relOld -> $relNew"
        if ($WhatIf) {
            $renamed += "$relOld -> $relNew"
        } else {
            # Backup file before rename if requested
            if ($Backup) {
                $destRenameBackup = Join-Path $backupFolder $relOld
                $destDir = Split-Path -Path $destRenameBackup -Parent
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
                Copy-Item -LiteralPath $file.FullName -Destination $destRenameBackup -Force
            }

            # Only rename if destination doesn't already exist
            if (-not (Test-Path $newFull)) {
                Rename-Item -LiteralPath $file.FullName -NewName $newName
                $renamed += "$relOld -> $relNew"
            } else {
                Write-Warning "Cannot rename $relOld: destination $relNew already exists."
            }
        }
    }
}

Write-Host "Done."
Write-Host ""
Write-Host "Summary:"
Write-Host "  Files modified: $($modified.Count)"
foreach ($m in $modified) { Write-Host "    $m" }
Write-Host "  Files renamed: $($renamed.Count)"
foreach ($r in $renamed) { Write-Host "    $r" }

if ($WhatIf) {
    Write-Host ""
    Write-Host "This was a dry-run (WhatIf). No files were changed. Rerun without -WhatIf to apply."
} elseif ($Backup) {
    Write-Host ""
    Write-Host "Backups saved to: $backupFolder"
}           <#
.SYNOPSIS
  Apply project-wide rename: replace 'CarToll' → 'DetectiveGame' (namespaces, class/type identifiers and file names).

.DESCRIPTION
  - Performs ordered replacements (namespaces/usings first, then identifier-wide).
  - Optionally makes a backup of changed files into '.rename-backup'.
  - Supports a dry-run using -WhatIf to preview changes without writing files or renaming.
  - Renames files whose filenames contain 'CarToll' to use 'DetectiveGame'.

.PARAMETER Root
  Root folder to scan. Defaults to current directory.

.PARAMETER Backup
  Create a backup copy of modified files under '.rename-backup\<timestamp>\'.

.PARAMETER WhatIf
  Dry-run: report planned changes but do not write files or rename.

.EXAMPLE
  .\ApplyDetectiveRename.ps1 -Root "C:\Detective Game Repository\Detective Game" -Backup
#>

param(
    [string]$Root = ".",
    [switch]$Backup,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$rootFull = (Resolve-Path -Path $Root).Path
Write-Host "Scanning: $rootFull"

# Ordered replacement rules (apply longer explicit replacements first)
$rules = @(
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\.UI'; Replacement = 'namespace Eduzo.Games.DetectiveGame.UI' ; Regex = $false },
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\.Data'; Replacement = 'namespace Eduzo.Games.DetectiveGame.Data' ; Regex = $false },
    @{ Pattern = 'namespace\s+Eduzo\.Games\.CarToll\b'; Replacement = 'namespace Eduzo.Games.DetectiveGame' ; Regex = $true },
    @{ Pattern = 'using\s+Eduzo\.Games\.CarToll\.UI'; Replacement = 'using Eduzo.Games.DetectiveGame.UI' ; Regex = $false },
    @{ Pattern = 'using\s+Eduzo\.Games\.CarToll\.Data'; Replacement = 'using Eduzo.Games.DetectiveGame.Data' ; Regex = $false },
    # Generic identifier replacement using word boundary to avoid partial matches
    @{ Pattern = '\bCarToll\b'; Replacement = 'DetectiveGame' ; Regex = $true }
)

# Prepare backup folder
$backupFolder = ""
if ($Backup -and -not $WhatIf) {
    $ts = (Get-Date).ToString("yyyyMMdd_HHmmss")
    $backupFolder = Join-Path $rootFull ".rename-backup\$ts"
    New-Item -ItemType Directory -Force -Path $backupFolder | Out-Null
    Write-Host "Backup folder: $backupFolder"
}

$csFiles = Get-ChildItem -Path $rootFull -Recurse -Include *.cs -File

$modified = @()
$renamed = @()

foreach ($file in $csFiles) {
    try {
        $orig = Get-Content -Raw -LiteralPath $file.FullName -ErrorAction Stop
    } catch {
        Write-Warning "Unable to read $($file.FullName): $_"
        continue
    }

    $updated = $orig
    foreach ($r in $rules) {
        if ($r.Regex) {
            $updated = [System.Text.RegularExpressions.Regex]::Replace($updated, $r.Pattern, $r.Replacement)
        } else {
            $updated = $updated -replace [regex]::Escape($r.Pattern), $r.Replacement
        }
    }

    if ($updated -ne $orig) {
        $relative = $file.FullName.Substring($rootFull.Length).TrimStart('\','/')
        $modified += $relative

        Write-Host "MODIFY: $relative"

        if ($WhatIf) {
            continue
        }

        # Backup single file
        if ($Backup) {
            $dest = Join-Path $backupFolder $relative
            $destDir = Split-Path -Path $dest -Parent
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
            Copy-Item -LiteralPath $file.FullName -Destination $dest -Force
        }

        # Write updated content (preserve original encoding by writing as UTF8 without BOM)
        $updated | Set-Content -LiteralPath $file.FullName -Encoding utf8
    }

    # File rename: if filename contains 'CarToll' replace with 'DetectiveGame'
    if ($file.Name -match 'CarToll') {
        $newName = $file.Name -replace 'CarToll','DetectiveGame'
        $newFull = Join-Path $file.DirectoryName $newName
        $relOld = $file.FullName.Substring($rootFull.Length).TrimStart('\','/')
        $relNew = $newFull.Substring($rootFull.Length).TrimStart('\','/')

        Write-Host "RENAME: $relOld -> $relNew"
        if ($WhatIf) {
            $renamed += "$relOld -> $relNew"
        } else {
            # Backup file before rename if requested
            if ($Backup) {
                $destRenameBackup = Join-Path $backupFolder $relOld
                $destDir = Split-Path -Path $destRenameBackup -Parent
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
                Copy-Item -LiteralPath $file.FullName -Destination $destRenameBackup -Force
            }

            # Only rename if destination doesn't already exist
            if (-not (Test-Path $newFull)) {
                Rename-Item -LiteralPath $file.FullName -NewName $newName
                $renamed += "$relOld -> $relNew"
            } else {
                Write-Warning "Cannot rename $relOld: destination $relNew already exists."
            }
        }
    }
}

Write-Host "Done."
Write-Host ""
Write-Host "Summary:"
Write-Host "  Files modified: $($modified.Count)"
foreach ($m in $modified) { Write-Host "    $m" }
Write-Host "  Files renamed: $($renamed.Count)"
foreach ($r in $renamed) { Write-Host "    $r" }

if ($WhatIf) {
    Write-Host ""
    Write-Host "This was a dry-run (WhatIf). No files were changed. Rerun without -WhatIf to apply."
} elseif ($Backup) {
    Write-Host ""
    Write-Host "Backups saved to: $backupFolder"
}   