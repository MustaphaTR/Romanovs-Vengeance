#!/bin/bash
set -e
MOD_ID="mishmash"
ENGINE_DIR="./engine"
RELEASE_DIR="./release/windows"
ZIP_OUTPUT="./release/Mishmash-Windows.zip"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR/mods"
cp -r "$ENGINE_DIR" "$RELEASE_DIR/engine"
cp -r "./mods/$MOD_ID" "$RELEASE_DIR/mods/$MOD_ID"
(cd release && zip -r "Mishmash-Windows.zip" "windows")
echo "✅ Windows package created: $ZIP_OUTPUT"