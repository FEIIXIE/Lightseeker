# Lightseeker | Unity Project Source Snapshot

Document version: `v0.5`  
Snapshot version: `v0.43.413`  
Status: `PRIVATE_REPOSITORY_SNAPSHOT / SIGNED_CANDIDATE_VALIDATED`

## Repository Contents

This snapshot contains the files required to restore and rebuild the Unity project:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Documentation/`
- `.vsconfig`

The repository intentionally excludes `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, APK/AAB packages, signing materials, locally generated project files, internal collaboration tools, and candidate-asset archives.

## Validated Baseline

- Unity: `2022.3.62f3c1`
- Project version: `v0.43.413`
- Android bundle version: `0.43.413`
- Android version code: `651`
- Core smoke tests: `65/65 PASS`
- Windows PowerShell 5.1 build-wrapper validation: `PASS`
- Android ARM64 non-debuggable signed candidate build and certificate checks: `PASS` (the APK is not stored in this source repository)

## Opening the Project

1. Install Unity `2022.3.62f3c1`.
2. In Unity Hub, select the repository root directory.
3. Allow Unity to regenerate its local caches.
4. Do not commit Unity-generated caches, build artifacts, or machine-specific user settings.

## Security and Third-Party Assets

- This repository must not contain API keys, wallet seed phrases, private keys, Android keystores, or machine-specific absolute paths.
- Attribution and licensing records for third-party fonts and audio are available in the [third-party runtime asset attribution and licensing notice](Documentation/主程-第三方运行时资源来源与许可-公开说明-v1.0.md).
- This repository is currently maintained as a private development repository. A fresh privacy, licensing, and binary-distribution audit is required before making it public.

## Document History

| Version | Date | Change |
|---|---|---|
| v0.1 | 2026-09-28 | Created the sanitized source-snapshot notes for the early v0.43.96 build. |
| v0.2 | 2026-09-28 | Added the Unity version, project-opening instructions, cache exclusions, and third-party attribution guidance. |
| v0.3 | 2026-10-01 | Updated the private repository baseline to v0.43.184 and recorded the applicable validation and third-party resource information. |
| v0.4 | 2026-10-12 | Updated the pre-final source snapshot to v0.43.413 and recorded Android 651, core smoke-test, and signed-candidate validation results. |
| v0.5 | 2026-10-12 | Rewrote the repository overview in English for international competition reviewers; no game or build output changed. |

