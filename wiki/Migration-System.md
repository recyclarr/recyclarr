The Migration System allows Recyclarr to attempt certain automatic actions for the user. These
actions, referred to as Migration Steps, are usually in response to certain changes between releases
of Recyclarr (mostly major releases, which represent breaking changes). The overall goal of this
system is to reduce the amount of manual action a user must take.

## Behavior

1. When Recyclarr is executed, it first runs through all of the Migration Steps in a specific,
   pre-determined order.
1. Each Migration Step is checked if it needs to run or not.
   - If it needs to run, its actions are performed immediately.
   - Otherwise, it is skipped and the next Migration Step is processed.

Migration Steps can fail. When this happens, instructions are provided to the user on how to recover
and/or perform those steps manually. Regardless of the reason, Recyclarr will immediately exit and
cannot proceed until the advice output during the previous execution is followed.

## Failure & Recovery

When a Migration Step fails, processing of further steps is halted and the program exits. The
failure also results in diagnostic information and remediation steps being printed to the console:

- A description of the Migration Step that failed. This is usually a description of what the step
  was trying to do.
- A failure reason. Explains why the step failed and could not be processed.
- Remediation steps. One or more ways to solve the problem. Will likely either ask you to perform
  the steps by hand or take some action to allow the migration step to succeed the next time
  Recyclarr is executed.

## Current Migration Steps

The list below describes the migration steps that are performed today, under what conditions they
will be executed, and reasons why they might fail. Most of this information is already printed in
real time by Recyclarr in response to failures.

### Rename app data directory from `trash-updater` to `recyclarr`

- **When**: `v2.0`
- **What**: Renames your `trash-updater` app data directory to `recyclarr` automatically.
- **Why**: The application was renamed from Trash Updater to Recyclarr. Thus, the app data directory
  name needed to follow suit.
- **How can it fail?**
  - The `recyclarr` directory already exists.
  - User lacks sufficient permissions on the filesystem.

### Rename default `trash.yml` file to `recyclarr.yml`

- **When**: `v2.0`
- **What**: Renames your `trash.yml` file to `recyclarr.yml` automatically.
- **Why**: The application was renamed from Trash Updater to Recyclarr. Thus, the app data directory
  name needed to follow suit.
- **How can it fail?**
  - The `recyclarr.yml` file already exists.
  - User lacks sufficient permissions on the filesystem.
