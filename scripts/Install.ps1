#requires -RunAsAdministrator
<#
.SYNOPSIS
  Traduce USART HMI in italiano (decifra, patcha hmitype.dll, installa).
.PARAMETER InstallDir
  Cartella di installazione di USART HMI.
#>
param([string]$InstallDir = "C:\Program Files (x86)\USART HMI")

$ErrorActionPreference = 'Stop'
$root   = Split-Path $PSScriptRoot -Parent
$work   = Join-Path $env:TEMP 'usart-hmi-ita'
$backup = Join-Path $InstallDir 'backup_original'
$dec    = Join-Path $InstallDir 'decrypted'
$upstream = 'https://github.com/audiobrian/usart_hmi_english_translation'

if (-not (Test-Path "$InstallDir\ACTR.dll")) { throw "ACTR.dll non trovato in $InstallDir" }
if (Get-Process 'USART HMI' -ErrorAction SilentlyContinue) { throw "Chiudi USART HMI prima di continuare." }
foreach ($t in 'git','dotnet') { if (-not (Get-Command $t -ErrorAction SilentlyContinue)) { throw "$t non trovato nel PATH" } }

Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
New-Item $work -ItemType Directory | Out-Null

Write-Host '[1/5] Backup dei file originali...'
if (-not (Test-Path $backup)) {
    New-Item $backup -ItemType Directory | Out-Null
    Get-ChildItem $InstallDir -Filter *.dll | Copy-Item -Destination $backup
}

Write-Host '[2/5] Compilazione di ACTRDecryptor (dal repo upstream) e decifratura di ACTR.dll...'
git clone --quiet $upstream "$work\upstream"
Push-Location "$work\upstream\tools"
dotnet build ACTRDecryptor.csproj -c Release -o "$work\decryptor" | Out-Null
Pop-Location
Copy-Item "$work\upstream\tools\AppDllPass.dll" "$work\decryptor" -Force
Push-Location "$work\decryptor"
& .\ACTRDecryptor.exe decode "$InstallDir\ACTR.dll" | Out-Null
Pop-Location
if ((Get-ChildItem $dec -Filter *.dll -ErrorAction SilentlyContinue).Count -lt 21) { throw 'Decifratura fallita: DLL attese: 21' }

Write-Host '[3/5] Compilazione dell''helper di traduzione e patch di hmitype.dll...'
dotnet build "$root\src\HmiTr\HmiTr.csproj" -c Release -o "$work\hmitr" | Out-Null
dotnet run --project "$root\src\Patcher\Patcher.csproj" -c Release -- "$dec\hmitype.dll" "$work\hmitr\HmiTr.dll" "$work\hmitype.dll"
if ($LASTEXITCODE -ne 0) { throw 'Patch fallita' }

Write-Host '[4/5] Installazione...'
Copy-Item "$dec\*.dll" $InstallDir -Force
Copy-Item "$work\hmitype.dll" $InstallDir -Force
Remove-Item $dec -Recurse -Force

New-Item 'c:\devel' -ItemType Directory -Force | Out-Null
$dest = 'c:\devel\hmi_translation.txt'
if (Test-Path $dest) {
    Copy-Item $dest "$dest.bak" -Force
    Write-Host "      Traduzioni esistenti salvate in $dest.bak"
}
Copy-Item "$root\translation\hmi_translation.txt" $dest -Force

Write-Host '[5/5] Pulizia...'
Remove-Item $work -Recurse -Force
Write-Host 'Fatto. Avvia USART HMI.' -ForegroundColor Green
