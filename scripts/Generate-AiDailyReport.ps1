[CmdletBinding()]
param(
    [Parameter()]
    [datetime]$TargetDate = (Get-Date).Date.AddDays(-1),

    [Parameter()]
    [string]$ProjectRoot = "",

    [Parameter()]
    [string]$OutputRoot = ""
)

# 此脚本需要兼容多种日志事件结构，关闭严格模式以容忍可选字段缺失。
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $scriptRoot ".."
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Join-Path $scriptRoot "..") "artifacts\ai-daily-reports"
}

try {
    $resolvedProjectRoot = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ProjectRoot).Path).TrimEnd("\")
} catch {
    $resolvedProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd("\")
}

try {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
} catch {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedProjectRoot "artifacts\ai-daily-reports"))
}

$reportDate = $TargetDate.Date
$windowStart = $reportDate
$windowEnd = $windowStart.AddDays(1)
$dateKey = $windowStart.ToString("yyyy-MM-dd")

if (-not (Test-Path -LiteralPath $resolvedOutputRoot)) {
    [System.IO.Directory]::CreateDirectory($resolvedOutputRoot) | Out-Null
}

function Add-UniqueText {
    <#
    .SYNOPSIS
    向列表中追加唯一文本，避免日报出现重复项。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.ArrayList]$List,

        [Parameter(Mandatory = $true)]
        [hashtable]$SeenMap,

        [Parameter()]
        [AllowNull()]
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return
    }

    $normalizedText = $Text.Trim()
    if (-not $SeenMap.ContainsKey($normalizedText)) {
        [void]$List.Add($normalizedText)
        $SeenMap[$normalizedText] = $true
    }
}

function Add-UniquePath {
    <#
    .SYNOPSIS
    向路径列表中追加唯一文件路径，统一日报里的文件清单。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.ArrayList]$List,

        [Parameter(Mandatory = $true)]
        [hashtable]$SeenMap,

        [Parameter()]
        [AllowNull()]
        [string]$PathText
    )

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        return
    }

    $normalizedPath = $PathText.Trim()
    if (-not $SeenMap.ContainsKey($normalizedPath)) {
        [void]$List.Add($normalizedPath)
        $SeenMap[$normalizedPath] = $true
    }
}

function ConvertFrom-JsonLine {
    <#
    .SYNOPSIS
    将单行 JSONL 安全解析为对象，遇到异常时跳过坏数据。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$Line
    )

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $null
    }

    try {
        return $Line | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Get-NormalizedPath {
    <#
    .SYNOPSIS
    统一路径大小写与尾部分隔符，便于跨日志来源做项目目录比对。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$PathText
    )

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        return $null
    }

    try {
        return [System.IO.Path]::GetFullPath($PathText).TrimEnd("\")
    } catch {
        return $PathText.Trim().TrimEnd("\")
    }
}

function Get-RelativeProjectPath {
    <#
    .SYNOPSIS
    将绝对路径折算为项目相对路径，便于日报展示。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$PathText
    )

    if ([string]::IsNullOrWhiteSpace($PathText)) {
        return $null
    }

    $normalizedPath = Get-NormalizedPath -PathText $PathText
    if ([string]::IsNullOrWhiteSpace($normalizedPath)) {
        return $null
    }

    if ($normalizedPath.StartsWith($resolvedProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $normalizedPath.Substring($resolvedProjectRoot.Length).TrimStart("\")
    }

    return $normalizedPath
}

function Get-LocalDateTime {
    <#
    .SYNOPSIS
    将日志中的时间戳转换为本地时间，兼容 ISO 字符串与 Unix 毫秒时间戳。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        $TimestampValue
    )

    if ($null -eq $TimestampValue) {
        return $null
    }

    try {
        if ($TimestampValue -is [long] -or $TimestampValue -is [int] -or $TimestampValue -is [double]) {
            return [DateTimeOffset]::FromUnixTimeMilliseconds([int64]$TimestampValue).ToLocalTime().DateTime
        }

        $timestampText = [string]$TimestampValue
        if ([string]::IsNullOrWhiteSpace($timestampText)) {
            return $null
        }

        return [DateTimeOffset]::Parse($timestampText).ToLocalTime().DateTime
    } catch {
        return $null
    }
}

function Convert-ToSingleLine {
    <#
    .SYNOPSIS
    将多行文本压平成单行摘要，并限制展示长度。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$Text,

        [Parameter()]
        [int]$MaxLength = 140
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $singleLineText = ($Text -replace "`r?`n", " " -replace "\s+", " ").Trim()
    $singleLineText = $singleLineText -replace "^\s*#+\s*", ""

    if ($singleLineText.Length -gt $MaxLength) {
        return ($singleLineText.Substring(0, $MaxLength - 3).TrimEnd() + "...")
    }

    return $singleLineText
}

function Normalize-CodexUserText {
    <#
    .SYNOPSIS
    提取 Codex 会话里的真实用户需求，剔除 IDE 上下文与系统注入文本。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    if ($Text -match "^\s*# AGENTS\.md instructions") {
        return $null
    }

    if ($Text -match "^\s*<environment_context>") {
        return $null
    }

    $normalizedText = $Text
    if ($normalizedText -match "(?s)## My request for Codex:\s*(.+)$") {
        $normalizedText = $matches[1]
    }

    return Convert-ToSingleLine -Text $normalizedText -MaxLength 180
}

function Get-TextItems {
    <#
    .SYNOPSIS
    从统一内容数组中提取指定类型的文本片段。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        $Items,

        [Parameter(Mandatory = $true)]
        [string[]]$AllowedTypes
    )

    $results = New-Object System.Collections.ArrayList
    $seenMap = @{}

    foreach ($item in @($Items)) {
        if ($null -eq $item) {
            continue
        }

        $itemType = [string]$item.type
        if ($AllowedTypes -notcontains $itemType) {
            continue
        }

        Add-UniqueText -List $results -SeenMap $seenMap -Text ([string]$item.text)
    }

    return $results
}

