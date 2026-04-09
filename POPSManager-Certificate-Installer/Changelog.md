---

## 4️⃣ `CHANGELOG.md`

```markdown
# Changelog

All notable changes to the **POPSManager Certificate Installer** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2025-04-09

### Added

- **Install-Certificate.ps1** — Automated certificate installer with:
  - Auto-elevation to Administrator when required
  - Certificate validation (format, expiration, thumbprint display)
  - Installation to `TrustedPeople` (CurrentUser) and `TrustedPublisher` (LocalMachine)
  - Duplicate detection — skips if certificate already installed
  - Interactive user confirmation with clear scope description
  - Timestamped log files via `-LogToFile` switch
  - Silent mode via `-Force` switch
  - Custom certificate path support via `-CertificatePath`
  - Professional banner and color-coded console output
  - Strict error handling with meaningful exit codes

- **Uninstall-Certificate.ps1** — Automated certificate removal with:
  - Removal by certificate file or thumbprint
  - Auto-elevation to Administrator when required
  - Safe removal with "not found" handling (idempotent)
  - Interactive confirmation with clear warning about consequences
  - All logging and silent mode features from the installer

- **README.md** — Comprehensive documentation with:
  - Quick start guide
  - Advanced usage and parameter reference
  - Security notes and trust scope explanation
  - Troubleshooting guide
  - Certificate generation instructions

- **Package structure** — Professional layout with:
  - `certificates/` directory for certificate file
  - `logs/` directory (auto-created) for audit logs
  - `.gitignore` configured for sensitive files
  - MIT License

### Security

- Certificate is installed to minimum required stores only
- No root CA trust is granted
- All operations require explicit user confirmation by default
- Full audit trail available via log files
- Private keys (`.pfx`) are excluded from distribution

---

## [Unreleased]

### Planned

- Optional Windows notification toast on completion
- Certificate expiration reminder check
- Batch mode for multi-machine deployment
- Integration with POPSManager MSIX installer workflow

---

[1.0.0]: https://github.com/POPSManager/POPSManager/releases/tag/cert-installer-v1.0.0
[Unreleased]: https://github.com/POPSManager/POPSManager/compare/cert-installer-v1.0.0...HEAD
