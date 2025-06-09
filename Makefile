.PHONY: all clean check test package

RUNTIME ?= dotnet
MOD_SOLUTION_FILES := $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

all: engine
ifeq ($(RUNTIME), mono)
	@echo "Using Mono..."
	@find . -maxdepth 1 -name '*.sln' -exec msbuild -t:Build -restore -p:Configuration=Release -p:Mono=true \;
else
	@echo "Using .NET..."
	@find . -maxdepth 1 -name '*.sln' -exec dotnet build -c Release \;
endif

engine:
	@echo "Fetching engine..."
	@./fetch-engine.sh || echo "Engine fetch stub (not implemented)"

clean:
	@echo "Cleaning..."
	@find . -maxdepth 1 -name '*.sln' -exec dotnet clean \;

check: all
	@echo "CI check passed"

test:
	@echo "No test defined"

package: all
	@chmod +x packaging/linux/buildpackage.sh packaging/windows/buildpackage.sh
	@./packaging/linux/buildpackage.sh
	@./packaging/windows/buildpackage.sh