# Makefile for Mishmash OpenRA SDK-based standalone packaging

VERSION ?= dev

.PHONY: all engine clean check-scripts check test version package

all: engine

engine:
	chmod +x fetch-engine.sh
	./fetch-engine.sh
	cd engine && make

clean:
	cd engine && make clean
	@find . -maxdepth 1 -name '*.sln' -exec dotnet clean -c Release \;

check-scripts:
	@luac -p $(shell find mods/*/maps/* -iname '*.lua' 2> /dev/null)

check: engine
	dotnet build -c Debug -warnaserror

test: all
	./utility.sh --check-yaml

version:
	sh -c '. $(ENGINE_DIRECTORY)/packaging/functions.sh; set_mod_version $(VERSION) "mods/rv/mod.yaml"'
	@echo "Version changed to $(VERSION)."

package:
	chmod +x packaging/package-all.sh packaging/windows/buildpackage.sh || true
	./packaging/package-all.sh $(VERSION)
