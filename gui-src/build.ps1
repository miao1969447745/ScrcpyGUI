$ErrorActionPreference = 'Stop'

$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetDir = Split-Path -Parent $sourceDir
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The built-in C# compiler was not found.'
}

& $compiler /nologo /target:winexe /optimize+ `
    /out:"$targetDir\ScrcpyGUI.exe" `
    /win32manifest:"$sourceDir\app.manifest" `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$sourceDir\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code: $LASTEXITCODE"
}

Write-Host "Built: $targetDir\ScrcpyGUI.exe"
