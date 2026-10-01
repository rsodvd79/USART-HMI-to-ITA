# USART HMI to ITA

Traduzione in italiano dell'interfaccia di **USART HMI** (software cinese per display TJC / Nextion‑compatibili), che esiste solo in cinese.

Il metodo si basa sul lavoro di [audiobrian/usart_hmi_english_translation](https://github.com/audiobrian/usart_hmi_english_translation) (traduzione inglese): ne riusa il decryptor di `ACTR.dll`, ma sostituisce la modifica manuale con dnSpy con una patch automatica e fornisce traduzioni italiane.

> **Avvertenze**
> - Strumento per uso personale/educativo (localizzazione). Non contiene né ridistribuisce il software USART HMI né i suoi file decifrati.
> - Modifica file in `C:\Program Files (x86)\USART HMI`. Viene creato un backup, ma usalo a tuo rischio.
> - Testato su USART HMI **1.68.1** (Windows 10/11).

## Come funziona

1. `USART HMI.exe` carica 21 assembly .NET cifrati da `ACTR.dll`.
2. Lo script li decifra con il decryptor del repo upstream (compilato in locale dal sorgente).
3. Il programma in `src/Patcher` (Mono.Cecil) inserisce all'inizio del metodo `hmitype.LanguageApp.Language(string)` una chiamata a un helper (`src/HmiTr`, copiato dentro `hmitype.dll`: nessuna DLL aggiuntiva). L'helper cerca il testo cinese in `c:\devel\hmi_translation.txt`; se lo trova restituisce la traduzione, altrimenti prosegue il codice originale.
4. I testi cinesi non ancora tradotti vengono scritti in `c:\devel\hmi.log`.
5. Le DLL decifrate/patchate vengono copiate nella cartella di installazione, che ha la precedenza su `ACTR.dll`.

## Requisiti

- USART HMI installato in `C:\Program Files (x86)\USART HMI`
- Windows con PowerShell e **privilegi di amministratore**
- [Git](https://git-scm.com/) e [.NET SDK 9](https://dotnet.microsoft.com/download) nel `PATH`
- Connessione internet (clone del repo upstream e pacchetto NuGet Mono.Cecil)

## Installazione

1. Chiudi USART HMI.
2. Clona questo repo:
   ```powershell
   git clone https://github.com/<tuo-utente>/USART-HMI-to-ITA.git
   cd USART-HMI-to-ITA
   ```
3. Apri PowerShell **come amministratore** ed esegui:
   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\scripts\Install.ps1
   ```
   Se la cartella di installazione è diversa: `.\scripts\Install.ps1 -InstallDir "D:\USART HMI"`.
4. Avvia USART HMI: menu, barre e toolbox sono in italiano.

Lo script esegue: backup delle DLL originali in `backup_original`, decifratura, patch, copia dei file, creazione di `c:\devel\` e installazione di `hmi_translation.txt` (se esisteva già, viene salvato come `.bak`).

## Aggiungere o correggere traduzioni

1. Usa il programma e apri `c:\devel\hmi.log`: contiene i testi cinesi non tradotti.
2. Aggiungi righe a `c:\devel\hmi_translation.txt`:
   ```
   testo cinese<TAB>traduzione italiana
   ```
   - codifica **UTF-8**, separatore **TAB** (non spazi)
   - corrispondenza esatta e case‑sensitive; vale la prima occorrenza
3. Riavvia USART HMI (il file viene riletto quando cambia).

Se aggiungi traduzioni utili, copiale anche in `translation/hmi_translation.txt` e apri una pull request.

## Disinstallazione / ripristino

Da PowerShell come amministratore:
```powershell
.\scripts\Restore.ps1
```
Ripristina le DLL originali dal backup e rimuove quelle aggiunte, così l'app torna a caricare `ACTR.dll`. Puoi eliminare `c:\devel\` a mano.

## Risoluzione dei problemi

| Problema | Soluzione |
|---|---|
| "Chiudi USART HMI prima di continuare" | Termina il processo `USART HMI` dal Task Manager. |
| Testi ancora in cinese | Il testo non è nel file: guarda `hmi.log` e aggiungilo. Verifica TAB e UTF‑8. |
| L'app si chiude all'avvio | Esegui `Restore.ps1`. Probabile versione di USART HMI diversa da 1.68.1. |
| Dopo un aggiornamento di USART HMI torna tutto in cinese | L'aggiornamento sovrascrive le DLL: riesegui `Install.ps1`. |
| Il log non viene creato | Controlla che `c:\devel\` esista e che `hmi_translation.txt` sia presente. |

## Struttura del repo

```
scripts/Install.ps1                 installazione automatica
scripts/Restore.ps1                 ripristino
src/HmiTr/T.cs                      helper di traduzione (target .NET 3.5, incorporato in hmitype.dll)
src/Patcher/Program.cs              patcher Mono.Cecil
translation/hmi_translation.txt     traduzioni cinese → italiano
```

## Crediti e licenza

- Idea e decryptor: [audiobrian/usart_hmi_english_translation](https://github.com/audiobrian/usart_hmi_english_translation) (scaricato a runtime, non incluso qui).
- USART HMI è proprietà dei rispettivi titolari. Il codice e le traduzioni di questo repo sono rilasciati con licenza MIT.
