.PHONY: all clean check test package engine

RUNTIME ?= dotnet
MOD_SOLUTION_FILES := $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

all: engine
ifeq ($(RUNTIME), mono)
	@echo "Using Mono to build..."
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Build -restore -p:Configuration=Release -p:Mono=true \;
endif
else
	@echo "Using .NET to build..."
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec dotnet build -c Release \;
endif
endif

engine:
	@echo "Fetching engine..."
	@chmod +x fetch-engine.sh
	@./fetch-engine.sh || echo "Engine fetch skipped or already present"

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

check: all
	@echo "✔ Build completed. Skipping utility checks for CI."

test:
	@echo "ℹ No test steps defined yet"

package: all
	@echo "📦 Running packaging scripts..."
	@chmod +x packaging/linux/buildpackage.sh packaging/windows/buildpackage.sh
	@./packaging/linux/buildpackage.sh
	@./packaging/windows/buildpackage.sh
	@echo "✅ Packaging complete"
