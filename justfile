# Run http/<file>.http against the running server; omit service to pick it interactively
http file service="":
    timeout --foreground 120 httpyac send http/{{ file }}.http --all --bail -o body \
        {{ if service != "" { "--var service=" + service } else { "" } }}
