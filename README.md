# MoneyMan

A financial money management library and applications that utilize it.

[![Install](https://img.shields.io/badge/prerelease-win--x64-green)](https://moneymanreleases.blob.core.windows.net/releases/prerelease/win-x64/Nerdbank.MoneyMan.Setup.exe)
[![Install](https://img.shields.io/badge/prerelease-win--arm64-green)](https://moneymanreleases.blob.core.windows.net/releases/prerelease/win-arm64/Nerdbank.MoneyMan.Setup.exe)

[![Join the chat at https://gitter.im/MoneyManagement/Lobby](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/MoneyManagement/Lobby?utm_source=badge&utm_medium=badge&utm_content=badge)

[![Azure Pipelines status](https://dev.azure.com/andrewarnott/OSS/_apis/build/status/AArnott.MoneyMan?branchName=main)](https://dev.azure.com/andrewarnott/OSS/_build/latest?definitionId=29&branchName=main)
[![codecov](https://codecov.io/gh/aarnott/moneyman/branch/main/graph/badge.svg)](https://codecov.io/gh/aarnott/moneyman)

## Why?

Because Intuit Quicken has become far too old to work well on modern systems. It suffers from:

1. Poor support for high DPI screens
1. Slow
1. Buggy
1. No competition

Yet Intuit charges $70+ each year for upgrades that add very little value.

MoneyMan is intended to offer users an alternative.

## How?

MoneyMan is primarily implemented in a .NET 5 library, with applications for (eventually) each platform and device.
It utilizes SQLite as its data store due to its cross-platform availability, compact size and high performance.
