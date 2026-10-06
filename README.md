# Beats Empire
A GEE! Award winning computer science and data usage game.
See https://info.beatsempire.org/
National Science Foundation under Grant No. 1742011 & 1741956
Beats Empire is licensed as CC BY-NC-SA 4.0.


## Building and Publishing to Vault

GitHub Actions (`.github/workflows/vault.yml`): every push builds WebGL with Unity 2018.4.36f1 and publishes it to
[Vault](https://vaultlearninggames.org) as the Teachers College, Columbia University studio, at
`https://builds.vaultlearninggames.org/tc-columbia/beats-empire/<branch or tag>/`. Deleting a branch removes its
preview. To release, push a version tag, test it, then *Request release* in the
[Vault Studio Portal](https://portal.vaultlearninggames.org). The Unity license comes from the `UNITY_EMAIL`,
`UNITY_PASSWORD` and `UNITY_SERIAL` repository secrets.

Branches: work goes to `develop` (test build at `…/tc-columbia/beats-empire/develop/`), and `develop` is merged into
`production` (`…/production/`) for the version teachers will get. Neither branch asks Vault for a release; that's a
*Request release* in the portal, when Teachers College decides to publish.

The WebGL page (`Assets/WebGLTemplates/VaultTemplate`) has no sign-in. While the game loads, players type their
player code to continue, or get a new one from the player code service
(`fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/`). Every save is written to the browser's
localStorage first, then sent (gzipped, base64, prefixed `BE1:`) to `/player/CODE/game/BEATS_EMPIRE/state`, with
retries; continuing loads both copies and plays the newer one, so a code works on any computer. Gameplay logging is
off. The WebGL build is gzip-compressed.

To build and test locally: `tools/unity-build.sh` builds WebGL into `build/WebGL` the way CI does (Docker and a Unity
license; see the script), and `tools/test/player-codes.mjs` plays a build in headless Chromium to check player codes
and saves (see the file for setup).

## OpenGameData logging: event inventory and mapping

Logging is off in this build. This section lists what the game already logs and suggests how each record maps to
the [OpenGameData Event Standard v1.0](https://opengamedata-doc.readthedocs.io/en/latest/appendices/event_standard/index.html),
as the plan for sending events to OpenGameData (app ID `BEATS_EMPIRE`).

### How the game logs today

Every record comes from one function, `GameRefs.PostGameState(includeAllData, triggerAction, actionValue)`
(`Assets/Scripts/VariableContainers/GameRefs.cs`), which only runs in WebGL builds. It builds one JSON object and
hands it to `Persister.Persist` → `PersistFirebase` in `Assets/Plugins/bridge.jslib`, which now does nothing (it
used to push to Firebase `users/<uid>/logs`). There are two ways to call it:

* **44 calls in code**, each naming its own `triggerAction` and `actionValue` (listed below).
* **The `RestLogAction` component on 120 UI objects** in the game's views. A click logs
  `triggerAction = "clickedButton"`, `actionValue = <the GameObject's name>`. Another 19 sit on the debug menu
  (`Prefabs/UI/DebugMenu.prefab`) and one in `Scenes/MeganSandbox.unity`; those aren't part of play.

So the existing log is already **per event**: one record per player action or game event. Each record, though,
carries a copy of the game state, and that copy is large when `includeAllData` is true:

| Always included | Only when `includeAllData` is true ("verbose") |
| --- | --- |
| `GameVersion` ("Version 1.04"), `CMSLogVersion` ("1.11"), `lastChangedDate`, `isLogVerbose`, `epochTime`, `realTimeUTC`, `upTimeSeconds`, `saveGameStartDate`, `gameSeed`, `playerUniqueID` (MD5 of the e-mail), `userEmail`; `triggerAction`, `actionValue`, `currentTurn`, `currentCash`, `recordingsInProgress`, `currentFans`, `currentScreen` | Device and system: `deviceModel`, `deviceName`, `deviceType`, `deviceUniqueIdentifier`, memory, OS, battery, processor, graphics device. Game: last week's storage costs, residual cash, band upkeep and sales; every signed band (with its recording song) and unsigned band; unconfirmed, confirmed and past marketing insights; marketing unlock levels per borough and genre; every released song's chart record; gold/platinum status per genre; and the latest market value of every genre, mood and topic in each of 6 locations |

### Suggested envelope (every event)

| OGD v1.0 parameter | From |
| --- | --- |
| `game_id` | `BEATS_EMPIRE` |
| `player_id` (`user_id` in the logger) | the player code (`BeatsSaves.code()`), or empty when playing without one |
| `session_id`, `timestamp`, `session_sequence_index` | set by the OGD logger |
| `game_version` / `source_version` | `PlayerInformation.versionNum` as SemVer (`1.04` → `1.4.0`) |
| `log_version` | `2` (the Firebase log was `CMSLogVersion` 1.11) |
| `schema_version` | `1.0` |
| `event_name` | the suggested name in the tables below |
| `event_data` | the event's own details (below), plus `event_code` (the standard's code) and the original `trigger_action` / `action_value` so old and new logs line up |
| `game_state` | a small context object on every event: `turn`, `cash`, `recordings_in_progress`, `fans`, `screen` (today's always-included game fields) |
| `platform` | once per session (session start), not per event: device type, OS, browser, graphics device, memory |

Drop from the logs: `userEmail`, `playerUniqueID` (both empty without sign-in, and identifying if not),
`deviceName` and `deviceUniqueIdentifier`. Don't send the verbose snapshot with every event: it repeats on every
click and is tens of KB. Send the full state once per week instead, with `advance_week` (below).

The OGD Unity logger's `1.0` mode sends `event_name` but has no numeric `event_id` field yet. That's why
`event_data.event_code` carries the code until the logger does.

### Events logged in code (44 calls)

| Where (`Assets/Scripts/…`) | `triggerAction` / `actionValue` today | Verbose | Suggested code and name | `event_data` |
| --- | --- | --- | --- | --- |
| `GameController.cs:226` | `autoEvent` / `startedGame` | yes | **3000** start global segment `start_game` (new game) or **3003** resume global segment `resume_game` (Continue): today both log `startedGame` | `is_new_game`, `seed` |
| `GameController.cs:341, 346` | `clickedButton` / `nextWeekRecording` | no | **4406** advance time `click_next_week` | `allowed: false`, `reason: "need_to_release" \| "need_to_record"` (the game opens the recording view instead) |
| `GameController.cs:355` | `clickedButton` / `NextWeekButton` | yes | **4406** advance time `click_next_week` | `allowed: true` |
| `UI/GameOverControl.cs:66` | `autoEvent` / `gameLose_noCash` | yes | **3001** end global segment `end_game` | `outcome: "out_of_cash"` (the game's rule, not an inference) |
| `UI/GameOverControl.cs:83–119` | `autoEvent` / `gameWin_generalization`, `gameWin_electronic`, `_hiphop`, `_pop`, `_rnb`, `_rap`, `_rock` | yes | **3001** end global segment `end_game` | `outcome: "win"`, `win_type: "generalization" \| <genre>` |
| `Utilities/TutorialController.cs:103` | `spawnTutorial` / `<id>_<TutorialID>` | no | **2201** enter tutorial `show_tutorial` | `tutorial_id`, `tutorial_name` |
| `UI/ArtistSigningView.cs:113`, `UI/Release.cs:198` | `tooltipHovered` / `<skill box name>` | no | **4204** inspect object `hover_tooltip` | `tooltip: <skill>`, `screen` |
| `UI/RecordingSlotsTooltipControl.cs:217` | `tooltipHovered` / `recordingSlots` | no | **4204** inspect object `hover_tooltip` | `tooltip: "recording_slots"` |
| `UI/CurrentCashTooltipControl.cs:69` | `tooltipHovered` / `cashFlow` | no | **4204** inspect object `hover_tooltip` | `tooltip: "cash_flow"` |
| `UI/ArtistSigningView.cs:432` | `clickedButton` / `SignArtistButton` | yes | **4500** buy `sign_artist` | the artist (name, genre, skills, cost) |
| `UI/ArtistSigningView.cs:478` | `clickedButton` / `FiringConfirmationButton` | yes | **4506** discard resource `release_artist` | the artist |
| `UI/ArtistSigningPanel.cs:251` | `clickedButton` / `ConfirmUpgradeButton` | yes | **4508** convert resource `upgrade_artist` | the artist, the upgraded skill or trait, cost |
| `UI/SongRecordingPanel.cs:419` | `clickedButton` / `RecordButton` | yes | **4604** schedule action `record_song` | artist, genre, mood, topic, location, attached insight |
| `UI/MarketingInsights.cs:606` | `clickedButton` / `MostPopular` or `TrendingUp` | yes | **4000** submit response `make_prediction` | `prediction: "most_popular" \| "trending_up"`, trait, location, artist (the insight the game records) |
| `UI/MarketingInsights.cs:467` | `clickedButton` / `deleteInsights` | yes | **4001** cancel submission `delete_prediction` | the prediction removed |
| `UI/Release.cs:582` | `clickedButton` / `CancelCurrentInsight` | yes | **4001** cancel submission `delete_prediction` | the prediction removed, artist |
| `UI/MarketingInsights.cs:469` | `clickedButton` / `cancelInsights` | no | **4707** cancel selection `cancel_prediction` | none |
| `UI/MarketingInsights.cs:245, 262` | `clickedBarOrLine` / `<mood or topic name>` | no | **4705** select item `select_graph_trait` | `trait_type: "mood" \| "topic"`, `trait` |
| `UI/MarketingInsights.cs:810` | `clickedButton` / `PlanSongButton` | yes | **4702** close interface `close_trends` (back to recording) | none |
| `UI/GraphManager.cs:1121–1128` | `setTimeDropdown` / `5Weeks`, `15Weeks`, `30Weeks` | no | **4708** switch view `set_graph_range` | `weeks: 5 \| 15 \| 30` |
| `UI/GraphManager.cs:227` | `clickedButton` / `cancelButton` | no | **4707** cancel selection `cancel_prediction` | none |
| `UI/DataCollectionController.cs:94` | `clickedButton` / `BuyStorageButton` | yes | **4500** buy `buy_storage` | slots after, cost |
| `UI/DataCollectionController.cs:140` | `changedSlider` / `<Genre\|Mood\|Topic>_toSampling_<n>` | no | **4507** allocate resource `set_data_sampling` | `trait_type`, `slots` |
| `UI/DataCollectionController.cs:187` | `clickedButton` / `closeDataManagement` | yes | **4702** close interface `close_data_management` | none |
| `UI/MarketingView.cs:349, 380, 416` | `autoEvent` / `unlockedMarketingUpgrade` | yes | **4500** buy `buy_marketing_upgrade` (the player buys it, though it logs as `autoEvent`) | `kind: "borough" \| "genre"`, which one, card, level, cost |
| `UI/Release.cs:328`, `UI/TopChartsView.cs:128`, `UI/GraphManager.cs:241`, `UI/ArtistSigningView.cs:136`, `UI/MarketingView.cs:164` | `clickedButton` / `backButton` | no | **4704** return to previous screen `click_back` | `from_screen` |

### Events from `RestLogAction` (clicks on 120 UI objects)

Today every one of these is `clickedButton` with the object's name. Suggested: map names to events with a lookup
table (a component field or a dictionary keyed by view and object name), keeping the object name in
`event_data.target`.

| View (prefab) | Objects | Suggested code and name |
| --- | --- | --- |
| Office (`OfficeView`) | `MarketingButton`, `RecordingButton`, `SigningButton` | **4703** navigate to screen `open_screen` (`screen`) |
| HUD | `TopCharts`, `TrendsButton` | **4703** navigate to screen `open_screen` |
| HUD, Game ending | `ReturnToTitleButton` | **1204** select menu option `return_to_title` |
| HUD | `MuteButton` | **1301** change setting `toggle_mute` (`muted`) |
| Recording (`RecordingView`, 22) | `Artist Selector Prefab` (prefab) | **4705** select item `select_artist` |
| | `Mood_1`–`Mood_6`; `Courage`, `Friendship`, `Hope`, `Love`, `Respect`, `Success` (topics); `Brower`, `Gorman`, `Ironwood`, `Morris`, `Turtlehill`, `Uptown` (locations) | **4705** select item `select_song_trait` (`trait_type`, `trait`) |
| | `GenerateSongTitle` | **4107** modify object `reroll_song_title` |
| | `MarketingInsightButton` | **4701** open interface `open_trends` |
| | `ReleaseFinishedSongButton`, `ReleaseEarlyButton` | **4000** submit response `release_song` (`early: false \| true`, song) |
| Signing (`SigningView`, 24) | `ViewApplicantsButton` | **4703** navigate to screen `open_screen` |
| | `NextButton`, `PreviousButton` | **4705** select item `browse_artists` (`direction`) |
| | `UpgradeButton` | **4701** open interface `open_artist_upgrade` |
| | `AmbitionUpgradeToggle`, `PersistenceUpgradeToggle`, `ReliabilityUpgradeToggle`, `SpeedUpgradeToggle`, `UpgradeToggleMood1`–`6`, `UpgradeToggleTopic1`–`6` | **4705** select item `select_upgrade` (`upgrade`) |
| | `CancelUpgrade`, `FiringCancelButton` | **4707** cancel selection |
| | `EndContractButton`, `Swap_EndContractButton` | **4705** select item `select_end_contract` (confirmed by `release_artist`) |
| Marketing (`MarketingView`, 14) | `BoroughUnlocks`, `GenreUnlocks` | **4708** switch view `marketing_tab` |
| | `MarketingBorough*` (6), `Electronic`, `HipHop`, `Pop`, `Rap`, `RnB`, `Rock` | **4705** select item `select_marketing_card` |
| Trends (`TrendsViewRevised`, 41) | `Bar`, `Line`, `Heatmap` | **4708** switch view `set_graph_type` |
| | `Genre`, `Mood`, `Topic` | **4708** switch view `set_graph_trait_type` |
| | `ButtonNextWeek`, `ButtonPreviousWeek` | **4708** switch view `step_graph_week` (`direction`) |
| | Genres (`AllGenre`, `Electronic`, `HipHop`, `Pop`, `RB`, `Rap`, `Rock`), moods (`Angry`, `Chill`, `Determined`, `Nostalgic`, `Sad`, `Upbeat`), topics (`Courage`, `Friendship`, `Hope`, `Love`, `Respect`, `Success`) | **4709** toggle overlay `toggle_graph_series` (`trait`) |
| | `*Selected`, `*Color` for the 6 locations | **4709** toggle overlay `toggle_graph_location` (`location`) |
| | `ButtonAddInsight` | **4701** open interface `open_prediction` |
| | `ManageDataButton` | **4701** open interface `open_data_management` |
| Data (`ManageDataView`) | `CloseButton` | **4702** close interface (duplicates `close_data_management`; keep one) |
| Top charts (`TopChartsView`, 7) | `Electronic`, `HipHop`, `Pop`, `Rap`, `RnB`, `Rock` | **4708** switch view `chart_genre` |
| | `ContinueButton` (verbose) | **4702** close interface `close_charts` |
| Song released (`SongReleaseView`) | `ContinueButton` | **4702** close interface `close_release_results` |
| Lost connection (`LostInternet`) | `FiringCancelButton` (reused name) | **4702** close interface `dismiss_connection_warning` |

### Gaps: things the game doesn't log yet

| Suggested code and name | When | `event_data` |
| --- | --- | --- |
| **1100** session started `session_start` / **1103** `session_end` | Play clicked / page closed | `platform` (once), how the code was chosen (`new`, `continue`, `no_code`) |
| **1204** select menu option `title_new_game`, `title_continue`, `title_credits` | title screen buttons (`TitleScreenControl`) | none |
| **7304** advance simulation `advance_week` | each new week, after the turn is simulated | the full verbose snapshot today's log sends: cash in/out, artists, recordings, predictions, unlocks, market values |
| **7400** reveal information `song_results` | a released song's sales and chart position are shown | song, sales, chart position, prediction outcome as the game scores it |
| **2211** complete tutorial `complete_tutorial` | `TutorialController.SetTutorialCompleted` | `tutorial_id` |
| **4703** navigate to screen `open_screen` | every change of `m_globalLastScreen`, so screens reached without a logged button still show | `screen`, `from_screen` |

Phase 3 changes `PostGameState` to send OGD events with this mapping. Either the
[OGD Unity logger](https://github.com/opengamedata/opengamedata-unity) (the project now uses the .NET 4.x runtime
it needs) or the JavaScript logger in the page can do the sending. Nothing about player codes or saves changes.

Shield: [![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]

This work is licensed under a
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License][cc-by-nc-sa].

[![CC BY-NC-SA 4.0][cc-by-nc-sa-image]][cc-by-nc-sa]

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg
