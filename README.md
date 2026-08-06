<p align="center">
	<img src="/title.png" />
	<h3 align="center">lamp/azer</h3>
</p>

# lamp/azer

solo fork of **deltalazer/delta**, with very **(awesome)** work in progress features!

# which means you can play with the source code and do a lot of things!

what should i put here?

## project base

- upstream project: https://github.com/deltalazer/delta
- up up upstream project: https://github.com/ppy/osu
so uh this is a fork of deltalazer so this is a fork fork of osu!(lazer) basically yeah i hope you understand :D

## ---> important server note

- **debug builds :D** connect to osu!dev server
- **release builds :c** connect to official osu! server

i **DON'T** recommend building for release as this currently doesn't have systems in place that make it safe to use in the official osu! server
(working on a custom server dw)

## run the game

```bash
dotnet build osu.Desktop/osu.Desktop.csproj -c Debug
```

publish debug builds:

```bash
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r win-x64 --self-contained false -o ../builds/windows-debug-v2
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r linux-x64 --self-contained false -o ../builds/linux-debug-v2
```

## reporting bugs

please include:

- map file and exact section/hitobject settings used
- expected vs actual behavior
- crash logs/stack trace if available
- whether you used debug or release build

## contributing :D

community contributions are welcome!!

- open issue for bug reports/feature proposals
- open pull request with focused changes
- include tests where possible for regression safety

## license

same license as upstream osu!lazer

see `LICENCE`
