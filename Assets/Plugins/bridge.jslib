// Saves go through window.BeatsSaves (Assets/WebGLTemplates/VaultTemplate/main.js): this browser first, then the
// player code service. Gameplay logging is off.
mergeInto(LibraryManager.library, {
  PersistFirebase: function(json) {
  },
  SaveData: function(json) {
    json = Pointer_stringify(json);
    if (!window.BeatsSaves.save(json)) return;
    setTimeout(function() {
      SendMessage('Game Controller', 'IndicateSave');
    }, 0);
  },
  LoadData: function() {
    var save = window.BeatsSaves.load();
    setTimeout(function() {
      SendMessage('Game Controller', 'LoadCallback', save);
    }, 0);
  },
  PendingSavesCount: function() {
    return window.BeatsSaves.pending();
  },
  FirebaseUserID: function() {
    var id = window.BeatsSaves.code();
    var bufferSize = lengthBytesUTF8(id) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(id, buffer, bufferSize);
    return buffer;
  },
  FirebaseUserEmail: function() {
    var email = '';
    var bufferSize = lengthBytesUTF8(email) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(email, buffer, bufferSize);
    return buffer;
  }
});
