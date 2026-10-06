# external-beats-empire
 Beats Empire build for others to use

## Vault

GitHub Actions (`.github/workflows/vault.yml`): every push builds WebGL with Unity 2018.4.36f1 and publishes it to
[Vault](https://vaultlearninggames.org) as the Teachers College, Columbia University studio, at
`https://builds.vaultlearninggames.org/tc-columbia/beats-empire/<branch or tag>/`. Deleting a branch removes its
preview. To release, push a version tag, test it, then *Request release* in the
[Vault Studio Portal](https://portal.vaultlearninggames.org). The Unity license comes from the `UNITY_EMAIL`,
`UNITY_PASSWORD` and `UNITY_SERIAL` repository secrets.

The WebGL page (`Assets/WebGLTemplates/VaultTemplate`) has no sign-in: it loads the game and a Play button starts it.
Saves are kept in the browser's localStorage (`Assets/Plugins/bridge.jslib`), so Continue works on the same computer
only, and gameplay logging is off.

Shield: [![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]

This work is licensed under a
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License][cc-by-nc-sa].

[![CC BY-NC-SA 4.0][cc-by-nc-sa-image]][cc-by-nc-sa]

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg