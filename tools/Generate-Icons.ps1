# Generates Muzzle's built-in icon pack from one table:
#
#   src/Muzzle.Theme/Themes/Icons/MuzzleIcons.xaml     Geometry resources "Muzzle.Icon.<Kind>" (24 x 24 grid, stroke-drawn)
#   src/Muzzle.Theme/Controls/MuzzleIconKind.cs        the matching enum
#
# Icons are outlines drawn with round caps and joins by MuzzleIcon (stroke 1.8 at 24 px). Run: pwsh tools/Generate-Icons.ps1
param([string]$Root = (Join-Path $PSScriptRoot '..\src\Muzzle.Theme'))

$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture

function N([double]$v) { return ([Math]::Round($v, 2)).ToString($inv) }

# Circle as two arcs.
function C([double]$cx, [double]$cy, [double]$r) {
    return "M $(N ($cx - $r)),$(N $cy) A $(N $r),$(N $r) 0 1 0 $(N ($cx + $r)),$(N $cy) A $(N $r),$(N $r) 0 1 0 $(N ($cx - $r)),$(N $cy) Z"
}

# A solid-looking dot (tiny circle thickened by the stroke).
function D([double]$x, [double]$y) { return C $x $y 0.7 }

# Rounded rectangle.
function RR([double]$x, [double]$y, [double]$w, [double]$h, [double]$r) {
    $x2 = $x + $w; $y2 = $y + $h
    return "M $(N ($x + $r)),$(N $y) H $(N ($x2 - $r)) A $(N $r),$(N $r) 0 0 1 $(N $x2),$(N ($y + $r)) V $(N ($y2 - $r)) A $(N $r),$(N $r) 0 0 1 $(N ($x2 - $r)),$(N $y2) H $(N ($x + $r)) A $(N $r),$(N $r) 0 0 1 $(N $x),$(N ($y2 - $r)) V $(N ($y + $r)) A $(N $r),$(N $r) 0 0 1 $(N ($x + $r)),$(N $y) Z"
}

# Radial strokes: $count lines from radius $r1 to $r2 around (cx, cy), starting at $start degrees.
function Rays([double]$cx, [double]$cy, [double]$r1, [double]$r2, [int]$count, [double]$start = 0) {
    $parts = for ($i = 0; $i -lt $count; $i++) {
        $a = ($start + (360.0 / $count) * $i) * [Math]::PI / 180
        "M $(N ($cx + $r1 * [Math]::Cos($a))),$(N ($cy + $r1 * [Math]::Sin($a))) L $(N ($cx + $r2 * [Math]::Cos($a))),$(N ($cy + $r2 * [Math]::Sin($a)))"
    }
    return $parts -join ' '
}

# n-pointed star.
function StarPath([double]$cx, [double]$cy, [double]$outer, [double]$inner, [int]$points) {
    $coords = for ($i = 0; $i -lt ($points * 2); $i++) {
        $r = if ($i % 2 -eq 0) { $outer } else { $inner }
        $a = (-90 + (180.0 / $points) * $i) * [Math]::PI / 180
        "$(N ($cx + $r * [Math]::Cos($a))),$(N ($cy + $r * [Math]::Sin($a)))"
    }
    return "M " + ($coords -join ' L ') + ' Z'
}

function Ring([double]$cx, [double]$cy, [double]$radius, [double]$r, [int]$count) {
    $parts = for ($i = 0; $i -lt $count; $i++) {
        $a = (-90 + (360.0 / $count) * $i) * [Math]::PI / 180
        C ($cx + $radius * [Math]::Cos($a)) ($cy + $radius * [Math]::Sin($a)) $r
    }
    return $parts -join ' '
}

$speaker = 'M 4,9.5 H 7.5 L 12,5.5 V 18.5 L 7.5,14.5 H 4 Z'
$file = 'M 7,3.5 H 13.5 L 18.5,8.5 V 19 A 1.5,1.5 0 0 1 17,20.5 H 7 A 1.5,1.5 0 0 1 5.5,19 V 5 A 1.5,1.5 0 0 1 7,3.5 Z M 13.5,3.5 V 8.5 H 18.5'
$cloud = 'M 7,18.5 A 4,4 0 0 1 6.6,10.5 A 5.5,5.5 0 0 1 17.2,9 A 4.75,4.75 0 0 1 17.5,18.5 Z'
$eye = 'M 2.5,12 C 5,7 8.5,5 12,5 C 15.5,5 19,7 21.5,12 C 19,17 15.5,19 12,19 C 8.5,19 5,17 2.5,12 Z ' + (C 12 12 3)
$bell = 'M 6,16 V 10.5 A 6,6 0 0 1 18,10.5 V 16 L 19.5,17.5 H 4.5 Z M 10,20 A 2,2 0 0 0 14,20'
$lockBody = RR 5 10.5 14 10 2

