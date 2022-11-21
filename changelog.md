# Changelog

## Version 1.0.2, November 21, 2022

- New support for **Team Frag Delta** curve to highlight the team score diffential as it changes throughout the game.
- Optimized the min and max axis ranges for both the time x-axis as well as the frag count vertical access.
- Bugfix: addressed erroneous 'MVDplayer 0 frag events' better so as to support games that go into overtime.
- Minor tweaks here and there.

## Version 1.0.1, November 17, 2022

- Generated chart images automatically get saved as `[MVDFilePath]-chart.png`.
- **MVDparser** now works in the Windows user's temp folder, and all generated logs get deleted after each run.
- Integrated with [NDesk.Options](https://github.com/gibbed/NDesk.Options) to quickly get command-line option functionality (see [REAMDE.md](https://github.com/delta42/qchart/blob/master/README.md))
- Bugfix: we skip over extraneous event log files created by **MVDparser** as a woraround to what appears to be a bug on its part.
- Bugfix: we now ignore any log events taking place after matchttime; these sometimes contained player frags of 0, causing incorrect graphing, incorrect player score as well as total team score.

## Version 1.0.0, October 17, 2022

- Inaugural version, aka **QChart Alpha**
