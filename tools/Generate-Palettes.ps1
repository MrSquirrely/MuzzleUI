# Generates Muzzle's palette and accent dictionaries from one set of token tables, so every file shares the same keys.
#
#   Themes/Muzzle.Light.xaml, Themes/Muzzle.Dark.xaml                 base palettes (colours, brushes, shadows)
#   Themes/Accents/Muzzle.Accent.<Name>.<Light|Dark>.xaml               accent overlays (primary / secondary / tertiary)
#
# Run from anywhere:  pwsh tools/Generate-Palettes.ps1
param([string]$ThemesDir = (Join-Path $PSScriptRoot '..\src\Muzzle.Theme\Themes'))

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------------------------------------------
# Base palette tokens: key = @(light, dark)
# ---------------------------------------------------------------------------------------------------------------
$tokens = [ordered]@{
    'Canvas'                 = @('#FAF8F6', '#181622')
    'CanvasAlt'              = @('#F4F0EC', '#1F1C2A')
    'Surface'                = @('#FFFFFF', '#221F2E')
    'SurfaceRaised'          = @('#FFFDFB', '#2A2638')
    'SurfaceBorder'          = @('#EFECE8', '#342F45')
    'SurfaceRimHighlight'    = @('#EFECE8', '#3D3750')
    'ControlBackground'      = @('#FFFFFF', '#221F2E')
    'ControlBorder'          = @('#E6E0DA', '#3A344C')
    'ControlBorderHover'     = @('#D8D0C9', '#4A4360')
    'ControlBorderStrong'    = @('#C4BAB1', '#5E596E')
    'Track'                  = @('#ECE6E1', '#302B3F')
    'TrackHover'             = @('#E3DCD6', '#3A344C')
    'Thumb'                  = @('#FFFFFF', '#F2EFF7')

    'Primary'                = @('#F497A9', '#F6A2B3')
    'PrimaryHover'           = @('#F7ABB9', '#F9B7C4')
    'PrimaryPressed'         = @('#E58296', '#E4899C')
    'PrimarySoft'            = @('#FDE7EC', '#3A2532')
    'PrimarySoftHover'       = @('#FBD5DD', '#4A2D3C')
    'PrimaryText'            = @('#B4405C', '#F6A2B3')
    'OnPrimary'              = @('#4A2333', '#3A1A26')

    'Secondary'              = @('#B8A9E6', '#C4B5FD')
    'SecondarySoft'          = @('#EEEAFA', '#2C2840')
    'SecondarySoftHover'     = @('#E2DBF7', '#363050')
    'SecondarySoftPressed'   = @('#D2C8F1', '#40395E')
    'SecondaryText'          = @('#4B3F7A', '#D6CCFE')

    'Tertiary'               = @('#9FD8CB', '#A7E3D5')
    'TertiarySoft'           = @('#E3F4F0', '#1F3332')
    'TertiaryText'           = @('#2F6458', '#B9EADF')

    'Mint'                   = @('#9FD8CB', '#A7E3D5')
    'MintSoft'               = @('#E3F4F0', '#1F3332')
    'MintSoftHover'          = @('#CFECE5', '#26403E')
    'MintSoftPressed'        = @('#B9E3D9', '#2E4C49')
    'MintText'               = @('#2F6458', '#B9EADF')

    'Butter'                 = @('#F8DE9B', '#F9D697')
    'Sky'                    = @('#A9D3F0', '#A3D1F7')
    'Peach'                  = @('#F9C3A5', '#F7C2A6')
    'Danger'                 = @('#F28B82', '#F5A0A8')
    'DangerSoft'             = @('#FDE4E2', '#382229')
    'DangerText'             = @('#9E3B35', '#F8BAC0')

    'TextPrimary'            = @('#2E2B36', '#F2EFF7')
    'TextSecondary'          = @('#787385', '#A6A0B5')
    'TextTertiary'           = @('#A8A3B3', '#7C768C')
    'TextDisabled'           = @('#B8B3C2', '#5E596E')

    'HoverTint'              = @('#0F2E2B36', '#14FFFFFF')
    'PressTint'              = @('#1C2E2B36', '#24FFFFFF')
    'FocusRing'              = @('#73F497A9', '#80F6A2B3')
    'ScrollThumb'            = @('#D9D1CA', '#3F3953')
    'ScrollThumbHover'       = @('#C4BAB1', '#554D6B')
    'SwatchOutline'          = @('#1A000000', '#33FFFFFF')
    'Shimmer'                = @('#B3FFFFFF', '#1FFFFFFF')

    'CaptionCloseHover'      = @('#F7B3AE', '#6B3440')
    'CaptionCloseGlyph'      = @('#9E3B35', '#F8BAC0')

    'TooltipBackground'      = @('#2E2B36', '#F2EFF7')
    'TooltipForeground'      = @('#FAF8F6', '#221F2E')

    'Info'                   = @('#98C9F2', '#A3D1F7')
    'InfoSoft'               = @('#E8F3FC', '#222E3D')
    'InfoText'               = @('#2D5E87', '#BFE0FA')
    'Success'                = @('#9FD8CB', '#A7E3D5')
    'SuccessSoft'            = @('#E3F4F0', '#1F3332')
    'SuccessText'            = @('#2F6458', '#BDEBE0')
    'Warning'                = @('#F7CF8B', '#F9D697')
    'WarningSoft'            = @('#FEF3E0', '#362F24')
    'WarningText'            = @('#875713', '#FBE3B5')
    'Error'                  = @('#F2949C', '#F5A0A8')
    'ErrorSoft'              = @('#FDE7E9', '#382229')
    'ErrorText'              = @('#9E3B45', '#F8BAC0')
    'OnStatus'               = @('#2E2B36', '#1E1A28')

    'PrimaryWash'            = @('#FFF5F7', '#2A2231')
    'SecondaryWash'          = @('#F7F5FD', '#25223A')
    'MintWash'               = @('#F2FAF8', '#1F2A2E')

    'Backdrop'               = @('#402E2B36', '#990E0C14')
    'BackdropCanvas'         = @('#B3FAF8F6', '#99181622')
    'Shadow'                 = @('#4B2E3B', '#000000')
}

