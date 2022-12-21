[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $Arch
)

if ($IsWindows) {
    "win-$Arch"
}
elseif ($IsLinux) {
    "linux-$Arch"
}
elseif ($IsMacOS) {
    "osx-$Arch"
}
