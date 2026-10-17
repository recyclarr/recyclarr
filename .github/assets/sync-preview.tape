# README demo. Record with `mise run demo` (scripts/record-demo.sh).
# The recording shell skips rc files so shell hooks (mise) cannot replace the demo config.

Output "/tmp/recyclarr-demo/frames/"

Set Shell bash
Set FontSize 16
Set Width 1200
Set Height 700
Set Padding 20
Set Theme "Catppuccin Mocha"
Set Framerate 10

Hide
Type "exec bash --norc --noprofile"
Enter
Type 'export PATH="$PWD/src/Recyclarr.Cli/bin/Debug/net10.0:$PATH" PS1="$ "'
Enter
Type 'export RECYCLARR_CONFIG_DIR=/tmp/recyclarr-demo/config'
Enter
# Reveal output line by line for readability; script keeps a TTY so tables still render.
Type 'recyclarr() { script -qfc "command recyclarr $*" /dev/null | while IFS= read -r l; do printf "%s\n" "$l"; sleep 0.05; done; }'
Enter
Type "clear"
Enter
Show

Type "recyclarr sync --preview"
Sleep 500ms
Enter
Wait@90s /\$\s*$/
Sleep 3s
