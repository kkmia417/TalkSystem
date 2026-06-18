# Contributing

Thanks for improving Talk System.

## Development Setup

1. Open this repository with Unity `6000.0` or newer.
2. Let Unity restore packages.
3. Run EditMode tests from the Unity Test Runner.

## Validation Checklist

- Runtime code remains independent from Editor-only APIs.
- CSV behavior is covered by tests when parser or schema logic changes.
- Editor tools do not silently mutate dialogue data.
- Public API changes are documented in `README.md` or `Documentation~/`.

## Pull Requests

Keep pull requests focused. Include the issue number, testing notes, and screenshots/GIFs for visible editor or sample changes.
