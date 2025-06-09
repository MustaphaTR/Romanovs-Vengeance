.PHONY: all clean check test package

# Default toolchain
RUNTIME ?= dotnet

# Auto-detect solution files
MOD_SOLUTION_FILES := $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

# Default build target
all: engine
ifeq ($(RUNTIME), mono)
	@echo "Using Mono to build..."
	@command -v msbuild >/dev/null || (echo "Mono is not installed!"; exit 1)
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Build -restore -p:Configuration=Release -p:Mono=true \;
endif
else
	@echo "Using .NET to build..."
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec dotnet build -c Release \;
endif
endif

# Fetch engine if needed
engine:
	@echo "Fetching engine..."
	@./fetch-engine.sh || (echo "Failed to fetch engine"; exit 1)

# Clean build output
clean:
	@echo "Cleaning mod and engine..."
ifneq ("$(MOD_SOLUTION_FILES)","")
ifeq ($(RUNTIME), mono)
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Clean \;
else
	@find . -maxdepth 1 -name '*.sln' -exec dotnet clean \;
endif
endif
	@cd engine && make clean || echo "No engine to clean"

# Basic check step
check: all
	@echo "Build check passed (skipping utility.sh checks)"

# Dummy test target
test:
	@echo "No tests defined"

# Run packaging for Linux and Windows
package: all
	@chmod +x packaging/linux/buildpackage.sh packaging/windows/buildpackage.sh
	@./packaging/linux/buildpackage.sh
	@./packaging/windows/buildpackage.sh
