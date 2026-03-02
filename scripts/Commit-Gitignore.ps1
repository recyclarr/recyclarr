#!/usr/bin/env pwsh
git commit -m 'chore(git): git ignore updates' -- `
    .gitignore `
    scripts/Update-Gitignore.ps1 `
    scripts/Commit-Gitignore.ps1
