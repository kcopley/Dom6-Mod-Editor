Dom6 Mod Editor @VERSION@ for macOS and Linux (test build, @DATE@, @COMMIT@, @RID@)
==============================================================================

An editor for Dominions 6 mod files (.dm): browse the game's own data and a mod's, edit every
entity, build event chains, and save back without disturbing lines you didn't change.

The same editor as the Windows one (one program on all three). New in this build (fixes): a
mercenary band's commander, unit and item are saved as names, as the game reads them (picking
one wrote a number, and the game stopped loading the mod); events show every #1unit, #2com,
#1d6units, ... line; an event with too many requirements or effects says which lines the game
drops; the report lists lines outside any block; sprites without transparency show as in the
game. In 0.13.0: "Merge mods..." (several mods into one new file, colliding numbers moved and
every reference following), entities in windows of their own ("New window", Ctrl+click),
spells' #damage read from the game itself, and the report saying when a spell can't work. It
hasn't been tried on a Mac yet: please report what looks wrong or doesn't work, with a
screenshot if you can.

Linux (linux-x64: most PCs; linux-arm64: ARM boards and laptops)
- Unpack and run:
      tar xzf Dom6ModEditor-@VERSION@-@RID@.tar.gz
      ./Dom6ModEditor/Dom5Editor            (or: ./Dom6ModEditor/Dom5Editor my.dm)
  Keep the other files next to the program (vanilla.dm and the rest are the game's data the
  editor builds on). The first start takes a few seconds longer: it unpacks its graphics
  libraries to ~/.net.
- Needs an X11 or Wayland desktop (Wayland through XWayland), fontconfig and ICU (libicu),
  which desktop installs have. If it says "Couldn't find a valid ICU package", install libicu
  (Debian/Ubuntu: apt install libicu-dev; Fedora: dnf install libicu), or start it with
      DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 ./Dom6ModEditor/Dom5Editor

macOS (osx-arm64: Apple Silicon, M1 and later; osx-x64: Intel Macs)
- Unzip and move "Dom6 Mod Editor.app" to Applications (or anywhere).
- The app isn't signed by a registered Apple developer, nor notarized by Apple, so macOS won't
  open it as downloaded (it says the app "is damaged" or "can't be checked for malicious
  software"). Apple Silicon runs only signed programs: the program inside has an ad-hoc
  signature from the build, but the app as a whole isn't signed. So, once, in Terminal:
      xattr -dr com.apple.quarantine "/Applications/Dom6 Mod Editor.app"
      codesign --force --deep --sign - "/Applications/Dom6 Mod Editor.app"
  then open it as usual. The first line tells macOS you trust the download; the second signs
  the whole app ad hoc, on your Mac ("--sign -": no developer identity; codesign comes with
  macOS, no Xcode needed). Doing it again after moving the app is harmless.
  On an Intel Mac the first line alone is enough (or right-click the app, Open, and confirm;
  or System Settings, Privacy & Security, "Open Anyway").
- Cmd works where these notes say Ctrl (Cmd+O, Cmd+S, Cmd+P, Cmd+F, Cmd+Z ...).
- .dm files can be opened with the editor from Finder (Open With).

Dominions 6
- Your mods are in the game's data folder, ~/.dominions6/mods (macOS and Linux; in the game:
  Game Tools, Open User Data Directory).
- The game's unit, item and site pictures and nation flags are read from your Dominions 6
  install (its data folder). The editor looks in Steam's libraries:
      Linux: ~/.local/share/Steam/steamapps/common/Dominions6
             (Flatpak Steam: ~/.var/app/com.valvesoftware.Steam/.local/share/Steam/...)
      macOS: ~/Library/Application Support/Steam/steamapps/common/Dominions6
  If it doesn't find the game, the status bar says so: use the small arrow next to Load,
  "Dominions 6 folder...", and pick the game's folder (the one with dom6_amd64 or dom6_mac and
  the data folder; in Steam: Manage, Browse local files). It's read at the next start.
- The game's own texts (unit, item, spell and nation descriptions) and the vanilla events'
  messages are read from Dominions6.exe, the game's Windows program, version 6.37. A macOS or
  Linux install may not have that file; then those texts aren't shown (the rest works). If
  you have it (the same game version), point the editor at it: start it with
      DOM6_EXE=/path/to/Dominions6.exe ./Dom6ModEditor/Dom5Editor
  (macOS: DOM6_EXE=/path/to/Dominions6.exe "/Applications/Dom6 Mod Editor.app/Contents/MacOS/Dom5Editor").

Using it
- Load (Ctrl+O) opens a .dm file; Save (Ctrl+S) writes it back. Lines you didn't touch are
  saved exactly as they were. Undo/Redo: Ctrl+Z / Ctrl+Y (or Ctrl+Shift+Z).
- Ctrl+P: go to anything by name or ID. Ctrl+F: search the list (for events, also the message
  text). Alt+Left / Alt+Right: back and forward. Hover anything for what it does.
- Validate checks the mod as it is now and opens the report; "Every issue..." in the report
  lists everything the checks found, with filters.
- Mods from the Steam workshop: Steam replaces those files when the mod updates, so the editor
  asks before saving there. Save a copy in ~/.dominions6/mods instead.

Your files, settings and errors
- Everything the editor keeps is in ~/.config/Dom5Editor:
      backups/      a copy of the mod's file when you open it and before each save (30 per mod;
                    the small arrow next to Load has "Backups of this mod...")
      recovery/     a copy of a mod with unsaved edits, if the editor fails
      errors.log    what went wrong, if something did: please send it with what you were doing
      settings.json the window, the list's width, recent mods, the game folder
- If a save doesn't read back the same, the file isn't replaced and the editor says why.