# name, blur, depth, light opacity, dark opacity
$shadows = @(
    @('Subtle',   12, 2,  0.08, 0.35),
    @('Soft',     16, 4,  0.09, 0.40),
    @('Raised',   20, 6,  0.12, 0.45),
    @('Floating', 24, 10, 0.15, 0.50)
)

# ---------------------------------------------------------------------------------------------------------------
# Accents
# ---------------------------------------------------------------------------------------------------------------
# The keys an accent overlay redefines, in output order.
$accentKeys = @(
    'Primary', 'PrimaryHover', 'PrimaryPressed', 'PrimarySoft', 'PrimarySoftHover', 'PrimaryText', 'OnPrimary', 'FocusRing', 'PrimaryWash',
    'Secondary', 'SecondarySoft', 'SecondarySoftHover', 'SecondarySoftPressed', 'SecondaryText', 'SecondaryWash',
    'Tertiary', 'TertiarySoft', 'TertiaryText'
)

$surface = @('#FFFFFF', '#221F2E')

# Supporting families that rotate so the secondary / tertiary roles never share the primary's hue.
$secondaryFamilies = @{
    # Secondary, SecondarySoft, SecondarySoftHover, SecondarySoftPressed, SecondaryText, SecondaryWash
    'Lavender' = @(
        @('#B8A9E6', '#EEEAFA', '#E2DBF7', '#D2C8F1', '#4B3F7A', '#F7F5FD'),
        @('#C4B5FD', '#2C2840', '#363050', '#40395E', '#D6CCFE', '#25223A'))
    'Blush'    = @(
        @('#F497A9', '#FDE7EC', '#FBD5DD', '#F8C3CF', '#B4405C', '#FFF5F7'),
        @('#F6A2B3', '#3A2532', '#4A2D3C', '#563443', '#F8BAC8', '#2A2231'))
}
$tertiaryFamilies = @{
    # Tertiary, TertiarySoft, TertiaryText
    'Mint'  = @(@('#9FD8CB', '#E3F4F0', '#2F6458'), @('#A7E3D5', '#1F3332', '#B9EADF'))
    'Peach' = @(@('#F9C3A5', '#FDEDE3', '#9A4E25'), @('#F7C2A6', '#3A2A24', '#FAD3BE'))
}

