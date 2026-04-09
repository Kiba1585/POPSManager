# POPSManager — Certificate Installer

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![PowerShell 5.1+](https://img.shields.io/badge/PowerShell-5.1%2B-blue.svg)](https://docs.microsoft.com/en-us/powershell/)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6.svg)](https://www.microsoft.com/windows)

> **Installs the POPSManager code-signing certificate** so Windows trusts MSIX sideloaded packages.

---

## 📋 Overview

POPSManager is distributed as an **MSIX package** signed with a self-signed certificate. Since the certificate is not issued by a public Certificate Authority, it must be manually trusted on each machine before installation.

This package provides professional, automated scripts to **install** and **uninstall** the certificate with full logging, error handling, and user confirmation.

### What Does It Do?

| Action  | Store                          | Scope         |
|---------|--------------------------------|---------------|
| Install | `TrustedPeople`                | CurrentUser   |
| Install | `TrustedPublisher`             | LocalMachine  |
| Remove  | Both stores listed above       | Both scopes   |

---

## 📦 Package Contents
