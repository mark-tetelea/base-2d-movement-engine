#!/bin/sh
# The removable-systems check (phase 14, vital). Runs Assets/Editor/RemovalCheck.cs in the
# open editor (edit mode) and prints its report. Run from the project folder:
#     sh Tools/measure/removal-check.sh
# Ends with "ALL PASS" or "FAILED". Nothing in Assets is changed; scratch assemblies go
# to Temp/RemovalCheck.
timeout 60 unity command eval --code 'TopDownKit.EditorTools.RemovalCheck.Start(); return "started";' < /dev/null > /dev/null 2>&1
i=0
while [ $i -lt 100 ]; do
  sleep 3
  r=$(timeout 30 unity command eval --code 'return TopDownKit.EditorTools.RemovalCheck.Running ? "still-going" : TopDownKit.EditorTools.RemovalCheck.Report;' < /dev/null 2>&1 | tail -1)
  echo "$r" | grep -q '"result":"still-going' || break
  i=$((i+1))
done
echo "$r" | grep -o '"result":"[^"]*' | sed 's/"result":"//; s/\\r\\n/\n/g; s/\\n/\n/g'