function Get-FilePathsFromPatchText {
    <#
    .SYNOPSIS
    从 apply_patch 内容中提取新增、更新、删除的文件路径。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string]$PatchText
    )

    $filePaths = New-Object System.Collections.ArrayList
    $seenMap = @{}

    if ([string]::IsNullOrWhiteSpace($PatchText)) {
        return $filePaths
    }

    $matches = [System.Text.RegularExpressions.Regex]::Matches(
        $PatchText,
        "(?m)^\*\*\* (?:Update|Add|Delete) File: (.+)$"
    )

    foreach ($match in $matches) {
        $relativePath = Get-RelativeProjectPath -PathText $match.Groups[1].Value
        Add-UniquePath -List $filePaths -SeenMap $seenMap -PathText $relativePath
    }

    return $filePaths
}

function New-SourceSummary {
    <#
    .SYNOPSIS
    初始化单个 AI 来源的日报聚合对象。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceName
    )

    return [pscustomobject]@{
        Source       = $SourceName
        SessionCount = 0
        SessionTitles = New-Object System.Collections.ArrayList
        Requests     = New-Object System.Collections.ArrayList
        Outcomes     = New-Object System.Collections.ArrayList
        Files        = New-Object System.Collections.ArrayList
        Tools        = New-Object System.Collections.ArrayList
        _SeenTitles  = @{}
        _SeenRequests = @{}
        _SeenOutcomes = @{}
        _SeenFiles   = @{}
        _SeenTools   = @{}
    }
}

function Finalize-SourceSummary {
    <#
    .SYNOPSIS
    移除内部去重字段，只保留最终日报需要的展示数据。
    #>
    param(
        [Parameter(Mandatory = $true)]
        $Summary
    )

    return [pscustomobject]@{
        Source       = $Summary.Source
        SessionCount = $Summary.SessionCount
        SessionTitles = @($Summary.SessionTitles)
        Requests     = @($Summary.Requests)
        Outcomes     = @($Summary.Outcomes)
        Files        = @($Summary.Files)
        Tools        = @($Summary.Tools)
    }
}

function Get-ToolSummaryText {
    <#
    .SYNOPSIS
    将工具调用列表汇总为“工具名 x 次数”的简洁文本。
    #>
    param(
        [Parameter()]
        [AllowNull()]
        [string[]]$Tools
    )

    if ($null -eq $Tools -or $Tools.Count -eq 0) {
        return "无"
    }

    $parts = @(
        $Tools |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Group-Object |
            Sort-Object -Property @{ Expression = "Count"; Descending = $true }, @{ Expression = "Name"; Descending = $false } |
            Select-Object -First 8 |
            ForEach-Object { "{0} x{1}" -f $_.Name, $_.Count }
    )

    if ($parts.Count -eq 0) {
        return "无"
    }

    return ($parts -join "；")
}

