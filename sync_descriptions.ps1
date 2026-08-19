$readmePath = "README.md"
$workshopPath = "description_workshop.txt"
$descPath = "description.txt"

if (-not (Test-Path $readmePath)) {
    Write-Host "README.md not found." -ForegroundColor Red
    exit
}

$readme = Get-Content -Raw -Encoding UTF8 $readmePath

# Strip ONLY the ## Demo section (from '## ... Demo' heading up to the next '##' or end of file)
$readmeCleaned = $readme -replace '(?m)^##\s+.*?Demo[^\r\n]*\r?\n(?s:.*?)(?=\r?\n##|\Z)', ''

function Convert-MarkdownToBBCode {
    param ([string]$Text)

    # Normalize line endings
    $Text = $Text -replace "\r\n", "`n" -replace "\r", "`n"

    $lines = $Text -split "`n"
    $output = [System.Collections.Generic.List[string]]::new()
    $inList = $false

    foreach ($line in $lines) {
        $processedLine = $line

        # 1. Convert Headings
        $processedLine = $processedLine -replace '^#\s+(.+)$', '[h1]$1[/h1]'
        $processedLine = $processedLine -replace '^##\s+(.+)$', '[h2]$1[/h2]'
        $processedLine = $processedLine -replace '^###\s+(.+)$', '[h3]$1[/h3]'

        # 2. Check if this line is a list item
        $isListItem = $false
        if ($processedLine -match '^[ \t]*[*\-]\s+(.+)$') {
            $isListItem = $true
            if (-not $inList) {
                $output.Add("[list]")
                $inList = $true
            }
            # Replace bullet marker with [*]
            $processedLine = "[*]" + $Matches[1]
        } else {
            if ($inList) {
                $output.Add("[/list]")
                $inList = $false
            }
        }

        # 3. Apply inline formatting to the line content ONLY
        $processedLine = $processedLine -replace '\[([^\]]+)\]\(([^)]+)\)', '[url=$2]$1[/url]'
        $processedLine = $processedLine -replace '~~([^~]+)~~', '[strike]$1[/strike]'
        $processedLine = $processedLine -replace '\*\*([^*]+)\*\*', '[b]$1[/b]'
        $processedLine = $processedLine -replace '(?<=\s|^|\b)\*([^*]+)\*(?=\s|$|\b)', '[i]$1[/i]'

        $output.Add($processedLine)
    }

    if ($inList) {
        $output.Add("[/list]")
    }

    return $output -join "`n"
}

function Extract-FeaturesRaw {
    param ([string]$Text)

    if ($Text -match '(?s)##\s*.*?Features\s*\r?\n(.*?)(?=\r?\n##|\Z)') {
        $featuresBlock = $Matches[1].Trim()
        $lines = $featuresBlock -split "\r?\n"
        $extracted = [System.Collections.Generic.List[string]]::new()

        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }

            if ($trimmed -match '^[*\-]\s+(.+)$') {
                $content = $Matches[1]
                $content = $content -replace '\*\*([^*]+)\*\*', '$1'
                $content = $content -replace '\[([^\]]+)\]\([^)]+\)', '$1'
                $extracted.Add("- $content")
            }
            elseif ($line -match '^[ \t]{2,}[*\-]\s+(.+)$') {
                $content = $Matches[1]
                $content = $content -replace '\*\*([^*]+)\*\*', '$1'
                $content = $content -replace '\[([^\]]+)\]\([^)]+\)', '$1'
                $extracted.Add("  - $content")
            }
        }
        return $extracted -join "`n"
    }
    return ""
}

# 1. Convert cleaned README to BBCode
$bbcode = Convert-MarkdownToBBCode -Text $readmeCleaned
Set-Content -Path $workshopPath -Value $bbcode -Encoding UTF8
Write-Host "Updated $workshopPath" -ForegroundColor Green

# 2. Extract features to raw text description
$features = Extract-FeaturesRaw -Text $readmeCleaned
$desc = @"
A Quality of Life expansion for the Requisition mod.

FEATURES:
$features

Requires Requisition (Base Mod) to function.
"@

Set-Content -Path $descPath -Value $desc -Encoding UTF8
Write-Host "Updated $descPath" -ForegroundColor Green