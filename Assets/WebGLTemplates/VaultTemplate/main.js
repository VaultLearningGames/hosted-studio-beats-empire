// Player codes and saves, then the game.
//
// While the game loads, the player either types their player code (Continue) or gets a new one (I'm new). Saves
// follow Field Day's Aqualab pattern: every save is written to this browser first (keyed by player code), then sent
// to the player code service (OpenGameData's /player/CODE/game/BEATS_EMPIRE/state) with retries. Continuing loads
// both copies and plays the newer one, so a save made offline isn't lost, and the same code works on any computer.
// The service keeps about 64 KB per save, and a save is ~70 KB of JSON, so it is sent gzipped and base64-encoded.
// Unity reaches this through window.BeatsSaves (Assets/Plugins/bridge.jslib).

var PLAYER_API = 'https://fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/';
var GAME_ID = 'BEATS_EMPIRE';
var SERVER_PREFIX = 'BE1:';          // "BE1:" + base64(gzip(JSON of { savedAt, save }))
var LAST_CODE_KEY = 'beats-empire/last-code';
var LOCAL_SAVE_KEY = 'beats-empire/save/';   // + player code, or "none" when playing without one
var UPLOAD_ATTEMPTS = 4;
var SERVER_MAX = 60000;               // the service fails above ~64 KB; larger saves stay in this browser

var gameInstance = null;
var runtimeReady = false;
var splashContainer = document.getElementById('splash-container');
var panel = document.getElementById('panel');
var gameContainer = document.getElementById('game-container');
var progressBarContainer = document.getElementById('progress-bar-container');
var progressBar = document.getElementById('progress-bar');
var loadingText = document.getElementById('loading-text');
var codePanel = document.getElementById('code-panel');
var codeInput = document.getElementById('code-input');
var continueButton = document.getElementById('continue-button');
var newPlayerButton = document.getElementById('new-player-button');
var codeMessage = document.getElementById('code-message');
var playPrompt = document.getElementById('play-prompt');
var codeBanner = document.getElementById('code-banner');
var playButton = document.getElementById('play-button');
var codeTag = document.getElementById('code-tag');

// ---------- storage helpers ----------

function storageGet(key) {
  try { return window.localStorage.getItem(key); } catch (e) { return null; }
}

function storageSet(key, value) {
  try { window.localStorage.setItem(key, value); return true; } catch (e) {
    console.log('[save] not saved in this browser:', e);
    return false;
  }
}

function canGzip() {
  return typeof CompressionStream !== 'undefined' && typeof DecompressionStream !== 'undefined';
}

function bytesToBase64(bytes) {
  var s = '';
  for (var i = 0; i < bytes.length; i += 0x8000) {
    s += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
  }
  return btoa(s);
}

function base64ToBytes(b64) {
  var s = atob(b64);
  var bytes = new Uint8Array(s.length);
  for (var i = 0; i < s.length; i++) bytes[i] = s.charCodeAt(i);
  return bytes;
}

function gzipToBase64(text) {
  var stream = new Blob([text]).stream().pipeThrough(new CompressionStream('gzip'));
  return new Response(stream).arrayBuffer().then(function(buf) { return bytesToBase64(new Uint8Array(buf)); });
}

function gunzipFromBase64(b64) {
  var stream = new Blob([base64ToBytes(b64)]).stream().pipeThrough(new DecompressionStream('gzip'));
  return new Response(stream).text();
}

// A record is { savedAt: ms since 1970, save: the game's save JSON string }.
function readLocalRecord(code) {
  var raw = storageGet(LOCAL_SAVE_KEY + code);
  if (!raw) return null;
  try { return JSON.parse(raw); } catch (e) { return null; }
}

function normalizeCode(text) {
  return (text || '').replace(/\s+/g, '');
}

// ---------- player code service ----------

function api(path, options) {
  return fetch(PLAYER_API + path, options).then(function(response) {
    return response.json().catch(function() {
      throw new Error('player code service: HTTP ' + response.status);
    });
  });
}

function stateUrl(code) {
  return encodeURIComponent(code) + '/game/' + GAME_ID + '/state';
}

