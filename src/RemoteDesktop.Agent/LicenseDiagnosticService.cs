using System.Diagnostics;
using System.Text;

namespace RemoteDesktop.Agent;

internal static class LicenseDiagnosticService
{
    private const int MaximumResultLength = 60_000;

    private const string DiagnosticScript = """
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        $ErrorActionPreference = 'Stop'
        $failed = $false

        function Write-Section([string] $title) {
            Write-Output ''
            Write-Output ('===== ' + $title + ' =====')
        }

        Write-Section 'WINDOWS'
        try {
            $os = Get-CimInstance -ClassName Win32_OperatingSystem
            Write-Output ('Hệ điều hành: ' + $os.Caption)
            Write-Output ('Phiên bản: ' + $os.Version + ' (Build ' + $os.BuildNumber + ')')

            $statusNames = @(
                'Chưa kích hoạt',
                'Đã kích hoạt',
                'Thời gian gia hạn OOB',
                'Thời gian gia hạn OOT',
                'Thời gian gia hạn không chính hãng',
                'Đang ở trạng thái thông báo',
                'Thời gian gia hạn mở rộng'
            )
            $windowsProducts = Get-CimInstance -ClassName SoftwareLicensingProduct -Filter "ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f'" |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_.PartialProductKey) } |
                Sort-Object -Property Name -Unique

            if (-not $windowsProducts) {
                throw 'Không tìm thấy thông tin bản quyền Windows.'
            }

            foreach ($product in $windowsProducts) {
                $status = if ($product.LicenseStatus -ge 0 -and $product.LicenseStatus -lt $statusNames.Count) {
                    $statusNames[$product.LicenseStatus]
                } else {
                    'Không xác định (' + $product.LicenseStatus + ')'
                }

                Write-Output ('Sản phẩm: ' + $product.Name)
                Write-Output ('Trạng thái: ' + $status)
                Write-Output ('5 ký tự cuối product key: ' + $product.PartialProductKey)
            }
        } catch {
            $failed = $true
            Write-Output ('LỖI WINDOWS: ' + $_.Exception.Message)
        }

        Write-Section 'MICROSOFT 365 / OFFICE'
        try {
            $programFiles = [Environment]::GetFolderPath('ProgramFiles')
            $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
            $roots = @($programFiles, $programFilesX86) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
            $vnextCandidates = foreach ($root in $roots) {
                Join-Path $root 'Microsoft Office\Office16\vnextdiag.ps1'
                Join-Path $root 'Microsoft Office\root\Office16\vnextdiag.ps1'
            }
            $osppCandidates = foreach ($root in $roots) {
                Join-Path $root 'Microsoft Office\Office16\OSPP.VBS'
                Join-Path $root 'Microsoft Office\root\Office16\OSPP.VBS'
                Join-Path $root 'Microsoft Office\Office15\OSPP.VBS'
                Join-Path $root 'Microsoft Office\Office14\OSPP.VBS'
            }

            $foundTool = $false
            $vnextPath = $vnextCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
            if ($vnextPath) {
                $foundTool = $true
                Write-Output 'Microsoft 365 Apps:'
                & $vnextPath -action list 2>&1 | Out-String -Width 240 | Write-Output
            }

            $osppPath = $osppCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
            if ($osppPath) {
                $foundTool = $true
                Write-Output 'Office bản quyền số lượng lớn / Office LTSC:'
                & "$env:SystemRoot\System32\cscript.exe" //Nologo $osppPath /dstatusall 2>&1 | Out-String -Width 240 | Write-Output
                if ($LASTEXITCODE -ne 0) {
                    throw ('OSPP.VBS trả về mã lỗi ' + $LASTEXITCODE + '.')
                }
            }

            if (-not $foundTool) {
                Write-Output 'Không tìm thấy Microsoft Office hoặc công cụ kiểm tra bản quyền Office trên máy này.'
            }
        } catch {
            $failed = $true
            Write-Output ('LỖI OFFICE: ' + $_.Exception.Message)
        }

        Write-Output ''
        Write-Output ('Thời điểm kiểm tra: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
        if ($failed) { exit 1 }
        exit 0
        """;

    public static async Task<LicenseCheckResult> RunAsync(string machineId)
    {
        try
        {
            var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(DiagnosticScript));
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return Failure(machineId, "Không thể khởi chạy PowerShell để kiểm tra bản quyền.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return Failure(machineId, "Quá thời gian kiểm tra bản quyền (90 giây). Máy có thể đang chặn PowerShell hoặc dịch vụ bản quyền không phản hồi.");
            }

            var output = Limit((await outputTask).Trim());
            var error = Limit((await errorTask).Trim());
            var succeeded = process.ExitCode == 0 && output.Length > 0;
            var failureMessage = succeeded
                ? null
                : error.Length > 0
                    ? error
                    : "Không đọc được đầy đủ thông tin bản quyền. Hãy kiểm tra quyền chạy PowerShell và dịch vụ Software Protection trên máy người dùng.";

            return new LicenseCheckResult
            {
                MachineId = machineId,
                Succeeded = succeeded,
                Details = output,
                Error = failureMessage,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            return Failure(machineId, exception.Message);
        }
    }

    private static LicenseCheckResult Failure(string machineId, string message)
    {
        return new LicenseCheckResult
        {
            MachineId = machineId,
            Succeeded = false,
            Error = message,
            CheckedAtUtc = DateTime.UtcNow
        };
    }

    private static string Limit(string value)
    {
        return value.Length <= MaximumResultLength
            ? value
            : value[..MaximumResultLength] + Environment.NewLine + "[Kết quả đã được rút gọn vì quá dài.]";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The process may have exited while the timeout was being handled.
        }
    }
}
