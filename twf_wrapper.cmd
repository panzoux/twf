@echo off
set "CWD_FILE=%TEMP%\twf_cwd_%RANDOM%.txt"
if exist "%CWD_FILE%" del "%CWD_FILE%"

:: Run TWF with the -cwd argument
:: Assumes twf.exe is in the same directory or in PATH
twf.exe -cwd "%CWD_FILE%" %*

:: Check if the file exists (meaning ExitApplicationAndChangeDirectory was called)
if exist "%CWD_FILE%" (
    set /p NEW_DIR=<%"%CWD_FILE%"
    del "%CWD_FILE%"
    if not "%NEW_DIR%"=="" (
        cd /d "%NEW_DIR%"
    )
)

