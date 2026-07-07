# SetupUnityYAMLMerge.ps1
# 現在のUnityEditorのバージョンを確認して、自動でマージツールと紐づけるコマンド
Write-Host ""
Write-Host "========================================="
Write-Host " UnityYAMLMerge Setup"
Write-Host "========================================="
Write-Host ""

#--------------------------------------------------
# Git Repository確認
#--------------------------------------------------
try {
    git rev-parse --show-toplevel *> $null
}
catch {
    Write-Host "Error: Git Repositoryではありません。"
    exit 1
}

#--------------------------------------------------
# Unity Hub
#--------------------------------------------------
$unityHub = "C:\Program Files\Unity\Hub\Editor"

if (!(Test-Path $unityHub)) {
    Write-Host "Unity Hubが見つかりません。"
    exit 1
}

#--------------------------------------------------
# ProjectVersion取得
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
else {
    Write-Host "ProjectVersion.txt が見つかりません。"
}

#--------------------------------------------------
# UnityYAMLMerge検索
#--------------------------------------------------

$mergeExe = $null
$selectedVersion = $null

# ① ProjectVersionを優先
if ($projectVersion) {

    $candidate = Join-Path `
        $unityHub `
        "$projectVersion\Editor\Data\Tools\UnityYAMLMerge.exe"

    if (Test-Path $candidate) {

        $mergeExe = Get-Item $candidate
        $selectedVersion = $projectVersion

        Write-Host "Projectに一致するUnityを使用します。"
    }
    else {

        Write-Host "一致するUnityが見つかりません。"
        Write-Host "インストール済みUnityから検索します..."
    }
}

# ② 見つからなかったら従来方式
if ($null -eq $mergeExe) {

    $mergeExe = Get-ChildItem `
        -Path $unityHub `
        -Filter UnityYAMLMerge.exe `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($null -eq $mergeExe) {

        Write-Host "UnityYAMLMerge.exe が見つかりません。"
        exit 1
    }

    # Editorフォルダ名からVersion取得
    $selectedVersion = Split-Path (
        Split-Path (
            Split-Path (
                Split-Path $mergeExe.FullName -Parent
            ) -Parent
        ) -Parent
    ) -Leaf

    Write-Host "最新版(Unity検索結果)を使用します。"
}

#--------------------------------------------------
# Git設定
#--------------------------------------------------

$escapedPath = $mergeExe.FullName.Replace("\","\\")

$cmd = "`"$escapedPath`" merge -p `"`$BASE`" `"`$REMOTE`" `"`$LOCAL`" `"`$MERGED`""

git config --local merge.tool unityyamlmerge
git config --local mergetool.unityyamlmerge.trustExitCode false
git config --local mergetool.unityyamlmerge.cmd $cmd

#--------------------------------------------------
# 完了
#--------------------------------------------------

Write-Host ""
Write-Host "========================================="
Write-Host " 設定完了"
Write-Host "========================================="
Write-Host ""

Write-Host "Unity Version"
Write-Host "  $selectedVersion"
Write-Host ""

Write-Host "UnityYAMLMerge"
Write-Host "  $($mergeExe.FullName)"
Write-Host ""

Write-Host "Git設定"

Write-Host "  merge.tool"
git config --local --get merge.tool

Write-Host ""

Write-Host "  mergetool.unityyamlmerge.cmd"
git config --local --get mergetool.unityyamlmerge.cmd

Write-Host ""
Write-Host "Setup completed successfully."