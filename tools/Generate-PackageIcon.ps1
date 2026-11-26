# Renders the NuGet package icon (assets/muzzle-icon.png, 128 x 128) from vector shapes with WPF, so the icon is
# reproducible from source. Run: pwsh -sta tools/Generate-PackageIcon.ps1
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\assets\muzzle-icon.png'),
    [int]$Size = 128
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

function New-Color([string]$hex) { return [System.Windows.Media.ColorConverter]::ConvertFromString($hex) }

function New-Brush([string]$hex) {
    $brush = [System.Windows.Media.SolidColorBrush]::new((New-Color $hex))
    $brush.Freeze()
    return $brush
}

$scale = $Size / 128.0
$visual = [System.Windows.Media.DrawingVisual]::new()
$context = $visual.RenderOpen()
$context.PushTransform([System.Windows.Media.ScaleTransform]::new($scale, $scale))

# Soft blush-to-lavender squircle.
$background = [System.Windows.Media.LinearGradientBrush]::new((New-Color '#FDE7EC'), (New-Color '#EEEAFA'), [System.Windows.Point]::new(0, 0), [System.Windows.Point]::new(1, 1))
$background.Freeze()
$context.DrawRoundedRectangle($background, $null, [System.Windows.Rect]::new(4, 4, 120, 120), 30, 30)

# Gentle top highlight.
$highlight = [System.Windows.Media.LinearGradientBrush]::new((New-Color '#80FFFFFF'), (New-Color '#00FFFFFF'), 90)
$highlight.Freeze()
$context.DrawRoundedRectangle($highlight, $null, [System.Windows.Rect]::new(4, 4, 120, 60), 30, 30)

# The toe-bean paw (same proportions as the gallery's paw icon, scaled to the tile).
$paw = New-Brush '#F497A9'
$shadow = New-Brush '#26B4405C'
$pads = @(
    @(64, 80, 25, 21),   # main pad
    @(34.5, 55, 9.8, 11.4),
    @(52.5, 38, 10.1, 12),
    @(75.5, 38, 10.1, 12),
    @(93.5, 55, 9.8, 11.4)
)
foreach ($pad in $pads) {
    $context.DrawEllipse($shadow, $null, [System.Windows.Point]::new($pad[0], $pad[1] + 2.5), $pad[2], $pad[3])
}
foreach ($pad in $pads) {
    $context.DrawEllipse($paw, $null, [System.Windows.Point]::new($pad[0], $pad[1]), $pad[2], $pad[3])
}

# A little shine on the main pad.
$shine = New-Brush '#59FFFFFF'
$context.DrawEllipse($shine, $null, [System.Windows.Point]::new(55, 72), 7, 4.5)

$context.Pop()
$context.Close()

$bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($Size, $Size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($visual)
$encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$directory = Split-Path $OutputPath -Parent
if (-not (Test-Path $directory)) { $null = New-Item -ItemType Directory -Path $directory }
$stream = [System.IO.File]::Create($OutputPath)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
"wrote $OutputPath ($Size x $Size)"