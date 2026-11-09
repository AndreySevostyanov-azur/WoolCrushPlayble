# kb-update-reminder.ps1
# Stop hook: if gameplay/playable code changed this session, emit a soft, non-blocking,
# targeted reminder to verify/update the matching KB doc. Escalates if no .claude/kb/ doc was
# touched this session. Always exits 0 (never blocks).

$ErrorActionPreference = 'SilentlyContinue'

try {
    $root = $env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($root)) {
        $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }

    Push-Location $root
    try {
        $changed = & git status --porcelain --untracked-files=all 2>$null
    } finally {
        Pop-Location
    }

    if ([string]::IsNullOrWhiteSpace($changed)) { exit 0 }

    $lines = $changed -split "`n"
    $codeChanges = $lines | Where-Object { $_ -match 'Assets/(Scripts|!Project/Scripts|Playable/Scripts|Editor|PlayablesPlugins)/|Assets/Playable/Scenes/' }
    if (-not $codeChanges) { exit 0 }

    $kbTouched = [bool]($lines | Where-Object { $_ -match '\.claude/kb/' })

    $areas = @{}
    foreach ($l in $codeChanges) {
        if ($l -match 'Assets/Scripts/' -or $l -match 'Assets/!Project/Scripts/') {
            $areas['gameplay'] = '.claude/kb/02-gameplay-ecs.md'
        }
        if ($l -match 'Assets/Playable/Scripts/|Assets/Playable/Scenes/|Assets/Editor/|Assets/PlayablesPlugins/') {
            $areas['luna/scenes/editor'] = '.claude/kb/03-luna-and-scenes.md'
        }
    }

    $hints = foreach ($a in ($areas.Keys | Sort-Object)) { "$a -> $($areas[$a])" }
    $hintText = $hints -join '; '

    if ($kbTouched) {
        Write-Output "[KB reminder] Code/scene changes ($hintText). A .claude/kb/ doc was edited this session - confirm it is the RIGHT doc(s), that UPDATELOG.md was appended, and bump 'Last verified:' for any re-confirmed section."
    } else {
        Write-Output "[KB reminder] Code/scene changes but NO .claude/kb/ doc was touched this session. Areas: $hintText. If architecture / system order / components / scene wiring / paths changed, update the matching doc + append .claude/kb/UPDATELOG.md before declaring done. Pure bug-fix with no structural change: fine to skip - state so explicitly."
    }
}
catch {
    # never block on hook failure
}

exit 0
