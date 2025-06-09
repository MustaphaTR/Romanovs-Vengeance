.PHONY: all clean check test

# Default toolchain
RUNTIME ?= dotnet

# Automatically detect target platform
ifndef TARGETPLATFORM
  UNAME_S := $(shell uname -s)
  UNAME_M := $(shell uname -m)
  ifeq ($(UNAME_S),Darwin)
    TARGETPLATFORM = osx-x64
  else
    ifeq ($(UNAME_M),x86_64)
      TARGETPLATFORM = linux-x64
    else
      TARGETPLATFORM = unix-generic
    endif
  endif
endif

# List .sln files
MOD_SOLUTION_FILES := $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

# Default: build
all:
ifeq ($(RUNTIME), mono)
	@echo "Building using Mono"
	@command -v msbuild >/dev/null || (echo "Mono is not installed!"; exit 1)
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Build -restore -p:Configuration=Release -p:Mono=true \;
endif
else
	@echo "Building using .NET"
	@find . -maxdepth 1 -name '*.sln' -exec dotnet build -c Release -p:TargetPlatform=$(TARGETPLATFORM) \;
endif

# Clean build artifacts
clean:
ifneq ("$(MOD_SOLUTION_FILES)","")
ifeq ($(RUNTIME), mono)
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Clean \;
else
	@find . -maxdepth 1 -name '*.sln' -exec dotnet clean \;
endif
endif

# Basic check step
check: all
	@echo "Skipping deep utility checks for now"
	@echo "Mod built successfully."

# Dummy test target
test: all
	@echo "Testing: No tests defined. Add OpenRA.Utility steps here if needed."
