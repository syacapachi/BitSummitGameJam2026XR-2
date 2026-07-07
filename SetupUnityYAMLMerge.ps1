# SetupUnityYAMLMerge.ps1
# 現在のUnityEditorのバージョンを確認して、自動でマージツールと紐づけるコマンド

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================="
Write-Host " UnityYAMLMerge Setup"
Write-Host "========================================="
Write-Host ""

#--------------------------------------------------
# Git Repository確認
#--------------------------------------------------

git rev-parse --show-toplevel *> $null

#--------------------------------------------------
# Unity Hub
#--------------------------------------------------

$unityHub = "C:\Program Files\Unity\Hub\Editor"

if (!(Test-Path $unityHub)) {
    throw "Unity Hub が見つかりません。"
}

#--------------------------------------------------
# Project Version
#--------------------------------------------------

$projectVersion = $null

$versionFile = Join-Path (Get-Location) "ProjectSettings\ProjectVersion.txt"

if (Test-Path $versionFile) {

    $line = Get-Content $versionFile |
        Where-Object { $_ -match "^m_EditorVersion:" }

    if ($line) {
        $projectVersion = ($line -replace "^m_EditorVersion:\s*", "").Trim()
        Write-Host "Project Unity Version : $projectVersion"
    }
}

#--------------------------------------------------
# UnityYAMLMerge検索
#--------------------------------------------------

$mergeExe = $null
$selectedVersion = $null

if ($projectVersion) {

    $candidate = Join-Path $unityHub "$projectVersion\Editor\Data\Tools\UnityYAMLMerge.exe"

    if (Test-Path $candidate) {

        $mergeExe = Get-Item $candidate
        $selectedVersion = $projectVersion

        Write-Host "Using matching Unity version."
    }
}

if ($null -eq $mergeExe) {

    Write-Host "Matching Unity not found."
    Write-Host "Searching installed Unity versions..."

    $mergeExe = Get-ChildItem `
        $unityHub `
        -Filter UnityYAMLMerge.exe `
        -Recurse |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($null -eq $mergeExe) {
        throw "UnityYAMLMerge.exe が見つかりません。"
    }

    $selectedVersion = Split-Path (
        Split-Path (
            Split-Path (
                Split-Path $mergeExe.FullName -Parent
            ) -Parent
        ) -Parent
    ) -Leaf
}

#--------------------------------------------------
# Git設定
#--------------------------------------------------

$path = $mergeExe.FullName.Replace("\","/")

$cmd = "$path merge -p `"`$BASE`" `"`$REMOTE`" `"`$LOCAL`" `"`$MERGED`""

git config --local "merge.tool" "unityyamlmerge"
git config --local "mergetool.unityyamlmerge.trustExitCode" "false"
git config --local "mergetool.unityyamlmerge.cmd" $cmd

#--------------------------------------------------
# Result
#--------------------------------------------------

Write-Host ""
Write-Host "========================================="
Write-Host " Completed"
Write-Host "========================================="
Write-Host ""

Write-Host "Unity Version"
Write-Host "  $selectedVersion"
Write-Host ""

Write-Host "UnityYAMLMerge"
Write-Host "  $($mergeExe.FullName)"
Write-Host ""

Write-Host "Registered Command"
Write-Host "  $cmd"
Write-Host ""

Write-Host "Verify"

git config --local --get merge.tool
git config --local --get mergetool.unityyamlmerge.cmd

Write-Host ""
Write-Host "Setup completed successfully."