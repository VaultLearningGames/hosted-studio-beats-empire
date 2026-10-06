# Beats Empire: Vault listing draft

A draft site listing for Beats Empire on the Vault website, for the **Teachers College, Columbia University** studio
(`tc-columbia`), CDN game `beats-empire`. `listing.json` has every field in the shape of the portal's listing
(`ListingFields` in VaultLearningGames/vault-publisher `src/listings.ts`); `images/` has the hero, thumbnail and
screenshots. The production portal's draft listing `beats-empire` was filled from these files (images copied to the
Vault CDN); it isn't on the site until Vault publishes it.

## Fields

| Field | Draft |
| --- | --- |
| Title | Beats Empire |
| Short description (123 chars; the catalog's median is ~145) | Run a music studio in a city full of data. Study what listeners want, sign artists, and record songs that climb the charts. |
| Made by | Teachers College, Columbia University; Filament Games (both are Vault studios) |
| Grades | Grades 5-8 (the game is designed for middle school) |
| Subjects | Information Technology, Math, Social Studies |
| Topics | Computer Science, Graphing, Business, Economics |
| Standards | `2-DA-07`, `2-DA-08`, `2-DA-09` (CSTA, the K-12 CS Framework's Data and Analysis strand, which the game's classroom guide names), `6.SP.B.5` |
| Related curriculum | https://info.beatsempire.org/use-in-your-classroom/ |
| Gameplay video | none (no trailer found; see below) |
| Play | the Vault CDN (`play_source: cdn`), once a release of `tc-columbia/beats-empire` is approved |

**About this game** is in `listing.json` (Markdown, ~1,400 characters): the premise, how data drives each song, four
"students will" bullets, the classroom time from the official guide (15–20 minutes, then 5–10; 4–6 sessions over 2–3
class periods), player codes, the partners, NSF funding and the awards.

Vault staff only: Beats Empire's interface has small text and dense charts. A minimum play area larger than the site's
880 × 525 default is worth trying, e.g. `min_width` 1024, `min_height` 640.

## Images

| File | What | Source |
| --- | --- | --- |
| `hero.jpg` (1600 × 900) | Title screen: logo and the band on stage | Captured from the Vault test build (`…/beats-empire/production/`) |
| `thumb.jpg` (800 × 450) | The same, smaller | Same |
| `screenshot-1-trends.jpg` | Trends: song listens by mood over 15 weeks (line graph) | Captured from the Vault test build |
| `screenshot-2-recording.jpg` | Recording a song for a target borough | [Filament Games project page](https://www.filamentgames.com/project/beats-empire) |
| `screenshot-3-results.jpg` | A release's results across the city map | Filament Games |
| `screenshot-4-top-charts.jpg` | This week's Top Charts | Filament Games |
| `screenshot-5-artists.jpg` | Managing an artist | Filament Games |

All 16:9, JPEG, 60–230 KB each. Filament's originals are 1919 × 1080 AVIF on their page; our captures were 1920 × 1080.

Before publishing, confirm with Filament Games (the developer) that the four screenshots from their portfolio page
can be used; ours can replace them otherwise (any screen can be captured from the test build). The game itself is
CC BY-NC-SA 4.0. Filament's header video (8.5 s) shows only the studio floor idling, so it isn't a gameplay
video.

## Sources

- Filament Games, [Beats Empire](https://www.filamentgames.com/project/beats-empire): description, learning focus,
  awards, screenshots
- [info.beatsempire.org](https://info.beatsempire.org/): [Use in your classroom](https://info.beatsempire.org/use-in-your-classroom/)
  (grades, standards, session lengths), [About us](https://info.beatsempire.org/about-us/) (team and partners), NSF
  awards 1742011 and 1741956
- Teachers College, [Easily teach data analysis with this free video game](https://www.tc.columbia.edu/articles/2020/march/easily-teach-data-analysis-with-this-free-video-game-/)
- Joan Ganz Cooney Center, [Exploring Data Science Through Video Games](https://joanganzcooneycenter.org/2020/09/08/exploring-data-science-through-video-games/)

## To put it on Vault

1. Request a release of a test build (e.g. `production`) in the [Vault Studio Portal](https://portal.vaultlearninggames.org)
   so the CDN game has something to play.
2. In the portal's listing editor for Teachers College, enter the fields above and upload the images (they're stored on
   the Vault CDN under `tc-columbia/beats-empire/_vault-assets/`).
3. Submit for review; Vault publishes it.