function Get-CodexDailySummary {
    <#
    .SYNOPSIS
    汇总 Codex 本地会话中属于当前项目且落在目标日期内的对话内容。
    #>
    param()

    $summary = New-SourceSummary -SourceName "Codex"
    $titleMap = @{}
    $indexFile = Join-Path $env:USERPROFILE ".codex\session_index.jsonl"

    if (Test-Path -LiteralPath $indexFile) {
        foreach ($line in Get-Content -LiteralPath $indexFile -Encoding UTF8) {
            $indexEntry = ConvertFrom-JsonLine -Line $line
            if ($null -eq $indexEntry) {
                continue
            }

            if (-not [string]::IsNullOrWhiteSpace([string]$indexEntry.id)) {
                $titleMap[[string]$indexEntry.id] = Convert-ToSingleLine -Text ([string]$indexEntry.thread_name) -MaxLength 120
            }
        }
    }

    $sessionRoot = Join-Path $env:USERPROFILE ".codex\sessions"
    if (-not (Test-Path -LiteralPath $sessionRoot)) {
        return (Finalize-SourceSummary -Summary $summary)
    }

    $candidateFiles = @(
        Get-ChildItem -LiteralPath $sessionRoot -Recurse -File -Filter "*.jsonl" |
            Where-Object {
                $_.Length -gt 0 -and
                $_.LastWriteTime -ge $windowStart.AddDays(-2) -and
                $_.LastWriteTime -lt $windowEnd.AddDays(2)
            }
    )

    foreach ($file in $candidateFiles) {
        $sessionId = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $sessionPathMatches = $false
        $sessionHasActivity = $false
        $sessionRequests = New-Object System.Collections.ArrayList
        $sessionSeenRequests = @{}
        $sessionOutcomes = New-Object System.Collections.ArrayList
        $sessionSeenOutcomes = @{}
        $sessionFiles = New-Object System.Collections.ArrayList
        $sessionSeenFiles = @{}
        $sessionTools = New-Object System.Collections.ArrayList
        $sessionSeenTools = @{}

        foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
            $entry = ConvertFrom-JsonLine -Line $line
            if ($null -eq $entry) {
                continue
            }

            if ([string]$entry.type -eq "session_meta") {
                if (-not [string]::IsNullOrWhiteSpace([string]$entry.payload.id)) {
                    $sessionId = [string]$entry.payload.id
                }

                $metaCwd = Get-NormalizedPath -PathText ([string]$entry.payload.cwd)
                if ($metaCwd -eq $resolvedProjectRoot) {
                    $sessionPathMatches = $true
                }

                continue
            }

            $entryTime = Get-LocalDateTime -TimestampValue $entry.timestamp
            if ($null -eq $entryTime -or $entryTime -lt $windowStart -or $entryTime -ge $windowEnd) {
                continue
            }

            if (-not $sessionPathMatches) {
                $entryCwd = $null
                if ($null -ne $entry.cwd) {
                    $entryCwd = [string]$entry.cwd
                } elseif ($null -ne $entry.payload -and $null -ne $entry.payload.cwd) {
                    $entryCwd = [string]$entry.payload.cwd
                }

                if ((Get-NormalizedPath -PathText $entryCwd) -eq $resolvedProjectRoot) {
                    $sessionPathMatches = $true
                }
            }

            if (-not $sessionPathMatches) {
                continue
            }

            $sessionHasActivity = $true

            if ([string]$entry.type -ne "response_item") {
                continue
            }

            $payloadType = [string]$entry.payload.type
            switch ($payloadType) {
                "message" {
                    $role = [string]$entry.payload.role
                    if ($role -eq "user") {
                        foreach ($text in Get-TextItems -Items $entry.payload.content -AllowedTypes @("input_text")) {
                            $normalizedRequest = Normalize-CodexUserText -Text ([string]$text)
                            Add-UniqueText -List $sessionRequests -SeenMap $sessionSeenRequests -Text $normalizedRequest
                        }
                    } elseif ($role -eq "assistant" -and [string]$entry.payload.phase -ne "commentary") {
                        foreach ($text in Get-TextItems -Items $entry.payload.content -AllowedTypes @("output_text")) {
                            Add-UniqueText -List $sessionOutcomes -SeenMap $sessionSeenOutcomes -Text (Convert-ToSingleLine -Text ([string]$text) -MaxLength 180)
                        }
                    }
                }
                "function_call" {
                    Add-UniqueText -List $sessionTools -SeenMap $sessionSeenTools -Text ([string]$entry.payload.name)
                }
                "custom_tool_call" {
                    Add-UniqueText -List $sessionTools -SeenMap $sessionSeenTools -Text ([string]$entry.payload.name)

                    if ([string]$entry.payload.name -eq "apply_patch") {
                        foreach ($changedFile in Get-FilePathsFromPatchText -PatchText ([string]$entry.payload.input)) {
                            Add-UniquePath -List $sessionFiles -SeenMap $sessionSeenFiles -PathText ([string]$changedFile)
                        }
                    }
                }
            }
        }

        if (-not $sessionPathMatches -or -not $sessionHasActivity) {
            continue
        }

        $summary.SessionCount++

        if ($titleMap.ContainsKey($sessionId)) {
            Add-UniqueText -List $summary.SessionTitles -SeenMap $summary._SeenTitles -Text $titleMap[$sessionId]
        }

        foreach ($item in $sessionRequests) {
            Add-UniqueText -List $summary.Requests -SeenMap $summary._SeenRequests -Text ([string]$item)
        }

        foreach ($item in $sessionOutcomes) {
            Add-UniqueText -List $summary.Outcomes -SeenMap $summary._SeenOutcomes -Text ([string]$item)
        }

        foreach ($item in $sessionFiles) {
            Add-UniquePath -List $summary.Files -SeenMap $summary._SeenFiles -PathText ([string]$item)
        }

        foreach ($item in $sessionTools) {
            Add-UniqueText -List $summary.Tools -SeenMap $summary._SeenTools -Text ([string]$item)
        }
    }

    return (Finalize-SourceSummary -Summary $summary)
}

