# Makefile for packaging OpenRA mod

MOD_NAME := Mishmash
VERSION ?= dev
DIST_DIR := dist
PACKAGE_CONTENT := mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/

all: clean windows linux mac

windows:
	@echo "Packaging for Windows..."
	mkdir -p $(DIST_DIR)/windows/$(MOD_NAME)
	cp -r $(PACKAGE_CONTENT) $(DIST_DIR)/windows/$(MOD_NAME)/
	cd $(DIST_DIR)/windows && zip -r ../$(MOD_NAME)-windows-$(VERSION).zip $(MOD_NAME)
	rm -rf $(DIST_DIR)/windows

linux:
	@echo "Packaging for Linux..."
	mkdir -p $(DIST_DIR)/linux/$(MOD_NAME)
	cp -r $(PACKAGE_CONTENT) $(DIST_DIR)/linux/$(MOD_NAME)/
	tar -czf $(DIST_DIR)/$(MOD_NAME)-linux-$(VERSION).tar.gz -C $(DIST_DIR)/linux $(MOD_NAME)
	rm -rf $(DIST_DIR)/linux

mac:
	@echo "Packaging for macOS..."
	mkdir -p $(DIST_DIR)/mac/$(MOD_NAME)
	cp -r $(PACKAGE_CONTENT) $(DIST_DIR)/mac/$(MOD_NAME)/
	tar -czf $(DIST_DIR)/$(MOD_NAME)-macos-$(VERSION).tar.gz -C $(DIST_DIR)/mac $(MOD_NAME)
	rm -rf $(DIST_DIR)/mac

clean:
	rm -rf $(DIST_DIR)
	mkdir -p $(DIST_DIR)
