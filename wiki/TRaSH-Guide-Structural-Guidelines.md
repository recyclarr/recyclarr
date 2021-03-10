In order for the `trash.py` script to remain as stable as possible between updates to the TRaSH
guides, the following structural guidelines are provided. This document also serves as documentation
on how the python script is implemented currently.

# Definitions

* **Term**<br>
  A phrase that is included in Sonarr release profiles under either the "Preferred", "Must Contain",
  or "Must Not Contain" sections. In the TRaSH guides these are regular expressions.

* **Ignored**<br>
  The API term for "Must Not Contain"

* **Required**<br>
  The API term for "Must Contain"

* **Category**<br>
  Refers to any of the different "sections" in a release profile where terms may be stored. Includes
  "Must Not Contain" (ignored), "Must Contain" (required), and "Preferred".

* **Mention**<br>
  This generally refers to any human-readable way of stating something that the script relies on for
  parsing purposes.

# Structural Guidelines

Different types of TRaSH guides are parsed in their own unique way, mostly because the data set is
different. In order to ensure the script continues to be reliable, it's important that the structure
of the guides do not change. The following sections outline various guidelines to help achieve this
goal.

Note that all parsing happens directly on the markdown files themselves from the TRaSH github
repository. Those files are processed one line at a time. Guidelines will apply on a per-line basis,
unless otherwise stated.

## Sonarr Release Profiles

1. **Headers define release profiles.**

   A header with the phrase `Release Profile` in it will start a new release profile. The header
   name may contain other keywords before or after that phrase, such as `First Release Profile`.
   This header name in its entirety will be used as part of the release profile name when the data
   is pushed to Sonarr.

1. **Fenced code blocks must *only* contain ignored, required, or preferred terms.**

   Between headers, fenced code blocks indicate the terms that will be captured and pushed to Sonarr
   for any given type of category (required, preferred, or ignored). There may be more than one
   fenced code block, and each fenced code block may have more than one line inside of it. Each line
   inside of a fenced code block is treated as 1 single term. Commas at the end of each line are
   removed, if they are present.

1. **For preferred terms, a score must be mentioned prior to the first fenced code block.**

   Each separate line in the markdown file is inspected for the word `score` followed by a number
   inside square brackets, such as `[100]`. If found, the score between the brackets is captured and
   applied to any future terms found within fenced code blocks. Between fenced code blocks under the
   same heading, a new score using these same rules may be mentioned to change it again.

   Terms mentioned prior to a score being set are discarded.

1. **Categories shall be specified before the first fenced code block.**

   Categories are technically optional; if one is never explicitly mentioned in the guide, the
   default is "Preferred". Depending on the category, certain requirements change. At the moment, if
   "Preferred" is used, this also requires a score. However "Must Not Contain" and "Must Contain" do
   not require a score.

   A category must mentioned as one of the following phrases (case insensitive):

   * `Preferred`
   * `Must Not Contain`
   * `Must Contain`

   These phrases may appear in nested headers, normal lines, and may even appear inside the same
   line that defines a score (e.g. `Insert these as "Preferred" with a score of [100]`).

1. **"Include Preferred when Renaming" may be optionally set via mention.**

   If you wish to control the checked/unchecked state of the "Include Preferred when Renaming"
   option in a release profile, simply mention the phrase `include preferred` (case-insensitive) on
   any single line. This marks it as "CHECKED". If it also finds the word `not` on that same line,
   it will instead be marked "UNCHECKED".

   This is optional and the default is always "UNCHECKED".

### Release Profile Naming

The script procedurally generates a name for release profiles it creates. For the following example:

```txt
[Trash] Anime - First Release Profile
```

The name is generated as follows:

* `Anime` comes from the guide type (could be `WEB-DL`)
* `First Release Profile` is directly from one of the headers in the anime guide
* `[Trash]` is used by the script to mean "This release profile is controlled by the script". This
  is to separate it from any manual ones the user has defined, which the script will not touch.