function Get-ClaudeDailySummary {
    <#
    .SYNOPSIS
    汇总 Claude Code 项目会话中属于当前项目且落在目标日期内的对话内容。
    #>
    param()

    $summary = New-SourceSummary -SourceName "Claude Code"
    $projectRootPath = Join-Path $env:USERPROFILE ".claude\projects"
    if (-not (Test-Path -LiteralPath $projectRootPath)) {
        return (Finalize-SourceSummary -Summary $summary)
    }

    $candidateFiles = @(
        Get-ChildItem -LiteralPath $projectRootPath -Recurse -File -Filter "*.jsonl" |
            Where-Object {
                $_.FullName -notlike "*\subagents\*" -and
                $_.Length -gt 0 -and
                $_.LastWriteTime -ge $windowStart.AddDays(-2) -and
                $_.LastWriteTime -lt $windowEnd.AddDays(2)
            }
    )

    foreach ($file in $candidateFiles) {
        $sessionHasActivity = $false
        $sessionPathMatches = $false
        $sessionTitle = $null
        $sessionRequests = New-Object System.Collections.ArrayList
        $sessionSeenRequests = @{}
        $sessionOutcomes = New-Object System.Collections.ArrayList
        $sessionSeenOutcomes = @{}
        $sessionFiles = New-Object System.Collections.ArrayList
        $sessionSeenFiles = @{}
        $sessionTools = New-Object System.Collections.ArrayList
        $sessionSeenTools = @{}

        foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
            $entry = ConvertFrom-JsonLine -Line $line
            if ($null -eq $entry) {
                continue
            }

            if ([string]$entry.type -eq "ai-title") {
                $sessionTitle = Convert-ToSingleLine -Text ([string]$entry.aiTitle) -MaxLength 120
                continue
            }

            $entryTime = Get-LocalDateTime -TimestampValue $entry.timestamp
            if ($null -eq $entryTime -or $entryTime -lt $windowStart -or $entryTime -ge $windowEnd) {
                continue
            }

            $entryCwd = $null
            if ($null -ne $entry.cwd) {
                $entryCwd = [string]$entry.cwd
            }

            if ((Get-NormalizedPath -PathText $entryCwd) -eq $resolvedProjectRoot) {
                $sessionPathMatches = $true
            }

            if (-not $sessionPathMatches) {
                continue
            }

            $sessionHasActivity = $true

            switch ([string]$entry.type) {
                "user" {
                    if ($null -ne $entry.toolUseResult) {
                        if ($null -ne $entry.toolUseResult.filePath) {
                            Add-UniquePath -List $sessionFiles -SeenMap $sessionSeenFiles -PathText (Get-RelativeProjectPath -PathText ([string]$entry.toolUseResult.filePath))
                        }

                        continue
                    }

                    foreach ($text in Get-TextItems -Items $entry.message.content -AllowedTypes @("text")) {
                        Add-UniqueText -List $sessionRequests -SeenMap $sessionSeenRequests -Text (Convert-ToSingleLine -Text ([string]$text) -MaxLength 180)
                    }
                }
                "assistant" {
                    foreach ($item in @($entry.message.content)) {
                        if ($null -eq $item) {
                            continue
                        }

                        if ([string]$item.type -eq "text") {
                            Add-UniqueText -List $sessionOutcomes -SeenMap $sessionSeenOutcomes -Text (Convert-ToSingleLine -Text ([string]$item.text) -MaxLength 180)
                        } elseif ([string]$item.type -eq "tool_use") {
                            Add-UniqueText -List $sessionTools -SeenMap $sessionSeenTools -Text ([string]$item.name)
                        }
                    }
                }
                "file-history-snapshot" {
                    $trackedBackups = $entry.snapshot.trackedFileBackups
                    if ($null -eq $trackedBackups) {
                        continue
                    }

                    foreach ($property in $trackedBackups.PSObject.Properties) {
                        Add-UniquePath -List $sessionFiles -SeenMap $sessionSeenFiles -PathText (Get-RelativeProjectPath -PathText ([string]$property.Name))
                    }
                }
            }
        }

        if (-not $sessionPathMatches -or -not $sessionHasActivity) {
            continue
        }

        $summary.SessionCount++

        Add-UniqueText -List $summary.SessionTitles -SeenMap $summary._SeenTitles -Text $sessionTitle

        foreach ($item in $sessionRequests) {
            Add-UniqueText -List $summary.Requests -SeenMap $summary._SeenRequests -Text ([string]$item)
        }

        foreach ($item in $sessionOutcomes) {
            Add-UniqueText -List $summary.Outcomes -SeenMap $summary._SeenOutcomes -Text ([string]$item)
        }

        foreach ($item in $sessionFiles) {
            Add-UniquePath -List $summary.Files -SeenMap $summary._SeenFiles -PathText ([string]$item)
        }

        foreach ($item in $sessionTools) {
            Add-UniqueText -List $summary.Tools -SeenMap $summary._SeenTools -Text ([string]$item)
        }
    }

    return (Finalize-SourceSummary -Summary $summary)
}

