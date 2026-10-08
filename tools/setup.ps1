# setup.ps1 — установка Pcad-Kit: junction скилла, PKIT_HOME, сборка DbxHost, дым-тест.
# Запуск: powershell -ExecutionPolicy Bypass -File tools\setup.ps1
# Прав администратора не требует.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "== Pcad-Kit setup =="
Write-Host "Корень набора: $root"

# 1. Junction ~/.kimi-code/skills/pcad -> <корень>\skill
$skillsDir = Join-Path $env:USERPROFILE ".kimi-code\skills"
$link = Join-Path $skillsDir "pcad"
$target = Join-Path $root "skill"
New-Item -ItemType Directory -Force -Path $skillsDir | Out-Null
if (Test-Path $link) {
    $cur = (Get-Item $link).Target
    if ($cur -ne $target) {
        Write-Host "junction $link ведёт в $cur — пересоздаю"
        cmd /c rmdir "$link"
    }
}
if (-not (Test-Path $link)) {
    cmd /c mklink /J "$link" "$target" | Out-Null
    Write-Host "junction: $link -> $target"
}
if (-not (Test-Path (Join-Path $link "SKILL.md"))) {
    throw "junction сломан: $link\SKILL.md не найден"
}

# 2. PKIT_HOME в профиль пользователя
[Environment]::SetEnvironmentVariable("PKIT_HOME", $root, "User")
$env:PKIT_HOME = $root
Write-Host "PKIT_HOME=$root (профиль пользователя)"

# 3. Сборка DbxHost.exe
& (Join-Path $root "tools\dbx-host\build.bat")
if ($LASTEXITCODE -ne 0) { throw "сборка DbxHost не удалась" }

# 4. Python
$py = Get-Command python -ErrorAction SilentlyContinue
if (-not $py) { throw "python не найден в PATH (нужен 3.11+, любой разрядности)" }
python -c "import sys; assert sys.version_info >= (3, 11), 'нужен Python 3.11+'"
Write-Host "python: $(python --version 2>&1)"

# 5. Дым-тест: импорт и пути (схему не открываем — Sch.exe может быть занят)
python -c "import os, sys; sys.path.insert(0, os.environ['PKIT_HOME']); import pkit as pk; print('pkit', pk.__version__, '|', pk.pcad_root())"
if ($LASTEXITCODE -ne 0) { throw "дым-тест pkit не прошёл" }

Write-Host ""
Write-Host "Готово. Полный тест (откроет Sch.exe на копии): python tests\test_dump.py"
Write-Host "Перезапустите Kimi Code, чтобы скилл 'pcad' появился в списке."
