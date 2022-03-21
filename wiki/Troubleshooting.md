# Errors & Solutions

* On Mac or Linux OS, you may see the following error when you run `trash`:

  ```txt
  Failed to map file. open(/Users/foo/Downloads/trash) failed with error 13
  Failure processing application bundle.
  Couldn't memory map the bundle file for reading.
  A fatal error occured while processing application bundle
  ```

  This cryptic message is actually a permissions error, likely because your executable does not have
  read permissions set. Simply run `chmod u+rx trash` to add read + execute permissions on the
  `trash` executable.