$groups = [ordered]@{
    'Navigation' = [ordered]@{
        'Home'           = 'M 4,11 L 12,4 L 20,11 M 6,9.5 V 19 A 1,1 0 0 0 7,20 H 17 A 1,1 0 0 0 18,19 V 9.5 M 10,20 V 14.5 H 14 V 20'
        'Menu'           = 'M 4,7 H 20 M 4,12 H 20 M 4,17 H 20'
        'Search'         = (C 11 11 6.5) + ' M 16,16 L 20,20'
        'Settings'       = (C 12 12 6.2) + ' ' + (Rays 12 12 6.2 8.8 8 22.5) + ' ' + (C 12 12 2.4)
        'ChevronLeft'    = 'M 14.5,6 L 8.5,12 L 14.5,18'
        'ChevronRight'   = 'M 9.5,6 L 15.5,12 L 9.5,18'
        'ChevronUp'      = 'M 6,14.5 L 12,8.5 L 18,14.5'
        'ChevronDown'    = 'M 6,9.5 L 12,15.5 L 18,9.5'
        'ArrowLeft'      = 'M 19,12 H 5 M 11,6 L 5,12 L 11,18'
        'ArrowRight'     = 'M 5,12 H 19 M 13,6 L 19,12 L 13,18'
        'ArrowUp'        = 'M 12,19 V 5 M 6,11 L 12,5 L 18,11'
        'ArrowDown'      = 'M 12,5 V 19 M 6,13 L 12,19 L 18,13'
        'ExternalLink'   = 'M 14,4 H 20 V 10 M 20,4 L 11,13 M 18,14 V 18.5 A 1.5,1.5 0 0 1 16.5,20 H 5.5 A 1.5,1.5 0 0 1 4,18.5 V 7.5 A 1.5,1.5 0 0 1 5.5,6 H 10'
        'MoreHorizontal' = (D 6 12) + ' ' + (D 12 12) + ' ' + (D 18 12)
        'MoreVertical'   = (D 12 6) + ' ' + (D 12 12) + ' ' + (D 12 18)
        'Apps'           = (RR 4 4 6.5 6.5 1.5) + ' ' + (RR 13.5 4 6.5 6.5 1.5) + ' ' + (RR 4 13.5 6.5 6.5 1.5) + ' ' + (RR 13.5 13.5 6.5 6.5 1.5)
        'List'           = (D 5 7) + ' ' + (D 5 12) + ' ' + (D 5 17) + ' M 9,7 H 20 M 9,12 H 20 M 9,17 H 20'
        'Sidebar'        = (RR 3 4 18 16 2.5) + ' M 9,4 V 20'
        'Compass'        = (C 12 12 8.5) + ' M 15.5,8.5 L 13.6,13.6 L 8.5,15.5 L 10.4,10.4 Z'
        'Map'            = 'M 3.5,6 L 9,4 L 15,6 L 20.5,4 V 18 L 15,20 L 9,18 L 3.5,20 Z M 9,4 V 18 M 15,6 V 20'
    }
    'Actions' = [ordered]@{
        'Plus'           = 'M 12,5 V 19 M 5,12 H 19'
        'Minus'          = 'M 5,12 H 19'
        'Close'          = 'M 6,6 L 18,18 M 18,6 L 6,18'
        'Check'          = 'M 5,12.5 L 10,17.5 L 19,7'
        'Edit'           = 'M 4,20 L 4.8,15.6 L 15.6,4.8 A 2.2,2.2 0 0 1 18.8,4.8 L 19.2,5.2 A 2.2,2.2 0 0 1 19.2,8.4 L 8.4,19.2 Z M 13.6,6.8 L 17.2,10.4'
        'Trash'          = 'M 4,7 H 20 M 9,7 V 5 A 1,1 0 0 1 10,4 H 14 A 1,1 0 0 1 15,5 V 7 M 6,7 L 7,19 A 1.5,1.5 0 0 0 8.5,20.4 H 15.5 A 1.5,1.5 0 0 0 17,19 L 18,7 M 10,11 V 16.5 M 14,11 V 16.5'
        'Copy'           = (RR 8 8 12 12 2) + ' M 16,8 V 6 A 2,2 0 0 0 14,4 H 6 A 2,2 0 0 0 4,6 V 14 A 2,2 0 0 0 6,16 H 8'
        'Clipboard'      = 'M 9,5 H 7 A 2,2 0 0 0 5,7 V 19 A 2,2 0 0 0 7,21 H 17 A 2,2 0 0 0 19,19 V 7 A 2,2 0 0 0 17,5 H 15 ' + (RR 9 3 6 4 1.2)
        'Save'           = 'M 5.5,4 H 16 L 20,8 V 18.5 A 1.5,1.5 0 0 1 18.5,20 H 5.5 A 1.5,1.5 0 0 1 4,18.5 V 5.5 A 1.5,1.5 0 0 1 5.5,4 Z M 8,4 V 8.5 H 15 V 4 M 7.5,20 V 14 H 16.5 V 20'
        'Download'       = 'M 12,4 V 15 M 7,10.5 L 12,15.5 L 17,10.5 M 5,19.5 H 19'
        'Upload'         = 'M 12,16 V 5 M 7,9.5 L 12,4.5 L 17,9.5 M 5,19.5 H 19'
        'Refresh'        = 'M 19,12 A 7,7 0 1 1 16.95,7.05 M 17.5,3.5 V 7.5 H 13.5'
        'Undo'           = 'M 9,6 L 4.5,10.5 L 9,15 M 4.5,10.5 H 14 A 5,5 0 0 1 14,20.5 H 11'
        'Redo'           = 'M 15,6 L 19.5,10.5 L 15,15 M 19.5,10.5 H 10 A 5,5 0 0 0 10,20.5 H 13'
        'Share'          = (C 18 5.5 2.5) + ' ' + (C 6 12 2.5) + ' ' + (C 18 18.5 2.5) + ' M 8.2,10.8 L 15.8,6.7 M 8.2,13.2 L 15.8,17.3'
        'Link'           = 'M 10,14 A 3.5,3.5 0 0 0 15,14 L 18.5,10.5 A 3.5,3.5 0 0 0 13.5,5.5 L 12.5,6.5 M 14,10 A 3.5,3.5 0 0 0 9,10 L 5.5,13.5 A 3.5,3.5 0 0 0 10.5,18.5 L 11.5,17.5'
        'Filter'         = 'M 4,5 H 20 L 14,12.5 V 19 L 10,17 V 12.5 Z'
        'Sort'           = 'M 7,4 V 20 M 3.5,16.5 L 7,20 L 10.5,16.5 M 17,20 V 4 M 13.5,7.5 L 17,4 L 20.5,7.5'
        'Lock'           = $lockBody + ' M 8,10.5 V 7.5 A 4,4 0 0 1 16,7.5 V 10.5 M 12,14.5 V 16.5'
        'Unlock'         = $lockBody + ' M 8,10.5 V 7.5 A 4,4 0 0 1 15.8,6.3 M 12,14.5 V 16.5'
        'Eye'            = $eye
        'EyeOff'         = $eye + ' M 4,4 L 20,20'
        'Pin'            = 'M 9,4 H 15 M 10,4 V 9.5 L 7,13.5 H 17 L 14,9.5 V 4 M 12,13.5 V 20'
        'Star'           = StarPath 12 12.6 8.8 3.9 5
        'Heart'          = 'M 12,20 C 12,20 3.5,15 3.5,9 A 4.25,4.25 0 0 1 12,6.75 A 4.25,4.25 0 0 1 20.5,9 C 20.5,15 12,20 12,20 Z'
        'Bookmark'       = 'M 6.5,4 H 17.5 V 20 L 12,16 L 6.5,20 Z'
        'Flag'           = 'M 5,21 V 4 M 5,4.5 H 17 L 14.5,8.5 L 17,12.5 H 5'
        'Tag'            = 'M 3.5,12.2 V 4.8 A 1.3,1.3 0 0 1 4.8,3.5 H 12.2 L 20.5,11.8 A 1.3,1.3 0 0 1 20.5,13.6 L 13.6,20.5 A 1.3,1.3 0 0 1 11.8,20.5 Z ' + (C 8 8 1.2)
        'Send'           = 'M 20.5,3.5 L 10.5,13.5 M 20.5,3.5 L 14,20.5 L 10.5,13.5 L 3.5,10 Z'
        'Reply'          = 'M 10,5 L 4,11 L 10,17 M 4,11 H 13 A 7,7 0 0 1 20,18 V 19'
        'LogOut'         = 'M 10,4 H 6 A 2,2 0 0 0 4,6 V 18 A 2,2 0 0 0 6,20 H 10 M 15,7.5 L 19.5,12 L 15,16.5 M 19.5,12 H 9.5'
        'LogIn'          = 'M 14,4 H 18 A 2,2 0 0 1 20,6 V 18 A 2,2 0 0 1 18,20 H 14 M 10,7.5 L 14.5,12 L 10,16.5 M 14.5,12 H 4.5'
        'ZoomIn'         = (C 11 11 6.5) + ' M 16,16 L 20,20 M 11,8 V 14 M 8,11 H 14'
        'ZoomOut'        = (C 11 11 6.5) + ' M 16,16 L 20,20 M 8,11 H 14'
        'Expand'         = 'M 14,4 H 20 V 10 M 20,4 L 14,10 M 10,20 H 4 V 14 M 4,20 L 10,14'
        'Collapse'       = 'M 20,4 L 14,10 M 14,5.5 V 10 H 18.5 M 4,20 L 10,14 M 5.5,14 H 10 V 18.5'
        'Print'          = 'M 7,9 V 4 H 17 V 9 M 7,17 H 5.5 A 1.5,1.5 0 0 1 4,15.5 V 10.5 A 1.5,1.5 0 0 1 5.5,9 H 18.5 A 1.5,1.5 0 0 1 20,10.5 V 15.5 A 1.5,1.5 0 0 1 18.5,17 H 17 M 7,14 H 17 V 20 H 7 Z'
        'Cut'            = (C 6.5 17.5 2.5) + ' ' + (C 17.5 17.5 2.5) + ' M 8.3,15.7 L 18,4 M 15.7,15.7 L 6,4'
        'Archive'        = (RR 3.5 4 17 4.5 1.2) + ' M 5,8.5 V 18.5 A 1.5,1.5 0 0 0 6.5,20 H 17.5 A 1.5,1.5 0 0 0 19,18.5 V 8.5 M 10,12.5 H 14'
        'Sliders'        = 'M 4,7 H 14 M 18,7 H 20 M 4,17 H 6 M 10,17 H 20 ' + (C 16 7 2) + ' ' + (C 8 17 2)
        'Zap'            = 'M 13,3 L 5,13.5 H 11.5 L 10.5,21 L 19,10 H 12.5 Z'
    }
    'Media' = [ordered]@{
        'Play'           = 'M 7.5,5 V 19 L 19,12 Z'
        'Pause'          = 'M 8,5 V 19 M 16,5 V 19'
        'Stop'           = RR 6 6 12 12 2
        'SkipNext'       = 'M 6,5.5 V 18.5 L 15.5,12 Z M 18.5,5.5 V 18.5'
        'SkipPrevious'   = 'M 18,5.5 V 18.5 L 8.5,12 Z M 5.5,5.5 V 18.5'
        'Repeat'         = 'M 17,3 L 20,6 L 17,9 M 4,11.5 V 10 A 4,4 0 0 1 8,6 H 20 M 7,21 L 4,18 L 7,15 M 20,12.5 V 14 A 4,4 0 0 1 16,18 H 4'
        'Shuffle'        = 'M 16.5,3.5 H 20.5 V 7.5 M 4,20 L 20.5,3.5 M 20.5,16.5 V 20.5 H 16.5 M 14.5,14.5 L 20.5,20.5 M 4,4 L 9,9'
        'VolumeUp'       = $speaker + ' M 15.5,9 A 4,4 0 0 1 15.5,15 M 18,6.5 A 7.5,7.5 0 0 1 18,17.5'
        'VolumeMute'     = $speaker + ' M 15.5,9.5 L 20.5,14.5 M 20.5,9.5 L 15.5,14.5'
        'Mic'            = (RR 9 3.5 6 11 3) + ' M 5.5,11 A 6.5,6.5 0 0 0 18.5,11 M 12,17.5 V 20.5 M 9,20.5 H 15'
        'Camera'         = 'M 4,8 A 1.5,1.5 0 0 1 5.5,6.5 H 8 L 9.5,4.5 H 14.5 L 16,6.5 H 18.5 A 1.5,1.5 0 0 1 20,8 V 17.5 A 1.5,1.5 0 0 1 18.5,19 H 5.5 A 1.5,1.5 0 0 1 4,17.5 Z ' + (C 12 12.5 3.5)
        'Image'          = (RR 3.5 4.5 17 15 2) + ' ' + (C 9 9.5 1.6) + ' M 20.5,15 L 15.5,10 L 6,19.5'
        'Video'          = (RR 3 6.5 12.5 11 2) + ' M 15.5,10.5 L 21,7.5 V 16.5 L 15.5,13.5'
        'Music'          = 'M 9,18 V 5.5 L 19,3.5 V 16 ' + (C 6.5 18 2.5) + ' ' + (C 16.5 16 2.5)
        'Headphones'     = 'M 4,17 V 12 A 8,8 0 0 1 20,12 V 17 ' + (RR 3.5 13 4 7 1.5) + ' ' + (RR 16.5 13 4 7 1.5)
    }
    'Files' = [ordered]@{
        'File'           = $file
        'FileText'       = $file + ' M 8.5,12.5 H 15.5 M 8.5,16 H 13.5'
        'Folder'         = 'M 3.5,7 A 1.5,1.5 0 0 1 5,5.5 H 9.5 L 11.5,7.5 H 19 A 1.5,1.5 0 0 1 20.5,9 V 17.5 A 1.5,1.5 0 0 1 19,19 H 5 A 1.5,1.5 0 0 1 3.5,17.5 Z'
        'FolderOpen'     = 'M 3.5,17.5 V 7 A 1.5,1.5 0 0 1 5,5.5 H 9.5 L 11.5,7.5 H 17.5 A 1.5,1.5 0 0 1 19,9 V 10.5 M 3.5,17.5 L 6,11.5 A 1.5,1.5 0 0 1 7.4,10.5 H 20 A 1,1 0 0 1 20.9,11.9 L 18.6,18 A 1.5,1.5 0 0 1 17.2,19 H 5 A 1.5,1.5 0 0 1 3.5,17.5 Z'
        'Paperclip'      = 'M 19.5,11.5 L 12.3,18.7 A 4.5,4.5 0 0 1 5.9,12.3 L 13.1,5.1 A 3,3 0 0 1 17.3,9.3 L 10.1,16.5 A 1.5,1.5 0 0 1 8,14.4 L 14.6,7.8'
        'Cloud'          = $cloud
        'CloudUpload'    = $cloud + ' M 12,16.5 V 11.5 M 9.8,13.7 L 12,11.5 L 14.2,13.7'
        'Database'       = 'M 4.5,6 A 7.5,2.8 0 1 0 19.5,6 A 7.5,2.8 0 1 0 4.5,6 Z M 4.5,6 V 18 A 7.5,2.8 0 0 0 19.5,18 V 6 M 4.5,12 A 7.5,2.8 0 0 0 19.5,12'
        'Code'           = 'M 8.5,7 L 3.5,12 L 8.5,17 M 15.5,7 L 20.5,12 L 15.5,17 M 13.5,5 L 10.5,19'
        'Terminal'       = (RR 3 4.5 18 15 2) + ' M 7,9.5 L 10,12 L 7,14.5 M 12,15 H 17'
    }
    'Status' = [ordered]@{
        'Info'           = (C 12 12 8.5) + ' M 12,11 V 16.5 ' + (D 12 7.8)
        'Warning'        = 'M 10.3,5 A 2,2 0 0 1 13.7,5 L 20.6,17 A 2,2 0 0 1 18.9,20 H 5.1 A 2,2 0 0 1 3.4,17 Z M 12,9.5 V 13.5 ' + (D 12 16.6)
        'Error'          = (C 12 12 8.5) + ' M 9,9 L 15,15 M 15,9 L 9,15'
        'Success'        = (C 12 12 8.5) + ' M 8,12.3 L 10.8,15 L 16,9.3'
        'Help'           = (C 12 12 8.5) + ' M 9.6,9.5 A 2.5,2.5 0 1 1 13,11.8 C 12.3,12.1 12,12.6 12,13.4 V 13.8 ' + (D 12 16.6)
        'Bell'           = $bell
        'BellOff'        = $bell + ' M 4,4 L 20,20'
        'Clock'          = (C 12 12 8.5) + ' M 12,7.5 V 12 L 15,14'
        'Calendar'       = (RR 4 5.5 16 15 2) + ' M 4,10 H 20 M 8.5,3.5 V 7 M 15.5,3.5 V 7'
        'Shield'         = 'M 12,3.5 L 19,6.5 V 11.5 C 19,16 15.8,19.2 12,20.5 C 8.2,19.2 5,16 5,11.5 V 6.5 Z'
        'Sparkles'       = 'M 10,4 L 11.6,8.4 L 16,10 L 11.6,11.6 L 10,16 L 8.4,11.6 L 4,10 L 8.4,8.4 Z M 17.5,14 L 18.3,16.2 L 20.5,17 L 18.3,17.8 L 17.5,20 L 16.7,17.8 L 14.5,17 L 16.7,16.2 Z'
        'Wifi'           = 'M 3.5,9.5 A 12,12 0 0 1 20.5,9.5 M 6.5,12.8 A 7.8,7.8 0 0 1 17.5,12.8 M 9.4,16 A 3.7,3.7 0 0 1 14.6,16 ' + (D 12 19)
        'Battery'        = (RR 3 7.5 16 9 2) + ' M 21,10.5 V 13.5 M 6.5,10.5 V 13.5 M 9.5,10.5 V 13.5'
        'Loader'         = Rays 12 12 4.5 8.5 8 -90
    }
    'Interface' = [ordered]@{
        'Sun'            = (C 12 12 4) + ' ' + (Rays 12 12 6.5 8.5 8 -90)
        'Moon'           = 'M 19.5,14.5 A 8,8 0 1 1 9.5,4.5 A 6.5,6.5 0 0 0 19.5,14.5 Z'
        'Monitor'        = (RR 3 4 18 12.5 2) + ' M 8.5,20 H 15.5 M 12,16.5 V 20'
        'Palette'        = 'M 12,3.5 A 8.5,8.5 0 0 0 12,20.5 C 13.3,20.5 13.8,19.6 13.4,18.6 C 12.9,17.4 13.6,16 15,16 H 16.6 A 3.9,3.9 0 0 0 20.5,12.1 C 20.5,7.4 16.7,3.5 12,3.5 Z ' + (D 7.5 12) + ' ' + (D 9.5 7.8) + ' ' + (D 14.5 7.8)
        'Brush'          = 'M 14.5,4.5 L 19.5,9.5 L 11,18 L 6,13 Z M 6,13 C 3.5,13.5 3.5,17 3.5,20.5 C 7,20.5 10.5,20.5 11,18'
        'Layout'         = (RR 3.5 3.5 17 17 2) + ' M 3.5,9 H 20.5 M 9.5,9 V 20.5'
        'Columns'        = (RR 3.5 3.5 17 17 2) + ' M 12,3.5 V 20.5'
        'Window'         = (RR 3 4 18 16 2) + ' M 3,8.5 H 21'
        'Keyboard'       = (RR 2.5 6 19 12 2) + ' ' + (D 6.5 10) + ' ' + (D 10 10) + ' ' + (D 14 10) + ' ' + (D 17.5 10) + ' M 8,14.5 H 16'
        'Command'        = 'M 9,9 V 7 A 2,2 0 1 0 7,9 H 17 A 2,2 0 1 0 15,7 V 17 A 2,2 0 1 0 17,15 H 7 A 2,2 0 1 0 9,17 Z'
        'Smartphone'     = (RR 7 3 10 18 2) + ' M 11,17.5 H 13'
        'Laptop'         = (RR 5 5 14 10 1.5) + ' M 3,18.5 H 21'
        'Toggle'         = (RR 3 7 18 10 5) + ' ' + (C 16 12 2.6)
    }
    'People' = [ordered]@{
        'User'           = (C 12 8 3.8) + ' M 5,20 C 5,16 8,14 12,14 C 16,14 19,16 19,20'
        'Users'          = (C 9 8.5 3.3) + ' M 3,19.5 C 3,16.2 5.6,14.5 9,14.5 C 12.4,14.5 15,16.2 15,19.5 M 15.5,5.4 A 3.3,3.3 0 0 1 15.5,11.6 M 17.5,14.8 C 19.6,15.4 21,17 21,19.5'
        'UserPlus'       = (C 10 8 3.8) + ' M 3,20 C 3,16 6,14 10,14 C 12.6,14 14.6,14.8 15.8,16.3 M 19,13 V 19 M 16,16 H 22'
        'Mail'           = (RR 3 5.5 18 13 2) + ' M 3.5,7 L 12,13 L 20.5,7'
        'Chat'           = 'M 4,6.5 A 2,2 0 0 1 6,4.5 H 18 A 2,2 0 0 1 20,6.5 V 15 A 2,2 0 0 1 18,17 H 10 L 5.5,20.5 V 17 A 2,2 0 0 1 4,15 Z'
        'Phone'          = 'M 5,4 H 8.5 L 10,8.5 L 8,10 A 11,11 0 0 0 14,16 L 15.5,14 L 20,15.5 V 19 A 1.5,1.5 0 0 1 18.5,20.5 A 15,15 0 0 1 3.5,5.5 A 1.5,1.5 0 0 1 5,4 Z'
        'Globe'          = (C 12 12 8.5) + ' M 3.5,12 H 20.5 M 12,3.5 C 9.5,6 8.5,9 8.5,12 C 8.5,15 9.5,18 12,20.5 M 12,3.5 C 14.5,6 15.5,9 15.5,12 C 15.5,15 14.5,18 12,20.5'
        'AtSign'         = (C 12 12 3.5) + ' M 15.5,12 V 13.5 A 2.5,2.5 0 0 0 20.5,13.5 V 12 A 8.5,8.5 0 1 0 17,18.8'
        'Hash'           = 'M 5,9 H 19 M 5,15 H 19 M 10,4 L 8.5,20 M 15.5,4 L 14,20'
    }
    'Cozy' = [ordered]@{
        'Paw'            = (C 12 15.5 4) + ' ' + (C 5.8 10.6 1.8) + ' ' + (C 9.4 6.6 1.8) + ' ' + (C 14.6 6.6 1.8) + ' ' + (C 18.2 10.6 1.8)
        'Cat'            = 'M 4.5,11 L 5,4 L 9.5,7.5 H 14.5 L 19,4 L 19.5,11 C 19.5,16.5 16,20 12,20 C 8,20 4.5,16.5 4.5,11 Z ' + (D 9 12.5) + ' ' + (D 15 12.5) + ' M 11,15.5 L 12,16.5 L 13,15.5'
        'Fish'           = 'M 3.5,12 C 6.5,7 13,6 17,12 C 13,18 6.5,17 3.5,12 Z M 17,12 L 21,8.5 V 15.5 Z ' + (D 8 11)
        'Coffee'         = 'M 4.5,9 H 16.5 V 15 A 4.5,4.5 0 0 1 12,19.5 H 9 A 4.5,4.5 0 0 1 4.5,15 Z M 16.5,10.5 H 18 A 2.5,2.5 0 0 1 18,15.5 H 16.5 M 8,3.5 V 6 M 12,3.5 V 6'
        'Gift'           = (RR 4 8.5 16 4.5 1) + ' M 5.5,13 V 19 A 1.5,1.5 0 0 0 7,20.5 H 17 A 1.5,1.5 0 0 0 18.5,19 V 13 M 12,8.5 V 20.5 M 12,8.5 C 9.5,8.5 7,7.8 7,6 A 2,2 0 0 1 11,5.5 Z M 12,8.5 C 14.5,8.5 17,7.8 17,6 A 2,2 0 0 0 13,5.5 Z'
        'Leaf'           = 'M 5,19 C 5,10 10,4.5 19.5,4.5 C 19.5,14 14,19 5,19 Z M 5,19 L 13,11'
        'Flower'         = (Ring 12 10 4.2 2.6 5) + ' ' + (C 12 10 1.6) + ' M 12,15.5 V 21'
        'Umbrella'       = 'M 3,12 A 9,9 0 0 1 21,12 Z M 12,12 V 18.5 A 2,2 0 0 1 8,18.5'
        'Rocket'         = 'M 12,3 C 15.5,5.5 16.5,9.5 16,14 H 8 C 7.5,9.5 8.5,5.5 12,3 Z M 8,14 L 5.5,17.5 L 8.5,17 M 16,14 L 18.5,17.5 L 15.5,17 M 10.5,17.5 L 12,21 L 13.5,17.5 ' + (C 12 9 1.5)
        'Trophy'         = 'M 8,4 H 16 V 9.5 A 4,4 0 0 1 8,9.5 Z M 8,5.5 H 5 A 3,3 0 0 0 8,10 M 16,5.5 H 19 A 3,3 0 0 1 16,10 M 12,13.5 V 17 M 8.5,20 H 15.5 M 9.5,20 L 10,17 H 14 L 14.5,20'
        'Lightbulb'      = 'M 9,17 V 15.5 C 7,14.2 5.5,12.2 5.5,9.5 A 6.5,6.5 0 0 1 18.5,9.5 C 18.5,12.2 17,14.2 15,15.5 V 17 Z M 9.5,20.5 H 14.5'
        'Package'        = 'M 12,3 L 20,7 V 17 L 12,21 L 4,17 V 7 Z M 4,7 L 12,11 L 20,7 M 12,11 V 21 M 8,5 L 16,9'
        'Book'           = 'M 4,5 A 1.5,1.5 0 0 1 5.5,3.5 H 11 A 1,1 0 0 1 12,4.5 V 20 A 1.5,1.5 0 0 0 10.5,18.5 H 4 Z M 20,5 A 1.5,1.5 0 0 0 18.5,3.5 H 13 A 1,1 0 0 0 12,4.5 V 20 A 1.5,1.5 0 0 1 13.5,18.5 H 20 Z'
        'ShoppingCart'   = 'M 3,4 H 5.5 L 8,15.5 H 18 L 20,7.5 H 6.3 ' + (C 9 19 1.4) + ' ' + (C 17 19 1.4)
        'CreditCard'     = (RR 3 5.5 18 13 2) + ' M 3,10 H 21 M 6.5,15 H 10'
        'Wrench'         = 'M 14.7,3.6 A 5,5 0 0 0 11.1,10.6 L 4.3,17.4 A 1.6,1.6 0 0 0 6.6,19.7 L 13.4,12.9 A 5,5 0 0 0 20.4,9.3 L 17.6,12.1 L 14.6,11.4 L 13.9,8.4 Z'
        'Bug'            = (RR 7 7 10 13 5) + ' M 12,11 V 20 M 7,12.5 H 3.5 M 20.5,12.5 H 17 M 7,16.5 L 4,18.5 M 17,16.5 L 20,18.5 M 7.5,8.5 L 4.5,6 M 16.5,8.5 L 19.5,6 M 9,6.5 A 3,3 0 0 1 15,6.5'
        'Gauge'          = 'M 4,16 A 8,8 0 1 1 20,16 M 12,16 L 15.5,9.5 ' + (D 12 16)
        'Activity'       = 'M 3,12 H 7 L 9.5,5 L 14.5,19 L 17,12 H 21'
        'TrendingUp'     = 'M 3,17 L 9,11 L 13,15 L 21,7 M 15,7 H 21 V 13'
        'BarChart'       = 'M 4,20 H 20 M 6.5,20 V 12 M 11,20 V 5 M 15.5,20 V 9 M 20,16.5 V 14'
        'PieChart'       = 'M 12,3.5 V 12 H 20.5 A 8.5,8.5 0 1 1 12,3.5 Z M 15,3.9 A 8.5,8.5 0 0 1 20.1,9 H 15 Z'
        'Inbox'          = 'M 3.5,13 L 6,5.5 A 1.5,1.5 0 0 1 7.4,4.5 H 16.6 A 1.5,1.5 0 0 1 18,5.5 L 20.5,13 V 18 A 1.5,1.5 0 0 1 19,19.5 H 5 A 1.5,1.5 0 0 1 3.5,18 Z M 3.5,13 H 8.5 L 10,15.5 H 14 L 15.5,13 H 20.5'
        'Layers'         = 'M 12,3.5 L 20.5,8 L 12,12.5 L 3.5,8 Z M 3.5,12 L 12,16.5 L 20.5,12 M 3.5,16 L 12,20.5 L 20.5,16'
        'GripVertical'   = (D 9 6) + ' ' + (D 15 6) + ' ' + (D 9 12) + ' ' + (D 15 12) + ' ' + (D 9 18) + ' ' + (D 15 18)
        'Power'          = 'M 12,3.5 V 11 M 7,6.5 A 7.5,7.5 0 1 0 17,6.5'
        'Droplet'        = 'M 12,3.5 C 12,3.5 5.5,10.5 5.5,14.5 A 6.5,6.5 0 0 0 18.5,14.5 C 18.5,10.5 12,3.5 12,3.5 Z'
        'Flame'          = 'M 12,20.5 A 6.5,6.5 0 0 1 5.5,14 C 5.5,10 9,8 9.5,4 C 12,5.5 13.5,8 13.5,10 C 14.5,9.5 15,8.5 15,7.5 C 17,9.5 18.5,11.5 18.5,14 A 6.5,6.5 0 0 1 12,20.5 Z'
        'Target'         = (C 12 12 8.5) + ' ' + (C 12 12 5) + ' ' + (C 12 12 1.5)
        'Circle'         = C 12 12 8.5
        'Square'         = RR 4 4 16 16 3
        'CheckSquare'    = (RR 4 4 16 16 3) + ' M 8,12.2 L 10.8,15 L 16,9.3'
        'PlusCircle'     = (C 12 12 8.5) + ' M 12,8 V 16 M 8,12 H 16'
        'MinusCircle'    = (C 12 12 8.5) + ' M 8,12 H 16'
    }
}

