#!/bin/bash
# Builds WebGL locally the way CI does (Unity 2018.4.36f1 in game-ci's Docker image) into build/WebGL.
#
#   tools/unity-build.sh
#
# Needs Docker and a Unity license in $UNITY_LICENSE_ENV (default ~/.config/unity-license.env, mode 600) with
# UNITY_EMAIL, UNITY_PASSWORD and UNITY_SERIAL lines. The seat is activated for the build and always returned.
# Logs go to $UNITY_WORK/logs. Snap-packaged Docker can only mount folders under $HOME, so UNITY_WORK lives there.
set -euo pipefail
REPO=$(cd "$(dirname "$0")/.." && pwd)
UNITY_LICENSE_ENV=${UNITY_LICENSE_ENV:-$HOME/.config/unity-license.env}
UNITY_WORK=${UNITY_WORK:-$HOME/.cache/beats-empire-unity}
IMAGE=unityci/editor:ubuntu-$(sed -n 's/^m_EditorVersion: //p' "$REPO/ProjectSettings/ProjectVersion.txt")-webgl-3
mkdir -p "$UNITY_WORK/home" "$UNITY_WORK/logs"
rm -f "${UNITY_WORK:?}"/logs/*.log

docker run --rm --user "$(id -u):$(id -g)" -e HOME=/uhome \
  -v "$UNITY_WORK/home:/uhome" -v "$UNITY_WORK/logs:/logs" -v "$REPO:/project" \
  --env-file "$UNITY_LICENSE_ENV" "$IMAGE" bash -c '
    cd /project
    ret() { unity-editor -batchmode -nographics -quit -logFile /logs/return.log -returnlicense -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD"; echo "returned license: $?"; }
    trap ret EXIT
    unity-editor -batchmode -nographics -quit -logFile /logs/activate.log -serial "$UNITY_SERIAL" -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD"
    echo "activate: $?"
    # Opening the scenes first lets TextMesh Pro upgrade its font assets outside the build.
    unity-editor -batchmode -nographics -quit -logFile /logs/prepare.log -projectPath /project -executeMethod LocalVaultBuild.Prepare
    echo "prepare: $?"
    unity-editor -batchmode -nographics -quit -logFile /logs/build.log -projectPath /project -buildTarget WebGL -executeMethod LocalVaultBuild.Build
    echo "build: $?"'
grep -h "LOCAL BUILD RESULT\|error CS" "$UNITY_WORK/logs/build.log" || true
