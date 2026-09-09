# verify.ps1 이 쓰는 검증용 빈 창. 창 핸들을 인수로 받은 파일에 적어 두고 떠 있는다.
param([Parameter(Mandatory = $true)][string]$HandleFile)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# 검증 대상과 같은 좌표계를 쓰도록 DPI 인식을 켠다.
Add-Type -Namespace VerifyHost -Name Dpi -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
'@
try { [VerifyHost.Dpi]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

$form = New-Object System.Windows.Forms.Form
$form.Text = "Rectangle 검증용 창"
$form.Size = New-Object System.Drawing.Size(700, 480)
$form.StartPosition = 'Manual'
$form.Location = New-Object System.Drawing.Point(120, 120)
$form.BackColor = [System.Drawing.Color]::White

$form.Add_Shown({
    [System.IO.File]::WriteAllText($HandleFile, $form.Handle.ToInt64().ToString())
})

[System.Windows.Forms.Application]::Run($form)
