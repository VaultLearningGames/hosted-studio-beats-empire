// Saves stay in this browser's localStorage (no sign-in, no cloud saves); gameplay logging is off.
mergeInto(LibraryManager.library, {
  PersistFirebase: function(json) {
  },
  SaveData: function(json) {
    json = Pointer_stringify(json);
    try {
      window.localStorage.setItem('beats-empire/save', json);
    } catch (e) {
      console.log("[save] not saved:", e);
      return;
    }
    setTimeout(function() {
      SendMessage('Game Controller', 'IndicateSave');
    }, 0);
  },
  LoadData: function() {
    var save = null;
    try {
      save = window.localStorage.getItem('beats-empire/save');
    } catch (e) {
      console.log("[save] can't read saves:", e);
    }
    // "null" when there is no save, as the Firebase load sent.
    setTimeout(function() {
      SendMessage('Game Controller', 'LoadCallback', save == null ? 'null' : save);
    }, 0);
  },
  PendingSavesCount: function() {
    return 0;
  },
  FirebaseUserID: function() {
    var id = '';
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
