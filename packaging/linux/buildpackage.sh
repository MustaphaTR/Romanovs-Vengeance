#!/bin/bash
set -e

MOD_ID="rv"
ENGINE_DIR="./engine"
RELEASE_DIR="./release/linux"
ZIP_OUTPUT="./release/Mishmash-Linux.zip"

echo "🔧 Creating Linux release directory: $RELEASE_DIR"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR"

echo "📦 Copying engine files..."
cp -r "$ENGINE_DIR" "$RELEASE_DIR/engine"

echo "📦 Copying mod files..."
mkdir -p "$RELEASE_DIR/mods"
cp -r "./mods/$MOD_ID" "$RELEASE_DIR/mods/$MOD_ID"

cp -r ./mods/common-content "$RELEASE_DIR/mods/" 2>/dev/null || echo "(optional) common-content not found"
cp LICENSE "$RELEASE_DIR/" 2>/dev/null || true
cp README.md "$RELEASE_DIR/" 2>/dev/null || true

echo "📦 Zipping Linux release..."
mkdir -p release
(cd release && zip -r "Mishmash-Linux.zip" "linux")

echo "✅ Linux package created: $ZIP_OUTPUT"