# Primary ramps. Base / Hover / Pressed / SoftFill (ARGB, flattened onto the surface) / OnPrimary come from the design
# brief; PrimaryText is the accent darkened (light) or lifted (dark) for readable text on soft fills and surfaces.
# Coral keeps the original palette values exactly so existing apps look identical.
$accents = [ordered]@{
    'Coral' = @{
        Description = 'Toe-Bean Blush (default)'
        Secondary = 'Lavender'; Tertiary = 'Mint'
        Exact = @(
            @{ Primary = '#F497A9'; PrimaryHover = '#F7ABB9'; PrimaryPressed = '#E58296'; PrimarySoft = '#FDE7EC'; PrimarySoftHover = '#FBD5DD'
               PrimaryText = '#B4405C'; OnPrimary = '#4A2333'; FocusRing = '#73F497A9'; PrimaryWash = '#FFF5F7' },
            @{ Primary = '#F6A2B3'; PrimaryHover = '#F9B7C4'; PrimaryPressed = '#E4899C'; PrimarySoft = '#3A2532'; PrimarySoftHover = '#4A2D3C'
               PrimaryText = '#F6A2B3'; OnPrimary = '#3A1A26'; FocusRing = '#80F6A2B3'; PrimaryWash = '#2A2231' })
    }
    'Lavender' = @{
        Description = 'Soft Wisteria'
        Secondary = 'Blush'; Tertiary = 'Mint'
        Light = @{ Base = '#A896E6'; Hover = '#B9AAEE'; Pressed = '#927FD6'; Soft = '#26A896E6'; On = '#FFFFFF'; Text = '#5B48A8' }
        Dark  = @{ Base = '#B8A9F2'; Hover = '#C9BDF7'; Pressed = '#A18FE3'; Soft = '#33B8A9F2'; On = '#181622'; Text = '#C9BDF7' }
    }
    'Mint' = @{
        Description = 'Pastel Sage'
        Secondary = 'Lavender'; Tertiary = 'Peach'
        Light = @{ Base = '#7CCBB9'; Hover = '#92D6C6'; Pressed = '#65B6A4'; Soft = '#267CCBB9'; On = '#1E332E'; Text = '#2B6658' }
        Dark  = @{ Base = '#9FD8CB'; Hover = '#B5E3D8'; Pressed = '#85C4B6'; Soft = '#339FD8CB'; On = '#181622'; Text = '#B5E3D8' }
    }
    'Peach' = @{
        Description = 'Warm Apricot'
        Secondary = 'Lavender'; Tertiary = 'Mint'
        Light = @{ Base = '#F5A67E'; Hover = '#F8B897'; Pressed = '#E39066'; Soft = '#26F5A67E'; On = '#FFFFFF'; Text = '#A3512A' }
        Dark  = @{ Base = '#F7B492'; Hover = '#FAC6AB'; Pressed = '#E59E7A'; Soft = '#33F7B492'; On = '#181622'; Text = '#FAC6AB' }
    }
}

function ConvertTo-Rgb([string]$hex) {
    $h = $hex.TrimStart('#')
    if ($h.Length -eq 8) { $h = $h.Substring(2) }
    return @([Convert]::ToInt32($h.Substring(0, 2), 16), [Convert]::ToInt32($h.Substring(2, 2), 16), [Convert]::ToInt32($h.Substring(4, 2), 16))
}

# Paints $hex at $alpha over $under and returns the opaque result.
function Get-Over([string]$hex, [double]$alpha, [string]$under) {
    $a = ConvertTo-Rgb $hex
    $b = ConvertTo-Rgb $under
    $parts = for ($i = 0; $i -lt 3; $i++) { '{0:X2}' -f [int][Math]::Round($b[$i] + (($a[$i] - $b[$i]) * $alpha)) }
    return '#' + ($parts -join '')
}

function Get-Alpha([string]$argb) {
    return [Convert]::ToInt32($argb.TrimStart('#').Substring(0, 2), 16) / 255.0
}