function newPlayerCode() {
  return api('', { method: 'GET' }).then(function(r) {
    if (r.status !== 'SUCCESS' || !r.val || !r.val[0]) throw new Error(r.msg || 'no code');
    var code = r.val[0];
    return api('?player_id=' + encodeURIComponent(code) + '&name=', { method: 'PUT' }).then(function(claim) {
      if (claim.status !== 'SUCCESS') throw new Error(claim.msg || 'code not claimed');
      return code;
    });
  });
}

// The newest record on the server, null when the player has none, or an error when the service can't be reached.
function fetchServerRecord(code) {
  return api(stateUrl(code), { method: 'GET' }).then(function(r) {
    if (r.status !== 'SUCCESS' || !r.val || !r.val[0]) return null;
    var state = r.val[0];
    if (state.indexOf(SERVER_PREFIX) !== 0) return null;
    if (!canGzip()) throw new Error('this browser cannot unpack saves');
    return gunzipFromBase64(state.substring(SERVER_PREFIX.length)).then(JSON.parse);
  });
}

function uploadRecord(code, record) {
  return gzipToBase64(JSON.stringify(record)).then(function(packed) {
    if (packed.length > SERVER_MAX) {
      var tooBig = new Error('save too large for the server (' + packed.length + ' chars); kept in this browser');
      tooBig.final = true;
      throw tooBig;
    }
    return api(stateUrl(code), {
      method: 'POST',
      headers: { 'Content-Type': 'text/plain' },
      body: SERVER_PREFIX + packed
    });
  }).then(function(r) {
    if (r.status !== 'SUCCESS') throw new Error(r.msg || 'not saved');
  });
}

// ---------- saves, as the game sees them ----------

var playerCode = null;       // null: playing without a code (saves stay in this browser)
var currentRecord = null;    // the save the game loads
var pendingUploads = 0;
var latestUpload = null;     // only the newest save is uploaded; older queued ones are skipped

function uploadWithRetries(code, record, attempt) {
  attempt = attempt || 1;
  if (latestUpload !== record) return Promise.resolve();
  return uploadRecord(code, record).then(function() {
    console.log('[save] saved to the player code service');
  }, function(e) {
    console.log('[save] upload failed (attempt ' + attempt + '):', e);
    if (attempt >= UPLOAD_ATTEMPTS || e.final) return;
    return new Promise(function(resolve) { setTimeout(resolve, 2000 * attempt); }).then(function() {
      return uploadWithRetries(code, record, attempt + 1);
    });
  });
}

function upload(code, record) {
  if (!canGzip()) return;
  latestUpload = record;
  ++pendingUploads;
  uploadWithRetries(code, record).then(function() { --pendingUploads; });
}

window.BeatsSaves = {
  // The save the game should load: its JSON, or "null" for none (as the Firebase load sent).
  load: function() {
    return currentRecord ? currentRecord.save : 'null';
  },
  save: function(json) {
    var record = { savedAt: Date.now(), save: json };
    currentRecord = record;
    var saved = storageSet(LOCAL_SAVE_KEY + (playerCode || 'none'), JSON.stringify(record));
    if (playerCode) upload(playerCode, record);
    return saved;
  },
  pending: function() {
    return pendingUploads;
  },
  code: function() {
    return playerCode || '';
  }
};

// Warn if the player tries to leave while a save is still being sent.
window.addEventListener('beforeunload', function(e) {
  if (pendingUploads > 0) {
    e.preventDefault();
    e.returnValue = '';
  }
});

// ---------- choosing a code ----------

function setMessage(text, isError) {
  codeMessage.textContent = text;
  codeMessage.className = isError ? 'error' : '';
}

function setBusy(busy) {
  codeInput.disabled = busy;
  continueButton.disabled = busy;
  newPlayerButton.disabled = busy;
}

function escapeHtml(s) {
  return s.replace(/[&<>"']/g, function(c) {
    return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
  });
}

