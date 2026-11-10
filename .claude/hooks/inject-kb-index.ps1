# inject-kb-index.ps1
# SessionStart (full) + UserPromptSubmit (brief): injects the KB protocol reminder into
# context. stdout from these hook events is added to the model context.
# Always exits 0 so it can never block the session.

param([ValidateSet('full','brief')][string]$Mode = 'full')

$ErrorActionPreference = 'SilentlyContinue'

try {
    $root = $env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($root)) {
        $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }
    $index = Join-Path $root '.claude\kb\INDEX.md'

    if ($Mode -eq 'brief') {
        Write-Output "[KB protocol] Before acting, consult .claude/kb/INDEX.md and the relevant KB doc for the area you are touching. After structural changes, update the KB doc + .claude/kb/UPDATELOG.md."
        exit 0
    }

    Write-Output "=== WoolCrushPlayble AI Operating System ==="
    Write-Output "Knowledge base protocol (see AGENTS.md for the full contract):"
    Write-Output " - BEFORE any task: read .claude/kb/INDEX.md, then the KB doc(s) for the area in scope. Verify cited paths against current code."
    Write-Output " - AFTER any task that changes architecture/system order/components/scene wiring/paths: update the KB doc + append a line to .claude/kb/UPDATELOG.md."
    Write-Output " - Non-trivial work: planner -> coder -> reviewer subagents (/feature)."
    if (Test-Path $index) {
        Write-Output ""
        Write-Output "--- .claude/kb/INDEX.md ---"
        Get-Content -LiteralPath $index -Encoding UTF8 | ForEach-Object { Write-Output $_ }
    } else {
        Write-Output "(KB INDEX not found at $index)"
    }
}
catch {
    # never block the session on hook failure
}

exit 0
