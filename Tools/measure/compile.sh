#!/bin/sh
# Recompiles the open editor and prints compile errors (or "0 errors").
# Run from the project folder: sh Tools/measure/compile.sh
timeout 30 unity command clear_console < /dev/null > /dev/null 2>&1
timeout 60 unity command recompile < /dev/null > /dev/null 2>&1
sleep 3
i=0
while [ $i -lt 60 ]; do
  r=$(timeout 30 unity command console_status < /dev/null 2>&1 | tail -1)
  echo "$r" | grep -q '"compiling":false' && break
  sleep 3; i=$((i+1))
done
if echo "$r" | grep -q '"compilationFailed":true'; then
  timeout 30 unity command console --level error < /dev/null 2>&1 | grep -o '"message":"[^"]*' | sed 's/"message":"//' | sort -u
else
  echo "0 errors"
fi
