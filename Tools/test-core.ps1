$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$mono = "C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Data\MonoBleedingEdge\bin\mono.exe"
$compiler = "C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Data\MonoBleedingEdge\lib\mono\4.5\csc.exe"
$outputDirectory = Join-Path $root "Temp\CoreTests"
$output = Join-Path $outputDirectory "PuzzleTests.exe"
$source = Join-Path $root "Assets\AquaPath\Scripts\Core\Puzzle.cs"
$tests = Join-Path $root "Tests\PuzzleTests.cs"

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
& $mono $compiler -nologo -warnaserror+ -out:$output $source $tests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $mono $output
exit $LASTEXITCODE