# --- XAML ---
$total = 0
$xaml = [System.Text.StringBuilder]::new()
$null = $xaml.AppendLine('<!--')
$null = $xaml.AppendLine('    Muzzle icon pack: rounded, stroke-drawn vector icons on a 24 x 24 grid, keyed "Muzzle.Icon.<Kind>".')
$null = $xaml.AppendLine('    Draw them with MuzzleIcon (or any Path: Stroke = colour, StrokeThickness ~1.8, round caps and joins, no Fill).')
$null = $xaml.AppendLine('    Generated by tools/Generate-Icons.ps1 together with MuzzleIconKind.cs: edit the table there, not this file.')
$null = $xaml.AppendLine('-->')
$null = $xaml.AppendLine('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
$null = $xaml.AppendLine('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"')
$null = $xaml.AppendLine('                    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"')
$null = $xaml.AppendLine('                    xmlns:po="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"')
$null = $xaml.AppendLine('                    mc:Ignorable="po">')
foreach ($group in $groups.Keys) {
    $null = $xaml.AppendLine('')
    $null = $xaml.AppendLine("    <!-- $group -->")
    foreach ($name in $groups[$group].Keys) {
        $null = $xaml.AppendLine("    <StreamGeometry x:Key=`"Muzzle.Icon.$name`" po:Freeze=`"True`">$($groups[$group][$name])</StreamGeometry>")
        $total++
    }
}
$null = $xaml.AppendLine('')
$null = $xaml.Append('</ResourceDictionary>')

# --- Enum ---
$cs = [System.Text.StringBuilder]::new()
$null = $cs.AppendLine('namespace Muzzle.Theme.Controls;')
$null = $cs.AppendLine('')
$null = $cs.AppendLine("/// <summary>")
$null = $cs.AppendLine("/// The $total icons in Muzzle's built-in pack (Themes/Icons/MuzzleIcons.xaml, resource key ""Muzzle.Icon.&lt;Kind&gt;"").")
$null = $cs.AppendLine('/// Generated by tools/Generate-Icons.ps1: edit the table there, not this file.')
$null = $cs.AppendLine('/// </summary>')
$null = $cs.AppendLine('public enum MuzzleIconKind {')
$null = $cs.AppendLine('    /// <summary>No icon (MuzzleIcon draws nothing).</summary>')
$null = $cs.AppendLine('    None,')
foreach ($group in $groups.Keys) {
    $null = $cs.AppendLine('')
    $null = $cs.AppendLine("    // $group")
    foreach ($name in $groups[$group].Keys) {
        $label = ([regex]::Replace($name, '(?<=[a-z0-9])(?=[A-Z])', ' ')).ToLowerInvariant()
        $null = $cs.AppendLine("    /// <summary>The `"$label`" icon ($($group.ToLowerInvariant()) group).</summary>")
        $null = $cs.AppendLine("    $name,")
    }
}
$text = $cs.ToString().TrimEnd("`r", "`n").TrimEnd(',') + "`n}"

function Save([string]$path, [string]$content) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { $null = New-Item -ItemType Directory -Path $dir }
    [IO.File]::WriteAllText($path, $content.Replace("`r`n", "`n").Replace("`n", "`r`n"), [Text.UTF8Encoding]::new($false))
}

Save (Join-Path $Root 'Themes\Icons\MuzzleIcons.xaml') $xaml.ToString()
Save (Join-Path $Root 'Controls\MuzzleIconKind.cs') $text
"generated $total icons"