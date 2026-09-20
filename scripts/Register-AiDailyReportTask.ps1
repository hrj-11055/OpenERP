[CmdletBinding()]
param(
    [Parameter()]
    [string]$TaskName = "OpenERP-AI-Daily-Report",

    [Parameter()]
    [string]$RunAt = "11:00"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

function Test-ValidDailyTime {
    <#
    .SYNOPSIS
    校验计划任务的执行时间格式，要求为 24 小时制 HH:mm。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$TimeText
    )

    return [System.Text.RegularExpressions.Regex]::IsMatch($TimeText, "^(?:[01]\d|2[0-3]):[0-5]\d$")
}

if (-not (Test-ValidDailyTime -TimeText $RunAt)) {
    throw "时间格式无效，请使用 24 小时制 HH:mm，例如 11:00。"
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..")).TrimEnd("\")
$scriptPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "Generate-AiDailyReport.ps1"))

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "未找到日报生成脚本：$scriptPath"
}

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$powershellPath = (Get-Command powershell.exe).Source
$actionArguments = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -ProjectRoot `"$projectRoot`""

try {
    $triggerTime = [datetime]::ParseExact($RunAt, "HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
    $action = New-ScheduledTaskAction -Execute $powershellPath -Argument $actionArguments
    $trigger = New-ScheduledTaskTrigger -Daily -At $triggerTime
    $principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew

    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    }

    Register-ScheduledTask `
        -TaskName $TaskName `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Description "基于 Codex 与 Claude Code 本地会话历史，自动生成 OpenERP 前一日日报。"

    $taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName
    Write-Host "计划任务已注册：$TaskName"
    Write-Host "执行时间：每天 $RunAt"
    Write-Host "运行账号：$currentUser"
    Write-Host "下次运行：$($taskInfo.NextRunTime)"
} catch {
    throw "计划任务注册失败：$($_.Exception.Message)"
}
