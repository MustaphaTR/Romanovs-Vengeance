MOD_NAME := Mishmash
VERSION ?= dev
DIST_DIR := dist
SRC_DIR := mods/$(MOD_NAME)

all: clean windows linux mac

windows:
	@echo "Packaging for Windows..."
	mkdir -p $(DIST_DIR)/windows/$(MOD_NAME)
	cp -r $(SRC_DIR)/. $(DIST_DIR)/windows/$(MOD_NAME)/ || echo "Some files skipped"
	cd $(DIST_DIR)/windows && zip -r ../$(MOD_NAME)-windows-$(VERSION).zip $(MOD_NAME)
	rm -rf $(DIST_DIR)/windows

linux:
	@echo "Packaging for Linux..."
	mkdir -p $(DIST_DIR)/linux/$(MOD_NAME)
	cp -r $(SRC_DIR)/* $(DIST_DIR)/linux/$(MOD_NAME)/ || echo "Some files skipped"
	tar -czf $(DIST_DIR)/$(MOD_NAME)-linux-$(VERSION).tar.gz -C $(DIST_DIR)/linux $(MOD_NAME)
	rm -rf $(DIST_DIR)/linux

mac:
	@echo "Packaging for macOS..."
	mkdir -p $(DIST_DIR)/mac/$(MOD_NAME)
	cp -r $(SRC_DIR)/* $(DIST_DIR)/mac/$(MOD_NAME)/ || echo "Some files skipped"
	tar -czf $(DIST_DIR)/$(MOD_NAME)-macos-$(VERSION).tar.gz -C $(DIST_DIR)/mac $(MOD_NAME)
	rm -rf $(DIST_DIR)/mac

clean:
	rm -rf $(DIST_DIR)
	mkdir -p $(DIST_DIR)