function Get-AccentValues([string]$name, [int]$index) {
    $accent = $accents[$name]
    $values = [ordered]@{}
    if ($accent.Exact) {
        foreach ($k in $accent.Exact[$index].Keys) { $values[$k] = $accent.Exact[$index][$k] }
    }
    else {
        $spec = if ($index -eq 0) { $accent.Light } else { $accent.Dark }
        $under = $surface[$index]
        $softAlpha = Get-Alpha $spec.Soft
        $values['Primary'] = $spec.Base
        $values['PrimaryHover'] = $spec.Hover
        $values['PrimaryPressed'] = $spec.Pressed
        $values['PrimarySoft'] = Get-Over $spec.Base $softAlpha $under
        $values['PrimarySoftHover'] = Get-Over $spec.Base ($softAlpha + 0.10) $under
        $values['PrimaryText'] = $spec.Text
        $values['OnPrimary'] = $spec.On
        $values['FocusRing'] = (@('#73', '#80')[$index]) + $spec.Base.TrimStart('#')
        $values['PrimaryWash'] = Get-Over $spec.Base (@(0.06, 0.05)[$index]) $under
    }
    $sec = $secondaryFamilies[$accent.Secondary][$index]
    $names = @('Secondary', 'SecondarySoft', 'SecondarySoftHover', 'SecondarySoftPressed', 'SecondaryText', 'SecondaryWash')
    for ($i = 0; $i -lt $names.Count; $i++) { $values[$names[$i]] = $sec[$i] }
    $ter = $tertiaryFamilies[$accent.Tertiary][$index]
    $names = @('Tertiary', 'TertiarySoft', 'TertiaryText')
    for ($i = 0; $i -lt $names.Count; $i++) { $values[$names[$i]] = $ter[$i] }
    return $values
}

function Save-Xaml([string]$path, [System.Text.StringBuilder]$sb) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { $null = New-Item -ItemType Directory -Path $dir }
    [IO.File]::WriteAllText($path, $sb.ToString().Replace("`r`n", "`n").Replace("`n", "`r`n"), [Text.UTF8Encoding]::new($false))
}

