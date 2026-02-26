#!/bin/bash

# Detect Python
if command -v python3 &>/dev/null; then
    PY=python3
elif command -v python &>/dev/null; then
    PY=python
else
    echo "Error: Python is not installed."
    read -p "Press enter to exit..."
    exit 1
fi

echo "Starting Women's Health App Server..."
echo "The app will open in your default browser at http://127.0.0.1:8080"

# Open browser based on OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    xdg-open http://127.0.0.1:8080 2>/dev/null &
elif [[ "$OSTYPE" == "darwin"* ]]; then
    open http://127.0.0.1:8080 &
fi

$PY server.py
if [ $? -ne 0 ]; then
    echo ""
    echo "Server stopped unexpectedly."
    read -p "Press enter to exit..."
fi
