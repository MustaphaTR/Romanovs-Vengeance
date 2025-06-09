#!/bin/bash
set -e

MOD_ID=$1
VERSION=$2

if [ -z "$MOD_ID" ] || [ -z "$VERSION" ]; then
	echo "Usage: $0 <mod-id> <version>"
	exit 1
fi

ENGINE_DIR=$(awk -F= '/ENGINE_DIRECTORY/ { print $2; exit }' mod.config)
RELEASE_DIR="release"
PACKAGE_DIR="${RELEASE_DIR}/mishmash-windows-${VERSION}"
OUTPUT_ZIP="${RELEASE_DIR}/Mishmash-Windows.zip"

echo "🛠️  Building Windows package for '$MOD_ID' version '$VERSION'"

# Clean up old package directory
rm -rf "$PACKAGE_DIR"
mkdir -p "$PACKAGE_DIR"

# Copy required engine files
cp -r "$ENGINE_DIR/bin" "$PACKAGE_DIR/"
cp -r "$ENGINE_DIR/mods" "$PACKAGE_DIR/engine-mods"

# Copy only this mod's files
mkdir -p "$PACKAGE_DIR/mods"
cp -r "mods/${MOD_ID}" "$PACKAGE_DIR/mods/${MOD_ID}"

# Optional: remove other engine mods if not needed
rm -rf "$PACKAGE_DIR/engine-mods/mods/"*
mkdir -p "$PACKAGE_DIR/engine-mods/mods/${MOD_ID}"
cp -r "mods/${MOD_ID}" "$PACKAGE_DIR/engine-mods/mods/"

# Copy launchers
cp launch-game.sh "$PACKAGE_DIR/"
cp utility.sh "$PACKAGE_DIR/"
cp launch-dedicated.sh "$PACKAGE_DIR/" 2>/dev/null || true

# Create final zip
cd "$RELEASE_DIR"
zip -r "$(basename "$OUTPUT_ZIP")" "$(basename "$PACKAGE_DIR")"
cd ..

echo "✅ Windows standalone ZIP created at: $OUTPUT_ZIP"
