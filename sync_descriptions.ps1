$readmePath = "README.md"
$workshopPath = "description_workshop.txt"
$descPath = "description.txt"

if (-not (Test-Path $readmePath)) {
    Write-Host "README.md not found." -ForegroundColor Red
    exit
}

$readme = Get-Content -Raw -Encoding UTF8 $readmePath

function Convert-MarkdownToBBCode {
    param ([string]$Text)

    # Normalize line endings to \n to prevent \r from splitting closing tags
    $Text = $Text -replace "\r\n", "`n" -replace "\r", "`n"

    # Convert Headings
    $Text = $Text -replace '(?m)^#\s+(.+)$', '[h1]$1[/h1]'
    $Text = $Text -replace '(?m)^##\s+(.+)$', '[h2]$1[/h2]'
    $Text = $Text -replace '(?m)^###\s+(.+)$', '[h3]$1[/h3]'

    # Convert Links: [text](url) -> [url=url]text[/url]
    $Text = $Text -replace '\[([^\]]+)\]\(([^)]+)\)', '[url=$2]$1[/url]'

    # Convert Formatting
    $Text = $Text -replace '\*\*([^*]+)\*\*', '[b]$1[/b]'
    $Text = $Text -replace '\*([^*]+)\*', '[i]$1[/i]'
    $Text = $Text -replace '~~([^~]+)~~', '[strike]$1[/strike]'

    # Process lists into [list]...[/list] blocks
    $lines = $Text -split "`n"
    $output = [System.Collections.Generic.List[string]]::new()
    $inList = $false

    foreach ($line in $lines) {
        if ($line -match '^[ \t]*[*\-]\s+(.+)$') {
            if (-not $inList) {
                $output.Add("[list]")
                $inList = $true
            }
            $output.Add("[*]$($Matches[1])")
        } else {
            if ($inList) {
                $output.Add("[/list]")
                $inList = $false
            }
            $output.Add($line)
        }
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

# 1. Convert README to BBCode
$bbcode = Convert-MarkdownToBBCode -Text $readme
Set-Content -Path $workshopPath -Value $bbcode -Encoding UTF8
Write-Host "Updated $workshopPath" -ForegroundColor Green

# 2. Extract features to raw text description
$features = Extract-FeaturesRaw -Text $readme
$desc = @"
A Quality of Life expansion for the Requisition mod.

FEATURES:
$features

Requires Requisition (Base Mod) to function.
"@

Set-Content -Path $descPath -Value $desc -Encoding UTF8
Write-Host "Updated $descPath" -ForegroundColor Green