function choose(code, record, banner) {
  playerCode = code;
  currentRecord = record;
  if (code) storageSet(LAST_CODE_KEY, code);
  codePanel.style.display = 'none';
  codeBanner.innerHTML = banner;
  playPrompt.style.display = 'block';
  updatePlayButton();
}

codePanel.onsubmit = function(e) {
  e.preventDefault();
  var code = normalizeCode(codeInput.value);
  if (!code) {
    setMessage('Type your player code, or choose "I\'m new".', true);
    return;
  }
  setBusy(true);
  setMessage('Looking up ' + code + '…');
  var local = readLocalRecord(code);
  fetchServerRecord(code).then(function(server) {
    var localIsNewer = local && (!server || local.savedAt > server.savedAt);
    var record = localIsNewer ? local : server;
    if (!record) {
      setBusy(false);
      setMessage('No Beats Empire game saved for ' + code + '. Check the spelling, or choose "I\'m new".', true);
      return;
    }
    // Give the server this browser's newer copy (e.g. one saved while offline).
    if (localIsNewer) upload(code, local);
    choose(code, record, 'Welcome back! Your player code is<b>' + escapeHtml(code) + '</b>');
  }, function(e) {
    console.log('[save] could not reach the player code service:', e);
    setBusy(false);
    if (local) {
      choose(code, local, 'Can\'t reach the save server, so this computer\'s copy of <b>' + escapeHtml(code) +
        '</b> will be used. Your progress is sent with your next save once the server is back.');
    } else {
      setMessage('Can\'t reach the save server right now. Check your connection and try again.', true);
    }
  });
};

function addPlayWithoutCode() {
  if (document.getElementById('no-code-button')) return;
  var noCode = document.createElement('input');
  noCode.type = 'button';
  noCode.id = 'no-code-button';
  noCode.className = 'button';
  noCode.value = 'Play without a code';
  noCode.style.marginTop = '10px';
  noCode.onclick = function() {
    choose(null, readLocalRecord('none'), 'Playing without a player code: your game saves on this computer only.');
  };
  codePanel.appendChild(noCode);
}

newPlayerButton.onclick = function() {
  setBusy(true);
  setMessage('Getting you a player code…');
  newPlayerCode().then(function(code) {
    choose(code, null, 'Your player code is<b>' + escapeHtml(code) +
      '</b>Write it down: you need it to continue your game later.');
  }, function(e) {
    console.log('[save] could not get a player code:', e);
    setBusy(false);
    setMessage('Can\'t get a player code right now. Try again, or play without one (your game saves on this computer only).', true);
    addPlayWithoutCode();
  });
};

codeInput.value = storageGet(LAST_CODE_KEY) || '';

// ---------- loading and starting the game ----------

function setProgress(proportion) {
  progressBar.style.width = Math.round(proportion * 100) + '%';
}

function updatePlayButton() {
  playButton.disabled = !runtimeReady;
  playButton.value = runtimeReady ? 'Play' : 'Loading…';
}

function sendConnectionStatus() {
  if (gameInstance != null) {
    gameInstance.SendMessage('Session', 'OnConnectionChange', navigator.onLine ? 1 : 0);
  }
}

window.addEventListener('online', sendConnectionStatus);
window.addEventListener('offline', sendConnectionStatus);

// The click also lets the browser start the game's audio.
function toInGame() {
  splashContainer.style.display = 'none';
  panel.style.display = 'none';
  gameContainer.style.visibility = 'visible';
  if (playerCode) {
    codeTag.textContent = 'Player code: ' + playerCode;
    codeTag.style.display = 'block';
  }
  gameInstance.SendMessage('Session', 'OnVisible');
  sendConnectionStatus();
}

playButton.onclick = toInGame;

setProgress(0);
updatePlayButton();
gameInstance = UnityLoader.instantiate('game-container', buildUrl, {
  onProgress: function(gameInstance, proportion) {
    setProgress(proportion);
  },
  Module: {
    noInitialRun: false,
    onRuntimeInitialized: function() {
      runtimeReady = true;
      progressBarContainer.style.display = 'none';
      loadingText.style.display = 'none';
      updatePlayButton();
    }
  }
});
