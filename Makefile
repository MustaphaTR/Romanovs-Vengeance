.PHONY: all clean check test package

RUNTIME ?= dotnet

MOD_SOLUTION_FILES := $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

all:
ifeq ($(RUNTIME), mono)
	@echo "Building using Mono"
	@command -v msbuild >/dev/null || (echo "Mono is not installed!"; exit 1)
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Build -restore -p:Configuration=Release -p:Mono=true \;
endif
else
	@echo "Building using .NET"
	@find . -maxdepth 1 -name '*.sln' -exec dotnet build -c Release \;
endif

clean:
ifneq ("$(MOD_SOLUTION_FILES)","")
ifeq ($(RUNTIME), mono)
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Clean \;
else
	@find . -maxdepth 1 -name '*.sln' -exec dotnet clean \;
endif
endif

check: all
	@echo "Skipping deep utility checks for CI"
	@echo "Mod built successfully."

test: all
	@echo "No test steps defined"

# ✅ Build Windows & Linux release packages
package: all
	@chmod +x packaging/package-all.sh packaging/windows/buildpackage.sh packaging/linux/buildpackage.sh
	@./packaging/package-all.sh