function Get-MergedUniqueList {
    <#
    .SYNOPSIS
    合并多个来源列表并保留首次出现顺序，用于日报总览。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Collections
    )

    $result = New-Object System.Collections.ArrayList
    $seenMap = @{}

    foreach ($collection in $Collections) {
        foreach ($item in @($collection)) {
            Add-UniqueText -List $result -SeenMap $seenMap -Text ([string]$item)
        }
    }

    return @($result)
}

$codexSummary = Get-CodexDailySummary
$claudeSummary = Get-ClaudeDailySummary

$allRequests = @(Get-MergedUniqueList -Collections @($codexSummary.Requests, $claudeSummary.Requests))
$allOutcomes = @(Get-MergedUniqueList -Collections @($codexSummary.Outcomes, $claudeSummary.Outcomes))
$allFiles = @(Get-MergedUniqueList -Collections @($codexSummary.Files, $claudeSummary.Files))
$totalSessions = $codexSummary.SessionCount + $claudeSummary.SessionCount

$focusText = if ($allRequests.Count -gt 0) {
    "今天主要围绕 {0} 展开。" -f (($allRequests | Select-Object -First 3) -join "；")
} else {
    "今天未识别到与当前项目目录匹配的有效对话记录。"
}

$reportLines = New-Object System.Collections.ArrayList
[void]$reportLines.Add("# AI 协作日报（$dateKey）")
[void]$reportLines.Add("")
[void]$reportLines.Add($focusText)
[void]$reportLines.Add("")
[void]$reportLines.Add("## 今日概览")
[void]$reportLines.Add("- 统计窗口：$($windowStart.ToString("yyyy-MM-dd HH:mm")) - $($windowEnd.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm"))")
[void]$reportLines.Add(('- 项目目录：`{0}`' -f $resolvedProjectRoot))
[void]$reportLines.Add("- 数据来源：Codex 本地会话历史、Claude Code 本地会话历史")
[void]$reportLines.Add("- 会话数量：共 $totalSessions 个（Codex $($codexSummary.SessionCount) / Claude Code $($claudeSummary.SessionCount)）")
[void]$reportLines.Add("- 识别需求：$($allRequests.Count) 条")
[void]$reportLines.Add("- 识别产出说明：$($allOutcomes.Count) 条")
[void]$reportLines.Add("- 涉及文件：$($allFiles.Count) 个")
[void]$reportLines.Add("")

