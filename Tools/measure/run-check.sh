#!/bin/sh
# Runs one play-mode check from start to end (phase 15). From the project folder:
#     sh Tools/measure/run-check.sh <Scene> <file.cs> <state key> [max seconds]
# e.g. sh Tools/measure/run-check.sh Playground Tools/measure/camera.cs cam_state 120
# Stops play mode if on, opens Assets/Scenes/<Scene>.unity (unsaved scene edits are NOT
# kept - it refuses if the open scene is dirty), enters play mode, runs the file once,
# then polls the state key until it is no longer "running" (a check that polls itself,
# like screens.cs, is re-run instead: key "-"; a check that answers at once, like
# dungeon-play.cs: key "="). Leaves play mode at the end.
scene=$1; file=$2; key=$3; max=${4:-180}
u() { timeout 60 unity command "$@" < /dev/null 2>&1 | tail -1; }
res() { printf '%s\n' "$1" | node -e 'const t = require("fs").readFileSync(0, "utf8"); for (const f of t.split("\t")) { try { const j = JSON.parse(f); if (j && "result" in j) { console.log(String(j.result)); process.exit(0); } } catch (e) {} } console.log("(no result) " + t.slice(0, 1500));'; }
waitplay() { i=0; while [ $i -lt 60 ]; do r=$(u eval --code 'return UnityEditor.EditorApplication.isPlaying ? "is-on" : "is-off";'); echo "$r" | grep -q "\"result\":\"$1" && return 0; sleep 1; i=$((i+1)); done; echo "timed out waiting for $1"; return 1; }
u editor_stop > /dev/null
waitplay is-off || exit 1
r=$(u eval --code 'return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty ? "dirty-scene" : "clean";')
echo "$r" | grep -q '"result":"dirty-scene' && { echo "open scene has unsaved edits - save or revert it first"; exit 1; }
u eval --code "UnityEditor.SceneManagement.EditorSceneManager.OpenScene(\"Assets/Scenes/$scene.unity\"); return \"opened\";" > /dev/null
[ "$key" != "-" ] && [ "$key" != "=" ] && u eval --code "System.AppDomain.CurrentDomain.SetData(\"$key\", null); return \"cleared\";" > /dev/null
u editor_play > /dev/null
waitplay is-on || exit 1
sleep 2
r=$(timeout 60 unity command eval_file --file "$file" < /dev/null 2>&1 | tail -4 | tr '\n' ' ')
start=$(date +%s)
if [ "$key" = "=" ]; then
  res "$r"
elif [ "$key" = "-" ]; then
  while res "$r" | head -1 | grep -qv '^done'; do
    [ $(( $(date +%s) - start )) -gt "$max" ] && { echo "TIMED OUT"; break; }
    sleep 1; r=$(u eval_file --file "$file")
  done
  res "$r"
else
  res "$r" | head -3
  while :; do
    sleep 3
    r=$(u eval --code "var s = System.AppDomain.CurrentDomain.GetData(\"$key\") as string; return s == null ? \"no-state\" : s;")
    echo "$r" | grep -q '"result":"running' || break
    [ $(( $(date +%s) - start )) -gt "$max" ] && { echo "TIMED OUT (still running after $max s)"; break; }
  done
  res "$r"
fi
u editor_stop > /dev/null
waitplay is-off > /dev/null
# Checks that save point SaveFiles at a scratch folder; clear it so the user's own Play saves
# to the real folder again (a static survives play mode - trap 88). No Save folder: no harm.
u eval --code 'TopDownKit.SaveFiles.FolderOverride = null; return "cleared";' > /dev/null
# Checks switch the real keyboard / mouse off while they run; make sure they are back on.
u eval --code 'int n = 0; foreach (var d in UnityEngine.InputSystem.InputSystem.devices) if (!d.enabled && (d is UnityEngine.InputSystem.Keyboard || d is UnityEngine.InputSystem.Mouse)) { UnityEngine.InputSystem.InputSystem.EnableDevice(d); n++; } return "re-enabled " + n;' > /dev/null
