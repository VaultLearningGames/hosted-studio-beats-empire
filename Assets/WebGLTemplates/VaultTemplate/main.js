// No sign-in: loads the game, then Play starts it. Saves stay in this browser (Assets/Plugins/bridge.jslib).

var gameInstance = null;
var splashContainer = document.getElementById('splash-container');
var gameContainer = document.getElementById('game-container');
var progressBarContainer = document.getElementById('progress-bar-container');
var progressBar = document.getElementById('progress-bar');
var playPromptContainer = document.getElementById('play-prompt-container');
var playButton = document.getElementById('play-button');

function setProgress(proportion) {
  progressBar.style.width = Math.round(proportion * 100) + '%';
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
  playPromptContainer.style.display = 'none';
  gameContainer.style.visibility = 'visible';
  gameInstance.SendMessage('Session', 'OnVisible');
  sendConnectionStatus();
}

playButton.onclick = toInGame;

setProgress(0);
progressBarContainer.style.display = 'block';
gameInstance = UnityLoader.instantiate('game-container', buildUrl, {
  onProgress: function(gameInstance, proportion) {
    setProgress(proportion);
  },
  Module: {
    noInitialRun: false,
    onRuntimeInitialized: function() {
      progressBarContainer.style.display = 'none';
      playPromptContainer.style.display = 'block';
    }
  }
});