[void]$reportLines.Add("## 今日事项")
if ($allRequests.Count -gt 0) {
    foreach ($request in ($allRequests | Select-Object -First 10)) {
        [void]$reportLines.Add("- $request")
    }
} else {
    [void]$reportLines.Add("- 无")
}
[void]$reportLines.Add("")

[void]$reportLines.Add("## 完成情况")
if ($allOutcomes.Count -gt 0) {
    foreach ($outcome in ($allOutcomes | Select-Object -First 10)) {
        [void]$reportLines.Add("- $outcome")
    }
} else {
    [void]$reportLines.Add("- 无明确完成说明")
}
[void]$reportLines.Add("")

[void]$reportLines.Add("## Codex 摘要")
[void]$reportLines.Add("- 会话主题：{0}" -f ($(if ($codexSummary.SessionTitles.Count -gt 0) { ($codexSummary.SessionTitles | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 识别需求：{0}" -f ($(if ($codexSummary.Requests.Count -gt 0) { ($codexSummary.Requests | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 产出说明：{0}" -f ($(if ($codexSummary.Outcomes.Count -gt 0) { ($codexSummary.Outcomes | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 使用工具：$(Get-ToolSummaryText -Tools $codexSummary.Tools)")
[void]$reportLines.Add("")

[void]$reportLines.Add("## Claude Code 摘要")
[void]$reportLines.Add("- 会话主题：{0}" -f ($(if ($claudeSummary.SessionTitles.Count -gt 0) { ($claudeSummary.SessionTitles | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 识别需求：{0}" -f ($(if ($claudeSummary.Requests.Count -gt 0) { ($claudeSummary.Requests | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 产出说明：{0}" -f ($(if ($claudeSummary.Outcomes.Count -gt 0) { ($claudeSummary.Outcomes | Select-Object -First 5) -join "；" } else { "无" })))
[void]$reportLines.Add("- 使用工具：$(Get-ToolSummaryText -Tools $claudeSummary.Tools)")
[void]$reportLines.Add("")

[void]$reportLines.Add("## 涉及文件")
if ($allFiles.Count -gt 0) {
    foreach ($filePath in ($allFiles | Sort-Object)) {
        [void]$reportLines.Add(('- `{0}`' -f $filePath))
    }
} else {
    [void]$reportLines.Add("- 无")
}
[void]$reportLines.Add("")

[void]$reportLines.Add("## 自动备注")
[void]$reportLines.Add("- 本日报依据本地对话历史自动归纳，可直接作为日报草稿使用。")
[void]$reportLines.Add("- 默认计划任务会在每天 11:00 生成前一日完整日报。")
[void]$reportLines.Add("- 对话外的线下操作、手工改动或未落盘日志不会出现在本报告中。")
[void]$reportLines.Add("")
[void]$reportLines.Add("> 生成时间：$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")

$reportTextLines = @($reportLines | ForEach-Object { [string]$_ })
$reportContent = ([string]::Join([Environment]::NewLine, $reportTextLines) + [Environment]::NewLine)
$reportPath = Join-Path $resolvedOutputRoot "$dateKey.md"
$latestPath = Join-Path $resolvedOutputRoot "latest.md"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

[System.IO.File]::WriteAllText($reportPath, $reportContent, $utf8NoBom)
[System.IO.File]::WriteAllText($latestPath, $reportContent, $utf8NoBom)

Write-Host "AI 日报已生成：$reportPath"
Write-Host "固定最新文件：$latestPath"
