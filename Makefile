MOD_NAME := Mishmash
VERSION ?= dev
DIST_DIR := dist

.PHONY: all clean build package package-windows package-linux

all: clean build package

clean:
	rm -rf $(DIST_DIR)

build:
	dotnet restore OpenRA.sln
	dotnet build OpenRA.sln -c Release

# Optional check; doesn't stop build if fails
check:
	@echo "Running OpenRA.Utility check (will not fail build)"
	- ./engine/bin/OpenRA.Utility.exe /run-all-rules || true

package: package-windows package-linux

package-windows:
	mkdir -p $(DIST_DIR)/windows/$(MOD_NAME)
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/windows/$(MOD_NAME)/
	cd $(DIST_DIR)/windows && zip -r $(MOD_NAME)-windows-$(VERSION).zip $(MOD_NAME)

package-linux:
	mkdir -p $(DIST_DIR)/linux/$(MOD_NAME)
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/linux/$(MOD_NAME)/
	cd $(DIST_DIR)/linux && zip -r $(MOD_NAME)-linux-$(VERSION).zip $(MOD_NAME)
