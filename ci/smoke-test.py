#!/usr/bin/env python3
"""Verify published binaries start: the CLI reports a version and the server signals READY."""

import argparse
import os
import queue
import stat
import subprocess
import sys
import threading
import time
from pathlib import Path

STARTUP_TIMEOUT_SECONDS = 30


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("publish_dir", type=Path)
    publish_dir: Path = parser.parse_args().publish_dir

    # Windows binaries carry an .exe suffix; Unix binaries have none.
    suffix = ".exe" if os.name == "nt" else ""
    cli = publish_dir / f"recyclarr{suffix}"
    server = publish_dir / f"recyclarr-server{suffix}"

    if os.name != "nt":
        for binary in (cli, server):
            binary.chmod(binary.stat().st_mode | stat.S_IRUSR | stat.S_IXUSR)

    version = subprocess.run(
        [cli, "--version"], capture_output=True, text=True, check=True
    ).stdout.strip()
    print(f"Recyclarr CLI started successfully: {version}")

    smoke_server(server)


def smoke_server(server: Path) -> None:
    # The server refuses to start without an API key of at least 32 characters.
    env = os.environ | {"RECYCLARR_API_KEY": "smoke-test-api-key-0123456789abcdef"}
    process = subprocess.Popen(
        [server, "--urls=http://127.0.0.1:0"],
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
        env=env,
    )

    # A reader thread lets the main loop enforce the startup deadline while waiting for lines.
    lines: queue.Queue[str | None] = queue.Queue()
    threading.Thread(target=read_lines, args=(process, lines), daemon=True).start()

    # The server logs to stdout, so keep consumed lines to report why it failed.
    output: list[str] = []
    deadline = time.monotonic() + STARTUP_TIMEOUT_SECONDS
    try:
        while (remaining := deadline - time.monotonic()) > 0:
            try:
                line = lines.get(timeout=remaining)
            except queue.Empty:
                break
            if line is None:
                fail("Recyclarr server exited before startup", output)
            output.append(line)
            if line.startswith("READY:"):
                print(f"Recyclarr server started successfully: {line}")
                return
        fail(
            f"Recyclarr server did not start within {STARTUP_TIMEOUT_SECONDS} seconds",
            output,
        )
    finally:
        process.kill()
        process.wait()


def read_lines(
    process: subprocess.Popen[str], lines: "queue.Queue[str | None]"
) -> None:
    assert process.stdout is not None
    for line in process.stdout:
        lines.put(line.rstrip("\n"))
    lines.put(None)


def fail(message: str, output: list[str]) -> None:
    sys.exit("\n".join([f"{message}:", *output]))


if __name__ == "__main__":
    main()
