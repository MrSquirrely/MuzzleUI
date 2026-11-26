# CLAUDE.md

Everything an agent needs is in [AGENTS.md](AGENTS.md): layout, branch workflow, commands, coding style, theming rules and the definition of done.

@AGENTS.md

## Claude Code specifics

- Work on `dev`. Never commit to `main`; it only changes via a PR from `dev` when the maintainer asks for a release.
- Before saying work is done, run the Release build (0 warnings) and the test suite, and say what you ran.
- For visual changes, run the gallery's `--snapshot` mode into a scratch directory and look at the PNGs; don't write them into the repo.