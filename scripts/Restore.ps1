#requires -RunAsAdministrator
# Ripristina le DLL originali di USART HMI dal backup creato da Install.ps1.
param([string]$InstallDir = "C:\Program Files (x86)\USART HMI")

$ErrorActionPreference = 'Stop'
$backup = Join-Path $InstallDir 'backup_original'
if (-not (Test-Path $backup)) { throw "Backup non trovato: $backup" }
if (Get-Process 'USART HMI' -ErrorAction SilentlyContinue) { throw 'Chiudi USART HMI prima di continuare.' }

Copy-Item "$backup\*" $InstallDir -Force
# DLL aggiunte dall'installazione (non presenti nel backup) vanno rimosse per tornare a caricare ACTR.dll
$orig = (Get-ChildItem $backup).Name
Get-ChildItem $InstallDir -Filter *.dll |
    Where-Object { $_.Name -notin $orig -and $_.Name -ne 'ACTR.dll' } |
    Remove-Item -Force
Write-Host 'Ripristino completato.' -ForegroundColor Green
