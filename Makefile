# VojkanBroforceMods Makefile

PROJECT_NAME := VojkanBroforceMods

# Custom help text for individual projects
define EXTRA_HELP
	@echo "Individual projects:"
	@echo "  make cobro              Build Cobro"
	@echo "  make jack-broton        Build Jack Broton"
endef
export EXTRA_HELP

# No .sln file - use custom build targets
CUSTOM_BUILD := 1
include Scripts/Makefile.common

# Custom build targets (no .sln file in this repo)
.PHONY: build
build: cobro jack-broton

.PHONY: build-no-launch
build-no-launch:
	$(MAKE) build LAUNCH=no

.PHONY: clean
clean:
	"$(MSBUILD)" "Cobro/Cobro/Cobro.csproj" /t:Clean $(MSBUILD_FLAGS)
	"$(MSBUILD)" "Jack Broton/Jack Broton/Jack Broton.csproj" /t:Clean $(MSBUILD_FLAGS)

.PHONY: rebuild
rebuild: clean
	$(MAKE) build LAUNCH=no

# Individual project targets
.PHONY: cobro
cobro:
	"$(MSBUILD)" "Cobro/Cobro/Cobro.csproj" $(MSBUILD_FLAGS) $(LAUNCH_FLAGS)

.PHONY: jack-broton
jack-broton:
	"$(MSBUILD)" "Jack Broton/Jack Broton/Jack Broton.csproj" $(MSBUILD_FLAGS) $(LAUNCH_FLAGS)
