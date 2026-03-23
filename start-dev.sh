#!/bin/bash
echo "Killing any existing API on port 7219 or 5289..."
PID=$(netstat -ano 2>/dev/null | grep ":7219" | grep "LISTENING" | awk '{print $5}' | head -1)
if [ -n "$PID" ]; then
  taskkill //F //PID $PID 2>/dev/null && echo "Killed PID $PID"
fi
sleep 2
echo "Starting API..."
cd /e/Projects/BdStockOMS/BdStockOMS.API
dotnet run --launch-profile https &
echo "Waiting for API to be ready on port 7219..."
for i in {1..30}; do
  sleep 2
  LISTENING=$(netstat -ano 2>/dev/null | grep ":7219" | grep "LISTENING")
  if [ -n "$LISTENING" ]; then
    echo "API is ready!"
    break
  fi
  echo "  waiting... ($((i*2))s)"
done
echo "Starting frontend..."
cd /e/Projects/BdStockOMS/BdStockOMS.Client
npm run dev