function Write-Header([System.Text.StringBuilder]$sb, [string[]]$comment) {
    $null = $sb.AppendLine('<!--')
    foreach ($line in $comment) { $null = $sb.AppendLine("    $line".TrimEnd()) }
    $null = $sb.AppendLine('-->')
    $null = $sb.AppendLine('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
    $null = $sb.AppendLine('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"')
    $null = $sb.AppendLine('                    xmlns:sys="clr-namespace:System;assembly=mscorlib"')
    $null = $sb.AppendLine('                    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"')
    $null = $sb.AppendLine('                    xmlns:po="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"')
    $null = $sb.AppendLine('                    mc:Ignorable="po">')
    $null = $sb.AppendLine('')
}

function Write-Palette([int]$index, [string]$name, [string]$file, [string]$description) {
    $sb = [System.Text.StringBuilder]::new()
    Write-Header $sb @(
        $description, '',
        'A Muzzle palette: semantic colours (Muzzle.Color.*), brushes (Muzzle.Brush.*) and elevation shadows',
        '(Muzzle.Shadow.*). Muzzle.Light.xaml and Muzzle.Dark.xaml define exactly the same keys; MuzzleThemeManager',
        'swaps one for the other at runtime, and every control picks the change up through DynamicResource.',
        'The Primary / Secondary / Tertiary keys hold the Coral accent; an accent overlay merged after the palette',
        '(Themes/Accents) redefines them. Generated by tools/Generate-Palettes.ps1: edit the table there, not this file.')
    $null = $sb.AppendLine("    <sys:String x:Key=`"Muzzle.ThemeName`">$name</sys:String>")
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Colours -->')
    foreach ($k in $tokens.Keys) { $null = $sb.AppendLine("    <Color x:Key=`"Muzzle.Color.$k`">$($tokens[$k][$index])</Color>") }
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Brushes -->')
    foreach ($k in $tokens.Keys) {
        if ($k -eq 'SurfaceRimHighlight') { continue }
        $null = $sb.AppendLine("    <SolidColorBrush x:Key=`"Muzzle.Brush.$k`" Color=`"{StaticResource Muzzle.Color.$k}`" po:Freeze=`"True`" />")
    }
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Elevated-surface border: a 1px rim highlight on the top edge fading into the normal border. -->')
    $null = $sb.AppendLine('    <LinearGradientBrush x:Key="Muzzle.Brush.SurfaceRim" MappingMode="Absolute" StartPoint="0,0" EndPoint="0,3" po:Freeze="True">')
    $null = $sb.AppendLine('        <GradientStop Offset="0" Color="{StaticResource Muzzle.Color.SurfaceRimHighlight}" />')
    $null = $sb.AppendLine('        <GradientStop Offset="0.34" Color="{StaticResource Muzzle.Color.SurfaceRimHighlight}" />')
    $null = $sb.AppendLine('        <GradientStop Offset="1" Color="{StaticResource Muzzle.Color.SurfaceBorder}" />')
    $null = $sb.AppendLine('    </LinearGradientBrush>')
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Transparency checkerboard (colour pickers, alpha previews). -->')
    $null = $sb.AppendLine('    <DrawingBrush x:Key="Muzzle.Brush.Checker" TileMode="Tile" Viewport="0,0,10,10" ViewportUnits="Absolute" po:Freeze="True">')
    $null = $sb.AppendLine('        <DrawingBrush.Drawing>')
    $null = $sb.AppendLine('            <DrawingGroup>')
    $null = $sb.AppendLine('                <GeometryDrawing Brush="{StaticResource Muzzle.Brush.Surface}" Geometry="M 0,0 H 10 V 10 H 0 Z" />')
    $null = $sb.AppendLine('                <GeometryDrawing Brush="{StaticResource Muzzle.Brush.SurfaceBorder}" Geometry="M 0,0 H 5 V 5 H 0 Z M 5,5 H 10 V 10 H 5 Z" />')
    $null = $sb.AppendLine('            </DrawingGroup>')
    $null = $sb.AppendLine('        </DrawingBrush.Drawing>')
    $null = $sb.AppendLine('    </DrawingBrush>')
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Elevation shadows -->')
    foreach ($s in $shadows) {
        $opacity = if ($index -eq 0) { $s[3] } else { $s[4] }
        $null = $sb.AppendLine("    <DropShadowEffect x:Key=`"Muzzle.Shadow.$($s[0])`" BlurRadius=`"$($s[1])`" ShadowDepth=`"$($s[2])`" Direction=`"270`" Opacity=`"$opacity`"")
        $null = $sb.AppendLine("                      Color=`"{StaticResource Muzzle.Color.Shadow}`" RenderingBias=`"Performance`" po:Freeze=`"True`" />")
    }
    $null = $sb.AppendLine('')
    $null = $sb.Append('</ResourceDictionary>')
    Save-Xaml (Join-Path $ThemesDir $file) $sb
}

function Write-Accent([string]$name, [int]$index) {
    $variant = @('Light', 'Dark')[$index]
    $accent = $accents[$name]
    $values = Get-AccentValues $name $index
    $sb = [System.Text.StringBuilder]::new()
    Write-Header $sb @(
        "Muzzle accent: $name ($($accent.Description)), $variant variant.", '',
        'Merged after Muzzle.Light.xaml / Muzzle.Dark.xaml (MuzzleThemeManager keeps it there), it redefines the',
        "primary ramp plus the secondary ($($accent.Secondary)) and tertiary ($($accent.Tertiary)) roles so they never share the",
        'primary hue. Generated by tools/Generate-Palettes.ps1: edit the table there, not this file.')
    $null = $sb.AppendLine('    <sys:Boolean x:Key="Muzzle.AccentSlot">True</sys:Boolean>')
    $null = $sb.AppendLine("    <sys:String x:Key=`"Muzzle.AccentName`">$name</sys:String>")
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Colours -->')
    foreach ($k in $accentKeys) { $null = $sb.AppendLine("    <Color x:Key=`"Muzzle.Color.$k`">$($values[$k])</Color>") }
    $null = $sb.AppendLine('')
    $null = $sb.AppendLine('    <!-- Brushes -->')
    foreach ($k in $accentKeys) {
        $null = $sb.AppendLine("    <SolidColorBrush x:Key=`"Muzzle.Brush.$k`" Color=`"{StaticResource Muzzle.Color.$k}`" po:Freeze=`"True`" />")
    }
    $null = $sb.AppendLine('')
    $null = $sb.Append('</ResourceDictionary>')
    Save-Xaml (Join-Path $ThemesDir "Accents\Muzzle.Accent.$name.$variant.xaml") $sb
}

Write-Palette 0 'Light' 'Muzzle.Light.xaml' 'Muzzle Light: the cozy pastel light theme (warm cream canvas, white surfaces, toe-bean blush accents).'
Write-Palette 1 'Dark'  'Muzzle.Dark.xaml'  'Muzzle Dark ("Pastel Twilight"): deep plum-charcoal canvas, soft slate-indigo surfaces and luminous pastel accents, with deeper ambient shadows and rim-lit card edges.'
foreach ($name in $accents.Keys) {
    Write-Accent $name 0
    Write-Accent $name 1
}
"generated 2 palettes ($($tokens.Count) colour tokens) and $($accents.Count * 2) accent dictionaries ($($accentKeys.Count) tokens each)"