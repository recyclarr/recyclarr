# Obtaining Debug Logs

Trash Updater always outputs logs as files in a directory on your filesystem. Each execution of
Trash Updater yields a new file and those files always contain verbose (debug) logs. When reporting
issues, I ask that you always include logs from the file rather than the command line output since
Trash Updater will not include debug logs by default in the console output.

Below is a list of locations where you can find the log directory depending on platform.

| Platform | Location                                           |
| -------- | -------------------------------------------------- |
| Windows  | `%APPDATA%\trash-updater\logs`                     |
| Linux    | `~/.config/trash-updater/logs`                     |
| MacOS    | `~/Library/Application Support/trash-updater/logs` |

# Errors & Solutions

* On Mac or Linux OS, you may see the following error when you run `trash`:

  ```txt
  Failed to map file. open(/Users/foo/Downloads/trash) failed with error 13
  Failure processing application bundle.
  Couldn't memory map the bundle file for reading.
  A fatal error occurred while processing application bundle
  ```

  This cryptic message is actually a permissions error, likely because your executable does not have
  read permissions set. Simply run `chmod u+rx trash` to add read + execute permissions on the
  `trash` executable.

* When communicating with Radarr or Sonarr, you get the following exception message:

  > FlurlParsingException: Response could not be deserialized to JSON: `GET
  > http://hostname:6767/api/v3/customformat?apikey=SNIP` --->
  > Newtonsoft.Json.JsonSerializationException: Deserialized JSON type
  > 'Newtonsoft.Json.Linq.JArray' is not compatible with expected type
  > 'Newtonsoft.Json.Linq.JObject'. Path '', line 1, position 2.

  This means your Base URL is missing from the URL you specified in the YAML. See issue [#42] for
  more details.

* On Ubuntu 22.04 or deriviates when you run 'trash radarr' you will get the following error: 

  ```txt
  [ERR] An exception occurred during git operations on path: /home/REDACTED/.config/trash-updater/repo
  LibGit2Sharp.LibGit2SharpException: could not load ssl libraries
  ------
  [INF] Deleting local git repo and retrying git operation...
  [1] 257872 segmentation fault (core dumped) ./trash radarr
  ```
  
  Ubuntu moved from libssl 1.1 to libssl 3.0 in 22.04. This currently breaks the updater. See issue [#54] for more details.

  As a workaround you can install libssl-1.1 from an earlier version, however, this might impact other applications.

  On Ubuntu 22.04 x64 (64-bit) run the following commands in the shell  
    `wget http://mirrors.kernel.org/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1l-1ubuntu1.2_amd64.deb`  
    `dpkg -i libssl1.1_1.1.1l-1ubuntu1.2_amd64.deb`  

  On Ubuntu 22.04 x86 (32-bit) run the following commands in the shell
    `wget http://mirrors.kernel.org/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1l-1ubuntu1.2_i386.deb`  
    `dpkg -i libssl1.1_1.1.1l-1ubuntu1.2_i386.deb`  

[#42]: https://github.com/rcdailey/trash-updater/issues/42
[#54]: https://github.com/rcdailey/trash-updater/issues/54
